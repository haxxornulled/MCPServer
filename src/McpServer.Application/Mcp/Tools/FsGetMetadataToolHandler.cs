using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Files;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools
{
    public sealed class FsGetMetadataToolHandler(
        IFileSystemService fileSystemService,
        ILogger<FsGetMetadataToolHandler> logger) : IToolHandler<FsGetMetadataRequest>
    {
        public string Name => "fs.get_metadata";
        public string Description => "Gets metadata for a file or directory.";

        public JsonElement GetInputSchema() =>
            JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string" }
                },
                required = new[] { "path" }
            });

        public async ValueTask<Fin<CallToolResult>> Handle(FsGetMetadataRequest request, CancellationToken ct)
        {
            var result = await fileSystemService
                .GetMetadataAsync(new GetMetadataCommand(request.Path), ct)
                .ConfigureAwait(false);

            return result.Map(metadataResult =>
            {
                logger.LogInformation("Tool {ToolName} completed", Name);
                
                var content = JsonSerializer.Serialize(metadataResult, new JsonSerializerOptions { WriteIndented = true });
                
                return new CallToolResult(
                [
                    new ContentItem("text", content)
                ], 
                StructuredContent: metadataResult);
            });
        }
    }
}
