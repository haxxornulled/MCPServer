using System.Text.Json.Serialization;

namespace McpServer.Contracts.Tools
{
    public record SshExecuteRequest(
        [property: JsonPropertyName("profile")] string Profile,
        [property: JsonPropertyName("command")] string Command,
        [property: JsonPropertyName("working_directory")] string? WorkingDirectory = null);
}