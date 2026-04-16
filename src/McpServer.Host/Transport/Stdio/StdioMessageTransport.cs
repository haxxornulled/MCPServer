using System.Text;
using System.Text.Json;
using McpServer.Protocol.JsonRpc;

namespace McpServer.Host.Transport.Stdio;

public sealed class StdioMessageTransport(
    Stream inputStream,
    Stream outputStream,
    ILogger<StdioMessageTransport> logger) : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly StreamReader _reader = new(
        inputStream,
        Encoding.UTF8,
        detectEncodingFromByteOrderMarks: false,
        bufferSize: 16 * 1024,
        leaveOpen: true);

    private readonly StreamWriter _writer = new(
        outputStream,
        new UTF8Encoding(false),
        bufferSize: 16 * 1024,
        leaveOpen: true)
    {
        AutoFlush = true
    };

    public async ValueTask<JsonRpcRequest?> ReadRequestAsync(CancellationToken ct)
    {
        var line = await _reader.ReadLineAsync(ct).ConfigureAwait(false);
        if (line is null || string.IsNullOrWhiteSpace(line))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<JsonRpcRequest>(line, JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to deserialize JSON-RPC request line");
            return null;
        }
    }

    public async ValueTask WriteResponseAsync(JsonRpcResponse response, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(response, JsonOptions);

        if (json.Contains('\n') || json.Contains('\r'))
        {
            throw new InvalidOperationException("Serialized stdio MCP response must not contain embedded newlines.");
        }

        await _writer.WriteLineAsync(json).ConfigureAwait(false);
        await _writer.FlushAsync().ConfigureAwait(false);
    }

    public async ValueTask WriteNotificationAsync(JsonRpcNotification notification, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(notification, JsonOptions);

        if (json.Contains('\n') || json.Contains('\r'))
        {
            throw new InvalidOperationException("Serialized stdio MCP notification must not contain embedded newlines.");
        }

        await _writer.WriteLineAsync(json).ConfigureAwait(false);
        await _writer.FlushAsync().ConfigureAwait(false);
    }

    public ValueTask DisposeAsync()
    {
        _reader.Dispose();
        _writer.Dispose();
        return ValueTask.CompletedTask;
    }
}
