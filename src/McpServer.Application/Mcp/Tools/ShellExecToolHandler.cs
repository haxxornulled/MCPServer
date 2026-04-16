using System.Text;
using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Execution;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Application.Execution.Commands;
using McpServer.Application.Execution.Results;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools;

public sealed class ShellExecToolHandler(
    IProcessExecutionService processExecutionService,
    ILogger<ShellExecToolHandler> logger) : IToolHandler<ShellExecRequest>
{
    public string Name => "shell.exec";
    public string Description => "Runs a non-interactive command inside the MCP workspace and returns exit code, stdout, and stderr.";

    public JsonElement GetInputSchema() =>
        JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                command = new { type = "string" },
                args = new
                {
                    type = "array",
                    items = new { type = "string" }
                },
                workingDirectory = new { type = "string" },
                timeoutSeconds = new { type = "integer", @default = 60, minimum = 1, maximum = 600 },
                maxOutputChars = new { type = "integer", @default = 12000, minimum = 256, maximum = 200000 }
            },
            required = new[] { "command" }
        });

    public async ValueTask<Fin<CallToolResult>> Handle(ShellExecRequest request, CancellationToken ct)
    {
        var result = await processExecutionService
            .RunAsync(
                new RunProcessCommand(
                    request.Command,
                    request.Args ?? [],
                    request.WorkingDirectory,
                    request.TimeoutSeconds,
                    request.MaxOutputChars),
                ct)
            .ConfigureAwait(false);

        return result.Map(r =>
        {
            logger.LogInformation("Tool {ToolName} completed for {Command} with exit code {ExitCode}", Name, r.Command, r.ExitCode);

            var text = BuildSummary(r);

            return new CallToolResult(
                [new ContentItem("text", text)],
                StructuredContent: new
                {
                    r.Command,
                    r.Arguments,
                    r.WorkingDirectory,
                    r.ExitCode,
                    r.StandardOutput,
                    r.StandardError,
                    r.TimedOut,
                    r.OutputTruncated
                },
                IsError: r.TimedOut || r.ExitCode != 0);
        });
    }

    private static string BuildSummary(ProcessExecutionResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Command: {result.Command}");
        builder.AppendLine($"Arguments: {string.Join(' ', result.Arguments)}");
        builder.AppendLine($"WorkingDirectory: {result.WorkingDirectory}");
        builder.AppendLine($"ExitCode: {result.ExitCode}");
        builder.AppendLine($"TimedOut: {result.TimedOut}");

        if (result.OutputTruncated)
        {
            builder.AppendLine("OutputTruncated: true");
        }

        builder.AppendLine();
        builder.AppendLine("stdout:");
        builder.AppendLine(string.IsNullOrWhiteSpace(result.StandardOutput) ? "<empty>" : result.StandardOutput);
        builder.AppendLine();
        builder.AppendLine("stderr:");
        builder.Append(string.IsNullOrWhiteSpace(result.StandardError) ? "<empty>" : result.StandardError);
        return builder.ToString();
    }
}