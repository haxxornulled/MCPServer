using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Web;
using McpServer.Application.Web.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools
{
    public sealed class WebSearchToolHandler(
        IWebAccessService webAccessService,
        ILogger<WebSearchToolHandler> logger) : IToolHandler<WebSearchRequest>
    {
        public string Name => "web.search";
        public string Description => "Searches the web for a given query.";

        public JsonElement GetInputSchema() =>
            JsonSerializer.SerializeToElement(new
            {
                type = "object",
                properties = new
                {
                    query = new { type = "string" },
                    maxResults = new { type = "integer", @default = 5, minimum = 1, maximum = 20 }
                },
                required = new[] { "query" }
            });

        public async ValueTask<Fin<CallToolResult>> Handle(WebSearchRequest request, CancellationToken ct)
        {
            var result = await webAccessService
                .SearchWebAsync(new SearchWebCommand(request.Query, request.MaxResults), ct)
                .ConfigureAwait(false);

            return result.Map(searchResults =>
            {
                logger.LogInformation("Tool {ToolName} completed", Name);

                var payload = new
                {
                    query = request.Query,
                    result_count = searchResults.Count,
                    results = searchResults.Select((item, index) => new
                    {
                        rank = index + 1,
                        title = item.Title,
                        url = item.Url,
                        snippet = item.Snippet,
                        relevance = item.Relevance
                    }).ToArray()
                };

                var content = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });

                return new CallToolResult(
                [
                    new ContentItem("text", content)
                ],
                StructuredContent: payload);
            });
        }
    }
}
