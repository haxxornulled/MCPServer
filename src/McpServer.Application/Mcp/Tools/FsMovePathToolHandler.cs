using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Files;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools;

public sealed class FsMovePathToolHandler(
    IFileSystemService fileSystemService,
    ILogger<FsMovePathToolHandler> logger) : IToolHandler<MovePathRequest>
{
    public string Name => "fs.move_path";
    public string Description => "Moves a file or directory within allowed roots.";

    public JsonElement GetInputSchema() =>
        JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                sourcePath = new { type = "string" },
                destinationPath = new { type = "string" },
                overwrite = new { type = "boolean", @default = false }
            },
            required = new[] { "sourcePath", "destinationPath" }
        });

    public async ValueTask<Fin<CallToolResult>> Handle(MovePathRequest request, CancellationToken ct)
    {
        var result = await fileSystemService
            .MovePathAsync(new MovePathCommand(request.SourcePath, request.DestinationPath, request.Overwrite), ct)
            .ConfigureAwait(false);

        return result.Map(_ =>
        {
            logger.LogInformation("Tool {ToolName} completed", Name);
            return new CallToolResult(
            [
                new ContentItem("text", $"Moved path: {request.SourcePath} -> {request.DestinationPath}")
            ]);
        });
    }
}
