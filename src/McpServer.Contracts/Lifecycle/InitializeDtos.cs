using System.Text.Json.Serialization;

namespace McpServer.Contracts.Lifecycle;

public sealed record InitializeRequestDto(
    [property: JsonPropertyName("protocolVersion")] string ProtocolVersion,
    [property: JsonPropertyName("capabilities")] ClientCapabilitiesDto? Capabilities,
    [property: JsonPropertyName("clientInfo")] ClientInfoDto? ClientInfo);

public sealed record ClientInfoDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version);

public sealed record ClientCapabilitiesDto(
    [property: JsonPropertyName("roots")] object? Roots = null,
    [property: JsonPropertyName("sampling")] object? Sampling = null,
    [property: JsonPropertyName("elicitation")] object? Elicitation = null);

public sealed record InitializeResultDto(
    [property: JsonPropertyName("protocolVersion")] string ProtocolVersion,
    [property: JsonPropertyName("capabilities")] ServerCapabilitiesDto Capabilities,
    [property: JsonPropertyName("serverInfo")] ServerInfoDto ServerInfo);

public sealed record ServerInfoDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version);

public sealed record ServerCapabilitiesDto(
    [property: JsonPropertyName("tools")] ToolsCapabilityDto? Tools,
    [property: JsonPropertyName("resources")] ResourcesCapabilityDto? Resources,
    [property: JsonPropertyName("prompts")] PromptsCapabilityDto? Prompts);

public sealed record ToolsCapabilityDto([property: JsonPropertyName("listChanged")] bool ListChanged);

public sealed record ResourcesCapabilityDto(
    [property: JsonPropertyName("subscribe")] bool Subscribe,
    [property: JsonPropertyName("listChanged")] bool ListChanged);

public sealed record PromptsCapabilityDto([property: JsonPropertyName("listChanged")] bool ListChanged);
