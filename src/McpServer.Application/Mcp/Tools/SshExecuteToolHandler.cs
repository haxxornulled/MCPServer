using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Ssh;
using McpServer.Application.Ssh.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools
{
    public sealed class SshExecuteToolHandler(
        ISshService sshService,
        ILogger<SshExecuteToolHandler> logger) : IToolHandler<SshExecuteRequest>
    {
        public string Name => "ssh.execute";
        public string Description => "Executes a command on an SSH server.";

        public JsonElement GetInputSchema() =>
            JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    profile = new { type = "string" },
                    command = new { type = "string" },
                    working_directory = new { type = "string" }
                },
                required = new[] { "profile", "command" }
            });

        public async ValueTask<Fin<CallToolResult>> Handle(SshExecuteRequest request, CancellationToken ct)
        {
            var result = await sshService
                .ExecuteAsync(new ExecuteSshCommand(request.Profile, request.Command, request.WorkingDirectory), ct)
                .ConfigureAwait(false);

            return result.Map(sshResult =>
            {
                logger.LogInformation("Tool {ToolName} completed", Name);
                
                var content = JsonSerializer.Serialize(sshResult, new JsonSerializerOptions { WriteIndented = true });
                
                return new CallToolResult(
                [
                    new ContentItem("text", content)
                ], 
                StructuredContent: sshResult);
            });
        }
    }
}
