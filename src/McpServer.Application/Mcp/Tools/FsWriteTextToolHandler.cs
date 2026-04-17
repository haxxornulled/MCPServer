using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Files;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools
{
    public sealed class FsWriteTextToolHandler(
        IFileSystemService fileSystemService,
        ILogger<FsWriteTextToolHandler> logger) : IToolHandler<FsWriteTextRequest>
    {
        public string Name => "fs.write_text";
        public string Description => "Writes text to a file.";

        public JsonElement GetInputSchema() =>
            JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    path = new { type = "string" },
                    content = new { type = "string" },
                    encoding = new { type = "string" }
                },
                required = new[] { "path", "content" }
            });

        public async ValueTask<Fin<CallToolResult>> Handle(FsWriteTextRequest request, CancellationToken ct)
        {
            var result = await fileSystemService
                .WriteTextAsync(new WriteFileTextCommand(request.Path, request.Content, request.Encoding), ct)
                .ConfigureAwait(false);

            return result.Map(fileResult =>
            {
                logger.LogInformation("Tool {ToolName} completed", Name);
                return new CallToolResult(
                [
                    new ContentItem("text", $"Successfully wrote to file: {request.Path}")
                ], 
                StructuredContent: fileResult);
            });
        }
    }
}
