using System.Text.Json.Serialization;

namespace McpServer.Contracts.Tools;

public sealed record CreateDirectoryRequest(
    [property: JsonPropertyName("path")] string Path);
