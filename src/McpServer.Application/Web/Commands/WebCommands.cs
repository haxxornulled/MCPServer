namespace McpServer.Application.Web.Commands;

public sealed record FetchUrlCommand(
    string Url,
    bool ExtractReadableText,
    int? MaxBytes = null,
    TimeSpan? Timeout = null);

public sealed record SearchWebCommand(
    string Query,
    int MaxResults = 5);
