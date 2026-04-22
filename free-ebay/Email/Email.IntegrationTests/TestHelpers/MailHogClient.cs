using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Email.IntegrationTests.TestHelpers;

/// Thin wrapper around MailHog's HTTP v2 API for asserting delivered messages in tests.
public sealed class MailHogClient(string baseUrl) : IDisposable
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(baseUrl) };

    public async Task<MailHogMessage[]> GetMessagesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetFromJsonAsync<MailHogApiResponse>(
            "/api/v2/messages", cancellationToken);
        return response?.Items ?? [];
    }

    public async Task<MailHogMessage> WaitForMessageAsync(
        Func<MailHogMessage, bool> predicate,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        while (DateTime.UtcNow < deadline)
        {
            var messages = await GetMessagesAsync(cancellationToken);
            var match = messages.FirstOrDefault(predicate);
            if (match is not null) return match;
            await Task.Delay(100, cancellationToken);
        }
        throw new TimeoutException("MailHog did not receive an expected message within the timeout.");
    }

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default) =>
        await _http.DeleteAsync("/api/v1/messages", cancellationToken);

    public void Dispose() => _http.Dispose();

    private sealed class MailHogApiResponse
    {
        [JsonPropertyName("items")]
        public MailHogMessage[] Items { get; set; } = [];
    }
}

public sealed class MailHogMessage
{
    [JsonPropertyName("ID")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("Content")]
    public MailHogContent Content { get; set; } = new();
}

public sealed class MailHogContent
{
    [JsonPropertyName("Headers")]
    public Dictionary<string, string[]> Headers { get; set; } = [];

    [JsonPropertyName("Body")]
    public string Body { get; set; } = string.Empty;
}
