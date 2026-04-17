using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Web;
using McpServer.Application.Web.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools
{
    public sealed class WebFetchUrlToolHandler(
        IWebAccessService webAccessService,
        ILogger<WebFetchUrlToolHandler> logger) : IToolHandler<WebFetchUrlRequest>
    {
        public string Name => "web.fetch_url";
        public string Description => "Fetches the content of a URL.";

        public JsonElement GetInputSchema() =>
            JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    url = new { type = "string" },
                    timeout_seconds = new { type = "integer" }
                },
                required = new[] { "url" }
            });

        public async ValueTask<Fin<CallToolResult>> Handle(WebFetchUrlRequest request, CancellationToken ct)
        {
            var result = await webAccessService
                .FetchUrlAsync(new FetchUrlCommand(
                    request.Url,
                    TimeoutSeconds: request.TimeoutSeconds), ct)
                .ConfigureAwait(false);

            return result.Map(fetchResult =>
            {
                logger.LogInformation("Tool {ToolName} completed", Name);
                
                var content = JsonSerializer.Serialize(fetchResult, new JsonSerializerOptions { WriteIndented = true });
                
                return new CallToolResult(
                [
                    new ContentItem("text", content)
                ], 
                StructuredContent: fetchResult);
            });
        }
    }
}
