using System.Text.Json.Serialization;

namespace McpServer.Contracts.Tools;

public sealed record WebFetchUrlRequest(
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("extractReadableText")] bool ExtractReadableText = true,
    [property: JsonPropertyName("maxBytes")] int? MaxBytes = null,
    [property: JsonPropertyName("timeoutSeconds")] int? TimeoutSeconds = null);
