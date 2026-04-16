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
}
