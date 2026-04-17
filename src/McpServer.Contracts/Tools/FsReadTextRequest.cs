using System.Text.Json.Serialization;

namespace McpServer.Contracts.Tools
{
    public record FsReadTextRequest(
        [property: JsonPropertyName("path")] string Path,
        [property: JsonPropertyName("encoding")] string? Encoding = null);
}