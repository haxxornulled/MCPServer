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

        var line = await output.ReadLineAsync(timeoutCts.Token).ConfigureAwait(false);
        return line is null ? null : JsonDocument.Parse(line);
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
