using System.Net.Http.Headers;
using System.Text;
using LanguageExt;
using LanguageExt.Common;
using McpServer.Application.Abstractions.Web;
using McpServer.Application.Web.Commands;
using McpServer.Application.Web.Results;
using Microsoft.Extensions.Logging;

namespace McpServer.Infrastructure.Web;

public sealed class WebAccessService(
    IHttpClientFactory httpClientFactory,
    IWebPolicy webPolicy,
    ILogger<WebAccessService> logger) : IWebAccessService
{
    public async ValueTask<Fin<FetchedPageResult>> FetchUrlAsync(FetchUrlCommand command, CancellationToken ct)
    {
        var validated = webPolicy.ValidateUrl(command.Url);
        if (validated.IsFail)
        {
            return validated.Match<Fin<FetchedPageResult>>(
                Succ: _ => throw new InvalidOperationException("Expected URL validation to fail."),
                Fail: error => error);
        }

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(command.Timeout ?? webPolicy.DefaultTimeout);

            var client = httpClientFactory.CreateClient("web-access");

            using var request = new HttpRequestMessage(HttpMethod.Get, command.Url);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("McpServer", "0.1.0"));

            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                linkedCts.Token).ConfigureAwait(false);

            var contentType = response.Content.Headers.ContentType?.MediaType;
            var maxBytes = command.MaxBytes ?? webPolicy.MaxResponseBytes;

            await using var stream = await response.Content.ReadAsStreamAsync(linkedCts.Token).ConfigureAwait(false);
            using var ms = new MemoryStream();

            var buffer = new byte[16 * 1024];
            int read;
            int total = 0;

            while ((read = await stream.ReadAsync(buffer, linkedCts.Token).ConfigureAwait(false)) > 0)
            {
                total += read;
                if (total > maxBytes)
                {
                    return Error.New($"Response exceeded max allowed bytes: {maxBytes}");
                }

                await ms.WriteAsync(buffer.AsMemory(0, read), linkedCts.Token).ConfigureAwait(false);
            }

            var body = Encoding.UTF8.GetString(ms.ToArray());

            string? text = null;
            string? rawBody = null;
            string? title = null;
            IReadOnlyList<string> links = Array.Empty<string>();

            if (contentType?.Contains("html", StringComparison.OrdinalIgnoreCase) == true)
            {
                title = HtmlTextExtractor.ExtractTitle(body);
                links = HtmlTextExtractor.ExtractLinks(body, new Uri(command.Url));

                if (command.ExtractReadableText)
                {
                    text = HtmlTextExtractor.ExtractReadableText(body);
                }
                else
                {
                    rawBody = body;
                }
            }
            else
            {
                rawBody = body;
            }

            logger.LogInformation(
                "Fetched URL {Url} status {StatusCode} contentType {ContentType}",
                command.Url,
                (int)response.StatusCode,
                contentType);

            return new FetchedPageResult(
                Url: command.Url,
                StatusCode: (int)response.StatusCode,
                ContentType: contentType,
                Title: title,
                Text: text,
                RawBody: rawBody,
                Links: links);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed fetching URL {Url}", command.Url);
            return Error.New(ex.Message);
        }
    }

    public async ValueTask<Fin<IReadOnlyList<SearchHitResult>>> SearchWebAsync(SearchWebCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Query))
        {
            return Error.New("Search query is required.");
        }

        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(webPolicy.DefaultTimeout);

            var client = httpClientFactory.CreateClient("web-search");

            var requestUri = $"https://duckduckgo.com/html/?q={Uri.EscapeDataString(command.Query)}";
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("McpServer", "0.1.0"));

            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                linkedCts.Token).ConfigureAwait(false);

            var body = await response.Content.ReadAsStringAsync(linkedCts.Token).ConfigureAwait(false);
            var text = HtmlTextExtractor.ExtractReadableText(body);

            IReadOnlyList<SearchHitResult> results =
            [
                new SearchHitResult(
                    Title: $"Search results for: {command.Query}",
                    Url: requestUri,
                    Snippet: text.Length > 300 ? text[..300] : text)
            ];

            logger.LogInformation("Executed web search for query {Query}", command.Query);

            return results.Take(Math.Max(1, command.MaxResults)).ToArray();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed searching web for query {Query}", command.Query);
            return Error.New(ex.Message);
        }
    }
}
