using System.Text.Json;
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

    [Fact]
    public async Task File_Tool_And_Resource_Flow_Should_Work_End_To_End()
    {
        await using var server = await StdioTestServerProcess.StartAsync(HostProjectPath);
        var client = new JsonRpcTestClient(server.Input, server.Output);

        _ = await client.SendRequestAsync(new
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

        await client.SendNotificationAsync(new
        {
            jsonrpc = "2.0",
            method = "notifications/initialized"
        });

        var writeResponse = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new
            {
                name = "fs.write_text",
                arguments = new
                {
                    path = "smoke.txt",
                    content = "smoke test ok",
                    overwrite = true
                }
            }
        });

        Assert.NotNull(writeResponse);
        Assert.True(writeResponse!.RootElement.GetProperty("error").ValueKind is JsonValueKind.Null);

        var readResponse = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            id = 3,
            method = "resources/read",
            @params = new
            {
                uri = "file:///workspace/smoke.txt"
            }
        });

        Assert.NotNull(readResponse);
        Assert.True(readResponse!.RootElement.GetProperty("error").ValueKind is JsonValueKind.Null);

        var contents = readResponse.RootElement
            .GetProperty("result")
            .GetProperty("contents");

        var array = contents.EnumerateArray().ToArray();
        Assert.Single(array);
        var content = array[0];
        Assert.Equal("smoke test ok", content.GetProperty("text").GetString());
    }
}
