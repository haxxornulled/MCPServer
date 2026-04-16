using System.Text.Json.Serialization;

namespace McpServer.Contracts.Prompts;

public sealed record ReviewDirectoryPromptArguments(
    [property: JsonPropertyName("uri")] string Uri,
    [property: JsonPropertyName("goal")] string? Goal = null);
