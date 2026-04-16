using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Files;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools;

public sealed class FsCopyPathToolHandler(
    IFileSystemService fileSystemService,
    ILogger<FsCopyPathToolHandler> logger) : IToolHandler<CopyPathRequest>
{
    public string Name => "fs.copy_path";
    public string Description => "Copies a file or directory within allowed roots.";

    public JsonElement GetInputSchema() =>
        JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                sourcePath = new { type = "string" },
                destinationPath = new { type = "string" },
                overwrite = new { type = "boolean", @default = false },
                recursive = new { type = "boolean", @default = false }
            },
            required = new[] { "sourcePath", "destinationPath" }
        });

    public async ValueTask<Fin<CallToolResult>> Handle(CopyPathRequest request, CancellationToken ct)
    {
        var result = await fileSystemService
            .CopyPathAsync(new CopyPathCommand(request.SourcePath, request.DestinationPath, request.Overwrite, request.Recursive), ct)
            .ConfigureAwait(false);

        return result.Map(_ =>
        {
            logger.LogInformation("Tool {ToolName} completed", Name);
            return new CallToolResult(
            [
                new ContentItem("text", $"Copied path: {request.SourcePath} -> {request.DestinationPath}")
            ]);
        });
    }
}
