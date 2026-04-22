using System.Text.Json.Serialization;

namespace McpServer.Contracts.Tools;

public sealed record WorkspaceSelectFolderRequest(
    [property: JsonPropertyName("path")] string? Path = null);
