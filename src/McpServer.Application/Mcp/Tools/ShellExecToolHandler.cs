using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Execution;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Application.Execution.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools
{
    public sealed class ShellExecToolHandler(
        IProcessExecutionService processExecutionService,
        ILogger<ShellExecToolHandler> logger) : IToolHandler<ShellExecRequest>
    {
        public string Name => "shell.exec";
        public string Description => "Executes a shell command.";

        public JsonElement GetInputSchema() =>
            JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    command = new { type = "string" },
                    args = new { type = "array", items = new { type = "string" } },
                    workingDirectory = new { type = "string" },
                    timeoutSeconds = new { type = "integer", @default = 30, minimum = 1, maximum = 300 },
                    maxOutputChars = new { type = "integer", @default = 12000, minimum = 256, maximum = 200000 }
                },
                required = new[] { "command" }
            });

        public async ValueTask<Fin<CallToolResult>> Handle(ShellExecRequest request, CancellationToken ct)
        {
            var result = await processExecutionService
                .RunAsync(new RunProcessCommand(
                    request.Command,
                    request.Args,
                    request.WorkingDirectory,
                    request.TimeoutSeconds,
                    request.MaxOutputChars), ct)
                .ConfigureAwait(false);

            return result.Map(processResult =>
            {
                logger.LogInformation("Tool {ToolName} completed for command: {Command}", Name, request.Command);
                
                var text = $"Command: {processResult.Command}\n" +
                           $"Exit Code: {processResult.ExitCode}\n" +
                           $"Output: {processResult.StandardOutput}\n" +
                           $"Error: {processResult.StandardError}";

                return new CallToolResult(
                [
                    new ContentItem("text", text)
                ], 
                StructuredContent: processResult);
            });
        }
    }
}
