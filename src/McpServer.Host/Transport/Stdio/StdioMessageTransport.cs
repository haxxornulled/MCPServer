using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using McpServer.Protocol.JsonRpc;

namespace McpServer.Host.Transport.Stdio;

public sealed class StdioMessageTransport(
    Stream inputStream,
    Stream outputStream,
    ILogger<StdioMessageTransport> logger) : IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
        leaveOpen: true);
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    private int _nextRequestId = 1000;

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

        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _writer.WriteLineAsync(json).ConfigureAwait(false);
            await _writer.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async ValueTask WriteNotificationAsync(JsonRpcNotification notification, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(notification, JsonOptions);

        if (json.Contains('\n') || json.Contains('\r'))
        {
            throw new InvalidOperationException("Serialized stdio MCP notification must not contain embedded newlines.");
        }

        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _writer.WriteLineAsync(json).ConfigureAwait(false);
            await _writer.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public async ValueTask<JsonRpcResponse?> SendRequestAsync(string method, object? parameters, CancellationToken ct)
    {
        var requestId = Interlocked.Increment(ref _nextRequestId);
        var request = new JsonRpcRequest(
            "2.0",
            JsonSerializer.SerializeToElement(requestId, JsonOptions),
            method,
            parameters is null ? null : JsonSerializer.SerializeToElement(parameters, JsonOptions));

        var json = JsonSerializer.Serialize(request, JsonOptions);
        if (json.Contains('\n') || json.Contains('\r'))
        {
            throw new InvalidOperationException("Serialized stdio MCP request must not contain embedded newlines.");
        }

        await _writeLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _writer.WriteLineAsync(json).ConfigureAwait(false);
            await _writer.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }

        while (true)
        {
            var line = await _reader.ReadLineAsync(ct).ConfigureAwait(false);
            if (line is null || string.IsNullOrWhiteSpace(line))
            {
                return null;
            }

            try
            {
                var response = JsonSerializer.Deserialize<JsonRpcResponse>(line, JsonOptions);
                if (response?.Id is not { } responseId || responseId.ValueKind != JsonValueKind.Number)
                {
                    continue;
                }

                if (responseId.GetInt32() == requestId)
                {
                    return response;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to deserialize JSON-RPC response line");
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        _reader.Dispose();
        _writer.Dispose();
        _writeLock.Dispose();
        return ValueTask.CompletedTask;
    }
}
