using Confluent.Kafka;
using Newtonsoft.Json;
using payment_service.Entities;

namespace payment_service.Services
{
    public class PaymentService : IHostedService
    {
        private readonly ILogger<PaymentService> _logger;
        private readonly string _topic = "payment";
        private readonly string _bootstrapServers = "localhost:9092"; // Adjust if necessary
        private IConsumer<Ignore, string> _consumer;
        private IProducer<Null, string> _producer; // Add producer for sending messages

        public PaymentService(ILogger<PaymentService> logger)
        {
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _bootstrapServers,
                GroupId = "payment-service-group",
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
            _consumer.Subscribe(_topic);

            // Producer configuration
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers
            };
            _producer = new ProducerBuilder<Null, string>(producerConfig).Build();

            Task.Run(() => ConsumeMessages(cancellationToken));

            return Task.CompletedTask;
        }

        private void ConsumeMessages(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var cr = _consumer.Consume(cancellationToken);
                    var payment = JsonConvert.DeserializeObject<Payment>(cr.Value);
                    var result = ProcessMessage(payment);

                    // Send the result back to the same topic
                    SendResultToKafka(result);
                }
            }
            catch (OperationCanceledException)
            {
                _consumer.Close();
            }
        }

        public object ProcessMessage(Payment payment)
        {
            // Log the received payment message
            _logger.LogInformation($"Processing payment for OrderId: {payment.OrderId}, Amount: {payment.Amount.Format()}, Method: {payment.PaymentMethod}");

            // Return a success response
            return new { status = "success" };
        }

        private void SendResultToKafka(object result)
        {
            var resultJson = JsonConvert.SerializeObject(result);
            _producer.Produce(_topic, new Message<Null, string> { Value = resultJson });
            _producer.Flush(TimeSpan.FromSeconds(10)); // Ensure the message is sent
            _logger.LogInformation($"Response sent to Kafka: {resultJson}");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _consumer?.Close();
            _producer?.Dispose();
            return Task.CompletedTask;
        }
    }
}