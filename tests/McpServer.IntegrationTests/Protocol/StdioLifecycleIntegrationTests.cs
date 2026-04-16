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
    public async Task Initialize_Should_Fall_Back_To_Compatible_Server_Version_For_Unknown_Client_Request()
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
                protocolVersion = "2026-01-01",
                capabilities = new { },
                clientInfo = new { name = "lmstudio-test", version = "1.0.0" }
            }
        });

        Assert.NotNull(response);
        Assert.Equal("2025-03-26", response!.RootElement.GetProperty("result").GetProperty("protocolVersion").GetString());
    }

    [Fact]
    public async Task Ping_Should_Return_Empty_Result()
    {
        await using var server = await StdioTestServerProcess.StartAsync(HostProjectPath);
        var client = new JsonRpcTestClient(server.Input, server.Output);

        var response = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            id = 99,
            method = "ping"
        });

        Assert.NotNull(response);
        Assert.False(response!.RootElement.TryGetProperty("error", out _));
        Assert.Equal(JsonValueKind.Object, response.RootElement.GetProperty("result").ValueKind);
    }

    [Fact]
    public async Task Host_Should_Work_When_Started_From_An_Unrelated_Working_Directory()
    {
        var launchDirectory = Path.Combine(Path.GetTempPath(), "mcpserver-lmstudio-launch", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(launchDirectory);

        try
        {
            await using var server = await StdioTestServerProcess.StartAsync(HostProjectPath, launchDirectory);
            var client = new JsonRpcTestClient(server.Input, server.Output);

            _ = await client.SendRequestAsync(new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "initialize",
                @params = new
                {
                    protocolVersion = "2025-11-25",
                    capabilities = new { },
                    clientInfo = new { name = "lmstudio-test", version = "1.0.0" }
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
                        path = "lmstudio-smoke.txt",
                        content = "lmstudio cwd ok",
                        overwrite = true
                    }
                }
            });

            Assert.NotNull(writeResponse);
            Assert.False(writeResponse!.RootElement.TryGetProperty("error", out _));

            var expectedWorkspaceFile = Path.Combine(
                Path.GetDirectoryName(HostProjectPath)!,
                "bin",
                "Debug",
                "net10.0",
                "workspace",
                "lmstudio-smoke.txt");

            Assert.True(File.Exists(expectedWorkspaceFile));
        }
        finally
        {
            if (Directory.Exists(launchDirectory))
            {
                Directory.Delete(launchDirectory, recursive: true);
            }
        }
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
        Assert.False(writeResponse!.RootElement.TryGetProperty("error", out _));

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
        Assert.False(readResponse!.RootElement.TryGetProperty("error", out _));

        var contents = readResponse.RootElement
            .GetProperty("result")
            .GetProperty("contents");

        var array = contents.EnumerateArray().ToArray();
        Assert.Single(array);
        var content = array[0];
        Assert.Equal("smoke test ok", content.GetProperty("text").GetString());
    }

    [Fact]
    public async Task Shell_Exec_Tool_Should_Run_Dotnet_Version_In_Workspace()
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

        var execResponse = await client.SendRequestAsync(new
        {
            jsonrpc = "2.0",
            id = 4,
            method = "tools/call",
            @params = new
            {
                name = "shell.exec",
                arguments = new
                {
                    command = "dotnet",
                    args = new[] { "--version" },
                    timeoutSeconds = 30
                }
            }
        });

        Assert.NotNull(execResponse);
        Assert.False(execResponse!.RootElement.TryGetProperty("error", out _));

        var result = execResponse.RootElement.GetProperty("result");
        Assert.False(result.GetProperty("isError").GetBoolean());

        var structuredContent = result.GetProperty("structuredContent");
        Assert.Equal(0, structuredContent.GetProperty("exitCode").GetInt32());
        Assert.False(structuredContent.GetProperty("timedOut").GetBoolean());

        var stdout = structuredContent.GetProperty("standardOutput").GetString();
        Assert.False(string.IsNullOrWhiteSpace(stdout));
    }

    [Fact]
    public async Task Initialize_Response_Should_Omit_Null_Error_Field()
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
                protocolVersion = "2025-11-25",
                capabilities = new { },
                clientInfo = new { name = "shape-test", version = "1.0.0" }
            }
        });

        Assert.NotNull(response);
        Assert.False(response!.RootElement.TryGetProperty("error", out _));
        Assert.True(response.RootElement.TryGetProperty("result", out _));
    }
}
