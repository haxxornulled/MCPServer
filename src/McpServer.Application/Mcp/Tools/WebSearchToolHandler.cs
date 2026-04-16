using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Application.Abstractions.Web;
using McpServer.Application.Web.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools;

public sealed class WebSearchToolHandler(
    IWebAccessService webAccessService,
    ILogger<WebSearchToolHandler> logger) : IToolHandler<WebSearchRequest>
{
    public string Name => "web.search";
    public string Description => "Searches the web and returns matching result summaries.";

    public JsonElement GetInputSchema() =>
        JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                query = new { type = "string" },
                maxResults = new { type = "integer", @default = 5 }
            },
            required = new[] { "query" }
        });

    public async ValueTask<Fin<CallToolResult>> Handle(WebSearchRequest request, CancellationToken ct)
    {
        var result = await webAccessService
            .SearchWebAsync(new SearchWebCommand(request.Query, request.MaxResults), ct)
            .ConfigureAwait(false);

        return result.Map(results =>
        {
            var text = string.Join(
                Environment.NewLine + Environment.NewLine,
                results.Select((x, i) => $"{i + 1}. {x.Title}{Environment.NewLine}{x.Url}{Environment.NewLine}{x.Snippet}"));

            logger.LogInformation("Tool {ToolName} completed for query {Query}", Name, request.Query);

            return new CallToolResult([new ContentItem("text", text)], StructuredContent: results);
        });
    }
}
