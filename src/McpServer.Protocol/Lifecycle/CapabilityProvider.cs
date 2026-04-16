using McpServer.Contracts.Lifecycle;

namespace McpServer.Protocol.Lifecycle;

public sealed class CapabilityProvider
{
    public ServerCapabilitiesDto GetCapabilities() =>
        new(
            Tools: new ToolsCapabilityDto(ListChanged: false),
            Resources: new ResourcesCapabilityDto(Subscribe: false, ListChanged: false),
            Prompts: new PromptsCapabilityDto(ListChanged: false));
}
