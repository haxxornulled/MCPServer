using System.Text.Json;

namespace McpServer.IntegrationTests.Infrastructure;

public sealed class JsonRpcTestClient(StreamWriter input, StreamReader output)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<JsonDocument?> SendRequestAsync(object payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);

        if (json.Contains('\n') || json.Contains('\r'))
        {
            throw new InvalidOperationException("Test client emitted an invalid stdio MCP message.");
        }

        await input.WriteLineAsync(json).ConfigureAwait(false);
        await input.FlushAsync().ConfigureAwait(false);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

        while (true)
        {
            var line = await output.ReadLineAsync(timeoutCts.Token).ConfigureAwait(false);
            if (line is null)
            {
                return null;
            }

            var document = JsonDocument.Parse(line);
            if (document.RootElement.TryGetProperty("id", out _))
            {
                return document;
            }
        }
    }

    public async Task SendNotificationAsync(object payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload, JsonOptions);

        if (json.Contains('\n') || json.Contains('\r'))
        {
            throw new InvalidOperationException("Test client emitted an invalid stdio MCP notification.");
        }

        await input.WriteLineAsync(json).ConfigureAwait(false);
        await input.FlushAsync().ConfigureAwait(false);
        await Task.Delay(25, ct).ConfigureAwait(false);
    }
}
