using System.Text.Json.Serialization;

namespace McpServer.Contracts.Tools
{
    public record SshWriteTextRequest(
        [property: JsonPropertyName("profile")] string Profile,
        [property: JsonPropertyName("path")] string Path,
        [property: JsonPropertyName("content")] string Content);
}