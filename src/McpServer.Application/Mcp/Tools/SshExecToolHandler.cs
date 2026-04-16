using System.Text;
using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Application.Abstractions.Ssh;
using McpServer.Application.Ssh.Commands;
using McpServer.Application.Ssh.Results;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools;

public sealed class SshExecToolHandler(
    ISshService sshService,
    ILogger<SshExecToolHandler> logger) : IToolHandler<SshExecRequest>
{
    public string Name => "ssh.exec";
    public string Description => "Runs a non-interactive shell command on a configured SSH host profile.";

    public JsonElement GetInputSchema() =>
        JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                profile = new { type = "string" },
                command = new { type = "string" },
                workingDirectory = new { type = "string" },
                timeoutSeconds = new { type = "integer", @default = 60, minimum = 1, maximum = 1800 },
                maxOutputChars = new { type = "integer", @default = 12000, minimum = 256, maximum = 200000 }
            },
            required = new[] { "profile", "command" }
        });

    public async ValueTask<Fin<CallToolResult>> Handle(SshExecRequest request, CancellationToken ct)
    {
        var result = await sshService.ExecuteAsync(
                new ExecuteSshCommand(
                    request.Profile,
                    request.Command,
                    request.WorkingDirectory,
                    request.TimeoutSeconds,
                    request.MaxOutputChars),
                ct)
            .ConfigureAwait(false);

        return result.Map(commandResult =>
        {
            logger.LogInformation(
                "Tool {ToolName} completed for SSH profile {Profile} command {Command} with exit code {ExitCode}",
                Name,
                commandResult.Profile,
                commandResult.Command,
                commandResult.ExitCode);

            return new CallToolResult(
                [new ContentItem("text", BuildSummary(commandResult))],
                StructuredContent: new
                {
                    commandResult.Profile,
                    commandResult.Host,
                    commandResult.Port,
                    commandResult.Username,
                    commandResult.Command,
                    commandResult.WorkingDirectory,
                    commandResult.ExitCode,
                    commandResult.StandardOutput,
                    commandResult.StandardError,
                    commandResult.TimedOut,
                    commandResult.OutputTruncated
                },
                IsError: commandResult.TimedOut || commandResult.ExitCode != 0);
        });
    }

    private static string BuildSummary(SshCommandResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Profile: {result.Profile}");
        builder.AppendLine($"Host: {result.Username}@{result.Host}:{result.Port}");
        builder.AppendLine($"WorkingDirectory: {result.WorkingDirectory}");
        builder.AppendLine($"Command: {result.Command}");
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