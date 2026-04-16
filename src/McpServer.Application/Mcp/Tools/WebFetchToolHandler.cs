using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Application.Abstractions.Web;
using McpServer.Application.Web.Commands;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;

namespace McpServer.Application.Mcp.Tools;

public sealed class WebFetchToolHandler(
    IWebAccessService webAccessService,
    ILogger<WebFetchToolHandler> logger) : IToolHandler<WebFetchUrlRequest>
{
    public string Name => "web.fetch_url";
    public string Description => "Fetches a URL and returns readable text or raw content.";

    public JsonElement GetInputSchema() =>
        JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                url = new { type = "string" },
                extractReadableText = new { type = "boolean", @default = true },
                maxBytes = new { type = "integer" },
                timeoutSeconds = new { type = "integer" }
            },
            required = new[] { "url" }
        });

    public async ValueTask<Fin<CallToolResult>> Handle(WebFetchUrlRequest request, CancellationToken ct)
    {
        var result = await webAccessService
            .FetchUrlAsync(
                new FetchUrlCommand(
                    request.Url,
                    request.ExtractReadableText,
                    request.MaxBytes,
                    request.TimeoutSeconds is { } s ? TimeSpan.FromSeconds(s) : null),
                ct)
            .ConfigureAwait(false);

        return result.Map(r =>
        {
            var summary = $"URL: {r.Url}\nStatus: {r.StatusCode}\nContent-Type: {r.ContentType ?? "unknown"}\nTitle: {r.Title ?? "n/a"}\n\n{r.Text ?? r.RawBody ?? string.Empty}";

            logger.LogInformation("Tool {ToolName} completed for {Url}", Name, request.Url);

            return new CallToolResult(
                [new ContentItem("text", summary)],
                StructuredContent: new { r.Url, r.StatusCode, r.ContentType, r.Title, r.Links });
        });
    }
}
