using System.Text;
using System.Text.Json;
using McpServer.Host.Transport.Stdio;
using McpServer.Protocol.JsonRpc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace McpServer.UnitTests.Transport;

public sealed class StdioMessageTransportTests
{
    [Fact]
    public async Task ReadRequestAsync_Should_Read_NewlineDelimited_Request()
    {
        var input = new MemoryStream(Encoding.UTF8.GetBytes("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\",\"params\":{}}\n"));
        var output = new MemoryStream();
        var logger = Substitute.For<ILogger<StdioMessageTransport>>();

        await using var transport = new StdioMessageTransport(input, output, logger);

        var request = await transport.ReadRequestAsync(CancellationToken.None);

        Assert.NotNull(request);
        Assert.Equal("tools/list", request!.Method);
    }

    [Fact]
    public async Task WriteResponseAsync_Should_Write_Single_Line_Response()
    {
        var input = new MemoryStream();
        var output = new MemoryStream();
        var logger = Substitute.For<ILogger<StdioMessageTransport>>();

        await using var transport = new StdioMessageTransport(input, output, logger);
        var id = JsonDocument.Parse("1").RootElement.Clone();
        var response = new JsonRpcResponse("2.0", id, Result: new { ok = true });

        await transport.WriteResponseAsync(response, CancellationToken.None);

        output.Position = 0;
        using var reader = new StreamReader(output, Encoding.UTF8);
        var text = await reader.ReadToEndAsync();

        Assert.EndsWith("\n", text);
    }

    [Fact]
    public async Task WriteResponseAsync_Should_Omit_Null_Error_Field_From_Success_Response()
    {
        var input = new MemoryStream();
        var output = new MemoryStream();
        var logger = Substitute.For<ILogger<StdioMessageTransport>>();

        await using var transport = new StdioMessageTransport(input, output, logger);
        var id = JsonDocument.Parse("1").RootElement.Clone();
        var response = new JsonRpcResponse("2.0", id, Result: new { ok = true });

        await transport.WriteResponseAsync(response, CancellationToken.None);

        output.Position = 0;
        using var reader = new StreamReader(output, Encoding.UTF8);
        var text = await reader.ReadToEndAsync();

        Assert.DoesNotContain("\"error\":null", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task WriteResponseAsync_Should_Flush_Stream_Once()
    {
        var input = new MemoryStream();
        var output = new CountingStream();
        var logger = Substitute.For<ILogger<StdioMessageTransport>>();

        await using var transport = new StdioMessageTransport(input, output, logger);
        var id = JsonDocument.Parse("1").RootElement.Clone();
        var response = new JsonRpcResponse("2.0", id, Result: new { ok = true });

        await transport.WriteResponseAsync(response, CancellationToken.None);

        Assert.Equal(1, output.FlushCount);
        Assert.Equal(1, output.FlushAsyncCount);
    }

    [Fact]
    public async Task WriteNotificationAsync_Should_Write_Single_Line_Notification()
    {
        var input = new MemoryStream();
        var output = new MemoryStream();
        var logger = Substitute.For<ILogger<StdioMessageTransport>>();

        await using var transport = new StdioMessageTransport(input, output, logger);
        var notification = new JsonRpcNotification("2.0", "notifications/workspace/changed", new { ok = true });

        await transport.WriteNotificationAsync(notification, CancellationToken.None);

        output.Position = 0;
        using var reader = new StreamReader(output, Encoding.UTF8);
        var text = await reader.ReadToEndAsync();

        Assert.EndsWith("\n", text);
        Assert.Contains("\"notifications/workspace/changed\"", text, StringComparison.Ordinal);
    }

    private sealed class CountingStream : MemoryStream
    {
        public int FlushCount { get; private set; }
        public int FlushAsyncCount { get; private set; }

        public override void Flush()
        {
            FlushCount++;
            base.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            FlushAsyncCount++;
            return base.FlushAsync(cancellationToken);
        }
    }
}
