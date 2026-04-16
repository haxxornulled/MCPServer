using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Files;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools;

public sealed class FsWriteTextToolHandler(
    IFileSystemService fileSystemService,
    ILogger<FsWriteTextToolHandler> logger) : IToolHandler<WriteFileTextRequest>
{
    public string Name => "fs.write_text";
    public string Description => "Writes text to a file within allowed roots.";

    public JsonElement GetInputSchema() =>
        JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                path = new { type = "string" },
                content = new { type = "string" },
                encoding = new { type = "string" },
                overwrite = new { type = "boolean", @default = true },
                flush = new { type = "boolean", @default = false }
            },
            required = new[] { "path", "content" }
        });

    public async ValueTask<Fin<CallToolResult>> Handle(WriteFileTextRequest request, CancellationToken ct)
    {
        var result = await fileSystemService
            .WriteTextAsync(new WriteFileTextCommand(request.Path, request.Content, request.Encoding, request.Overwrite, request.Flush), ct)
            .ConfigureAwait(false);

        return result.Map(_ =>
        {
            logger.LogInformation("Tool {ToolName} completed", Name);
            return new CallToolResult(
            [
                new ContentItem("text", $"Wrote file: {request.Path}")
            ]);
        });
    }
}
