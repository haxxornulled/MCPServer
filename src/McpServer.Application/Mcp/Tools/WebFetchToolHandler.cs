using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Application.Abstractions.Web;
using McpServer.Application.Web.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools
{
    public sealed class WebFetchToolHandler(
        IWebAccessService webAccessService,
        ILogger<WebFetchToolHandler> logger) : IToolHandler<WebFetchUrlRequest>
    {
        public string Name => "web.fetch_url";
        public string Description => "Fetches content from a URL.";

        public JsonElement GetInputSchema() =>
            JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    url = new { type = "string" },
                    timeoutSeconds = new { type = "integer", @default = 30, minimum = 1, maximum = 300 },
                    extractReadableText = new { type = "boolean" },
                    maxBytes = new { type = "integer" }
                },
                required = new[] { "url" }
            });

        public async ValueTask<Fin<CallToolResult>> Handle(WebFetchUrlRequest request, CancellationToken ct)
        {
            var result = await webAccessService
                .FetchUrlAsync(new FetchUrlCommand(
                    request.Url,
                    request.ExtractReadableText,
                    request.MaxBytes,
                    request.TimeoutSeconds), ct)
                .ConfigureAwait(false);

            return result.Map(fetchResult =>
            {
                logger.LogInformation("Tool {ToolName} completed for URL {Url}", Name, request.Url);
                
                var text = $"Title: {fetchResult.Title}\nURL: {fetchResult.Url}\nContent: {fetchResult.Content}";

                return new CallToolResult(
                [
                    new ContentItem("text", text)
                ], 
                StructuredContent: fetchResult);
            });
        }
    }
}
