using System.Text.Json.Serialization;

namespace McpServer.Contracts.Tools;

public sealed record SshExecRequest(
    [property: JsonPropertyName("profile")] string Profile,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("workingDirectory")] string? WorkingDirectory = null,
    [property: JsonPropertyName("timeoutSeconds")] int TimeoutSeconds = 60,
    [property: JsonPropertyName("maxOutputChars")] int MaxOutputChars = 12000);