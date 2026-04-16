namespace McpServer.Application.Web.Results;

public sealed record FetchedPageResult(
    string Url,
    int StatusCode,
    string? ContentType,
    string? Title,
    string? Text,
    string? RawBody,
    IReadOnlyList<string> Links);

public sealed record SearchHitResult(
    string Title,
    string Url,
    string? Snippet);
