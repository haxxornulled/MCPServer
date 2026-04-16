using System.Text.Json.Serialization;

namespace McpServer.Contracts.Tools;

public sealed record SshWriteTextRequest(
    [property: JsonPropertyName("profile")] string Profile,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("overwrite")] bool Overwrite = true,
    [property: JsonPropertyName("createDirectories")] bool CreateDirectories = true,
    [property: JsonPropertyName("encoding")] string Encoding = "utf-8",
    [property: JsonPropertyName("permissions")] string? Permissions = null);