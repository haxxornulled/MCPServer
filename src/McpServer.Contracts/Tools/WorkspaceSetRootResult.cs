using System.Text.Json.Serialization;

namespace McpServer.Contracts.Tools;

public sealed record WorkspaceSetRootResult(
    [property: JsonPropertyName("workspaceRoot")] string WorkspaceRoot,
    [property: JsonPropertyName("projectRoot")] string ProjectRoot,
    [property: JsonPropertyName("changed")] bool Changed);
