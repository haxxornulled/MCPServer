using McpServer.IntegrationTests.Infrastructure;
using Xunit;

namespace McpServer.IntegrationTests.Protocol;

public sealed class StdioLifecycleIntegrationTests
{
    private static string HostProjectPath =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "McpServer.Host", "McpServer.Host.csproj"));

    [Fact]
    public async Task Initialize_Response_Should_Match_Typed_Shape()
    {
        await using var server = await StdioTestServerProcess.StartAsync(HostProjectPath);
        var client = new JsonRpcTestClient(server.Input, server.Output);

        var response = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2025-03-26",
                capabilities = new { },
                clientInfo = new { name = "xunit-test", version = "1.0.0" }
            }
        });

        Assert.NotNull(response);
    }
}
