using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Email.Models;
using Email.Options;
using Email.Services;
using Microsoft.Extensions.Options;

namespace Email.Messaging;

public sealed class KafkaEmailConsumer(
    IOptions<KafkaOptions> kafkaOptions,
    IProcessedMessageStore processedMessageStore,
    IEmailSender emailSender,
    ILogger<KafkaEmailConsumer> logger) : BackgroundService
{
    private const string OrderConfirmationEventType = "OrderConfirmationEmailRequested";
    private const string EmailVerificationEventType = "EmailVerificationRequested";
    private const string PasswordResetEventType = "PasswordResetRequested";
    private readonly KafkaOptions _options = kafkaOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await processedMessageStore.InitializeAsync(stoppingToken);

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = _options.EnableAutoCommit,
            EnableAutoOffsetStore = false,
            IsolationLevel = IsolationLevel.ReadCommitted
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.All,
            MessageSendMaxRetries = 10,
            MaxInFlight = 1
        };

        using var consumer = new ConsumerBuilder<string, string>(consumerConfig)
            .SetErrorHandler((_, error) => logger.LogError("Kafka consumer error: {Reason}", error.Reason))
            .Build();

        using var producer = new ProducerBuilder<string, string>(producerConfig)
            .SetErrorHandler((_, error) => logger.LogError("Kafka producer error: {Reason}", error.Reason))
            .Build();

        consumer.Subscribe(_options.EmailEventsTopic);

        logger.LogInformation(
            "Email consumer subscribed to topic {Topic} with group {GroupId}",
            _options.EmailEventsTopic,
            _options.ConsumerGroupId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = consumer.Consume(stoppingToken);
                if (consumeResult?.Message?.Value is null)
                {
                    continue;
                }

                var handled = await HandleMessageAsync(consumeResult.Message, producer, stoppingToken);
                if (handled)
                {
                    consumer.StoreOffset(consumeResult);
                    consumer.Commit(consumeResult);
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Email consumer stopping");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error in email consumer loop");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private async Task<bool> HandleMessageAsync(
        Message<string, string> message,
        IProducer<string, string> producer,
        CancellationToken cancellationToken)
    {
        EventWrapper? wrapper;
        try
        {
            wrapper = JsonSerializer.Deserialize<EventWrapper>(message.Value);
        }
        catch (Exception ex)
        {
            await PublishToDlqAsync(producer, message.Key, message.Value, GetReplayAttempt(message.Headers), "Invalid event wrapper JSON", ex, cancellationToken);
            return true;
        }

        if (wrapper is null)
        {
            await PublishToDlqAsync(producer, message.Key, message.Value, GetReplayAttempt(message.Headers), "Event wrapper is null", null, cancellationToken);
            return true;
        }

        return wrapper.EventType switch
        {
            OrderConfirmationEventType or EmailVerificationEventType or PasswordResetEventType
                => await HandleEmailAsync(wrapper, producer, message, cancellationToken),
            _ => LogUnsupported(wrapper.EventType)
        };
    }

    private bool LogUnsupported(string eventType)
    {
        logger.LogWarning("Skipping unsupported event type {EventType}", eventType);
        return true;
    }

    private async Task<bool> HandleEmailAsync(
        EventWrapper wrapper,
        IProducer<string, string> producer,
        Message<string, string> message,
        CancellationToken cancellationToken)
    {
        AuthEmailMessage? request;
        try
        {
            request = wrapper.Payload.Deserialize<AuthEmailMessage>();
        }
        catch (Exception ex)
        {
            await PublishToDlqAsync(producer, message.Key, message.Value, GetReplayAttempt(message.Headers), "Invalid email payload JSON", ex, cancellationToken);
            return true;
        }

        if (request is null)
        {
            await PublishToDlqAsync(producer, message.Key, message.Value, GetReplayAttempt(message.Headers), "Email payload is null", null, cancellationToken);
            return true;
        }

        if (await processedMessageStore.IsProcessedAsync(wrapper.EventId, cancellationToken))
        {
            logger.LogInformation("Skipping duplicate email message {MessageId}", wrapper.EventId);
            return true;
        }

        var maxAttempts = request.IsImportant ? _options.MaxDeliveryAttempts : 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await emailSender.SendAsync(request.To, request.From, request.Subject, request.HtmlBody, cancellationToken);
                await processedMessageStore.MarkProcessedAsync(wrapper.EventId, cancellationToken);
                logger.LogInformation("Processed email message {MessageId}", wrapper.EventId);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to deliver email message {MessageId} on attempt {Attempt}/{MaxAttempts}",
                    wrapper.EventId,
                    attempt,
                    maxAttempts);

                if (attempt == maxAttempts)
                {
                    if (request.IsImportant)
                    {
                        await PublishToDlqAsync(
                            producer,
                            message.Key,
                            message.Value,
                            GetReplayAttempt(message.Headers),
                            "Email delivery failed after max retries",
                            ex,
                            cancellationToken);
                    }
                    else
                    {
                        logger.LogWarning(
                            ex,
                            "Non-important email {MessageId} failed and will not be sent to DLQ",
                            wrapper.EventId);
                        await processedMessageStore.MarkProcessedAsync(wrapper.EventId, cancellationToken);
                    }

                    return true;
                }

                await Task.Delay(_options.RetryDelayMs, cancellationToken);
            }
        }

        return true;
    }

    private async Task PublishToDlqAsync(
        IProducer<string, string> producer,
        string key,
        string value,
        int replayAttempt,
        string reason,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        var dlqMessage = new Message<string, string>
        {
            Key = key,
            Value = value,
            Headers = new Headers
            {
                { "dlq-reason", Encoding.UTF8.GetBytes(reason) },
                { "failed-at", Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString("O")) },
                { "dlq-replay-attempt", Encoding.UTF8.GetBytes(replayAttempt.ToString()) }
            }
        };

        if (exception is not null)
        {
            dlqMessage.Headers.Add("error", Encoding.UTF8.GetBytes(exception.Message));
        }

        await producer.ProduceAsync(_options.EmailDlqTopic, dlqMessage, cancellationToken);

        logger.LogWarning(
            "Published failed email message to DLQ topic {Topic}. Reason: {Reason}",
            _options.EmailDlqTopic,
            reason);
    }

    private static int GetReplayAttempt(Headers headers)
    {
        var raw = headers.FirstOrDefault(h => h.Key == "dlq-replay-attempt")?.GetValueBytes();
        if (raw is null)
        {
            return 0;
        }

        return int.TryParse(Encoding.UTF8.GetString(raw), out var parsed) ? parsed : 0;
    }
}