using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Execution;
using McpServer.Application.Execution.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools
{
    public sealed class ExecRunProcessToolHandler(
        IProcessExecutionService processExecutionService,
        ILogger<ExecRunProcessToolHandler> logger) : IToolHandler<ExecRunProcessRequest>
    {
        public string Name => "exec.run_process";
        public string Description => "Runs an external process.";

        public JsonElement GetInputSchema() =>
            JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    command = new { type = "string" },
                    arguments = new { type = "array", items = new { type = "string" } },
                    working_directory = new { type = "string" },
                    timeout_seconds = new { type = "integer" }
                },
                required = new[] { "command" }
            });

        public async ValueTask<Fin<CallToolResult>> Handle(ExecRunProcessRequest request, CancellationToken ct)
        {
            var result = await processExecutionService
                .RunAsync(new RunProcessCommand(
                    request.Command,
                    request.Arguments,
                    request.WorkingDirectory,
                    request.TimeoutSeconds), ct)
                .ConfigureAwait(false);

            return result.Map(processResult =>
            {
                logger.LogInformation("Tool {ToolName} completed", Name);
                
                var content = JsonSerializer.Serialize(processResult, new JsonSerializerOptions { WriteIndented = true });
                
                return new CallToolResult(
                [
                    new ContentItem("text", content)
                ], 
                StructuredContent: processResult);
            });
        }
    }
}
