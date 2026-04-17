using System.Text.Json.Serialization;

namespace McpServer.Contracts.Tools
{
    public record FsListDirectoryRequest(
        [property: JsonPropertyName("path")] string Path,
        [property: JsonPropertyName("search_pattern")] string? SearchPattern = null);
}