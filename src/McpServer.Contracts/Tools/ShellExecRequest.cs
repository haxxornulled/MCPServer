using System.Text.Json.Serialization;

namespace McpServer.Contracts.Tools;

public sealed record ShellExecRequest(
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("args")] string[]? Args = null,
    [property: JsonPropertyName("workingDirectory")] string? WorkingDirectory = null,
    [property: JsonPropertyName("timeoutSeconds")] int TimeoutSeconds = 60,
    [property: JsonPropertyName("maxOutputChars")] int MaxOutputChars = 12000);