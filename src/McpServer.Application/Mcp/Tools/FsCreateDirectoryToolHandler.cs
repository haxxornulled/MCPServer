using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Files;
using McpServer.Application.Files.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools
{
    public sealed class FsCreateDirectoryToolHandler(
        IFileSystemService fileSystemService,
        ILogger<FsCreateDirectoryToolHandler> logger) : IToolHandler<CreateDirectoryRequest>
    {
        public string Name => "fs.create_directory";
        public string Description => "Creates a directory.";

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

        public async ValueTask<Fin<CallToolResult>> Handle(CreateDirectoryRequest request, CancellationToken ct)
        {
            var result = await fileSystemService
                .CreateDirectoryAsync(new CreateDirectoryCommand(request.Path), ct)
                .ConfigureAwait(false);

            return result.Map(_ =>
            {
                logger.LogInformation("Tool {ToolName} completed", Name);
                return new CallToolResult(
                [
                    new ContentItem("text", $"Successfully created directory: {request.Path}")
                ]);
            });
        }
    }
}