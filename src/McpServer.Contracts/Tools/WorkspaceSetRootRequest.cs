using System.Text.Json.Serialization;

namespace McpServer.Contracts.Tools;

public sealed record WorkspaceSetRootRequest(
    [property: JsonPropertyName("path")] string Path);
