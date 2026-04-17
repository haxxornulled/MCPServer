using System.Text.Json.Serialization;

namespace McpServer.Contracts.Tools
{
    public record FsGetMetadataRequest(
        [property: JsonPropertyName("path")] string Path);
}