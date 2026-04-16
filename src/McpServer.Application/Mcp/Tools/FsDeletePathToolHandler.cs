using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Files;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools;

public sealed class FsDeletePathToolHandler(
    IFileSystemService fileSystemService,
    ILogger<FsDeletePathToolHandler> logger) : IToolHandler<DeletePathRequest>
{
    public string Name => "fs.delete_path";
    public string Description => "Deletes a file or directory within allowed roots.";

    public JsonElement GetInputSchema() =>
        JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                path = new { type = "string" },
                recursive = new { type = "boolean", @default = false }
            },
            required = new[] { "path" }
        });

    public async ValueTask<Fin<CallToolResult>> Handle(DeletePathRequest request, CancellationToken ct)
    {
        var result = await fileSystemService
            .DeletePathAsync(new DeletePathCommand(request.Path, request.Recursive), ct)
            .ConfigureAwait(false);

        return result.Map(_ =>
        {
            logger.LogInformation("Tool {ToolName} completed", Name);
            return new CallToolResult(
            [
                new ContentItem("text", $"Deleted path: {request.Path}")
            ]);
        });
    }
}
