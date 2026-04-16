using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Application.Abstractions.Ssh;
using McpServer.Application.Ssh.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools;

public sealed class SshWriteTextToolHandler(
    ISshService sshService,
    ILogger<SshWriteTextToolHandler> logger) : IToolHandler<SshWriteTextRequest>
{
    public string Name => "ssh.write_text";
    public string Description => "Writes a text file to a configured SSH host profile over SFTP.";

    public JsonElement GetInputSchema() =>
        JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                profile = new { type = "string" },
                path = new { type = "string" },
                content = new { type = "string" },
                overwrite = new { type = "boolean", @default = true },
                createDirectories = new { type = "boolean", @default = true },
                encoding = new { type = "string", @default = "utf-8" },
                permissions = new { type = "string", description = "Optional octal file mode such as 644 or 600." }
            },
            required = new[] { "profile", "path", "content" }
        });

    public async ValueTask<Fin<CallToolResult>> Handle(SshWriteTextRequest request, CancellationToken ct)
    {
        var result = await sshService.WriteTextAsync(
                new WriteSshTextCommand(
                    request.Profile,
                    request.Path,
                    request.Content,
                    request.Overwrite,
                    request.CreateDirectories,
                    request.Encoding,
                    request.Permissions),
                ct)
            .ConfigureAwait(false);

        return result.Map(writeResult =>
        {
            logger.LogInformation(
                "Tool {ToolName} wrote remote file {Path} via SSH profile {Profile}",
                Name,
                writeResult.Path,
                writeResult.Profile);

            return new CallToolResult(
                [new ContentItem("text", $"Wrote remote file {writeResult.Path} on {writeResult.Username}@{writeResult.Host}:{writeResult.Port}")],
                StructuredContent: new
                {
                    writeResult.Profile,
                    writeResult.Host,
                    writeResult.Port,
                    writeResult.Username,
                    writeResult.Path,
                    writeResult.BytesWritten,
                    writeResult.Overwritten,
                    writeResult.CreatedDirectories,
                    writeResult.PermissionsApplied
                });
        });
    }
}