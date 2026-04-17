using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Files;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools
{
    public sealed class FsMovePathToolHandler(
        IFileSystemService fileSystemService,
        ILogger<FsMovePathToolHandler> logger) : IToolHandler<FsMovePathRequest>
    {
        public string Name => "fs.move_path";
        public string Description => "Moves a file or directory.";

        public JsonElement GetInputSchema() =>
            JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    source_path = new { type = "string" },
                    destination_path = new { type = "string" },
                    overwrite = new { type = "boolean" }
                },
                required = new[] { "source_path", "destination_path" }
            });

        public async ValueTask<Fin<CallToolResult>> Handle(FsMovePathRequest request, CancellationToken ct)
        {
            var result = await fileSystemService
                .MovePathAsync(new MovePathCommand(request.SourcePath, request.DestinationPath, request.Overwrite), ct)
                .ConfigureAwait(false);

            return result.Map(_ =>
            {
                logger.LogInformation("Tool {ToolName} completed", Name);
                return new CallToolResult(
                [
                    new ContentItem("text", $"Successfully moved path from {request.SourcePath} to {request.DestinationPath}")
                ]);
            });
        }
    }
}
