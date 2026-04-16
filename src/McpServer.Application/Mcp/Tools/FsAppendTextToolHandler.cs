using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Files;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools;

public sealed class FsAppendTextToolHandler(
    IFileSystemService fileSystemService,
    ILogger<FsAppendTextToolHandler> logger) : IToolHandler<AppendFileTextRequest>
{
    public string Name => "fs.append_text";
    public string Description => "Appends text to a file within allowed roots.";

    public JsonElement GetInputSchema() =>
        JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                path = new { type = "string" },
                content = new { type = "string" },
                encoding = new { type = "string" },
                flush = new { type = "boolean", @default = false }
            },
            required = new[] { "path", "content" }
        });

    public async ValueTask<Fin<CallToolResult>> Handle(AppendFileTextRequest request, CancellationToken ct)
    {
        var result = await fileSystemService
            .AppendTextAsync(new AppendFileTextCommand(request.Path, request.Content, request.Encoding, request.Flush), ct)
            .ConfigureAwait(false);

        return result.Map(_ =>
        {
            logger.LogInformation("Tool {ToolName} completed", Name);
            return new CallToolResult(
            [
                new ContentItem("text", $"Appended file: {request.Path}")
            ]);
        });
    }
}
