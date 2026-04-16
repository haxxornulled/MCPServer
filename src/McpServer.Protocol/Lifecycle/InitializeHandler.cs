using LanguageExt;
using McpServer.Contracts.Lifecycle;
using McpServer.Protocol.Session;

namespace McpServer.Protocol.Lifecycle;

public sealed class InitializeHandler(CapabilityProvider capabilityProvider)
{
    private const string CompatibleFallbackProtocolVersion = "2025-03-26";

    private static readonly string[] SupportedProtocolVersions =
    [
        "2025-11-25",
        "2025-03-26",
        "2024-11-05"
    ];

    public Fin<InitializeResultDto> Handle(InitializeRequestDto request, McpSession session)
    {
        var negotiatedProtocolVersion = NegotiateProtocolVersion(request.ProtocolVersion);

        var init = session.CompleteInitialize(negotiatedProtocolVersion, request.Capabilities);
        if (init.IsFail)
        {
            return init.Match<Fin<InitializeResultDto>>(
                Succ: _ => throw new InvalidOperationException("Expected session initialization to fail."),
                Fail: error => error);
        }

        return new InitializeResultDto(
            ProtocolVersion: negotiatedProtocolVersion,
            Capabilities: capabilityProvider.GetCapabilities(),
            ServerInfo: new ServerInfoDto(Name: "McpServer.FileSystem", Version: "0.1.1"));
    }

    private static string NegotiateProtocolVersion(string requestedProtocolVersion)
    {
        foreach (var supportedVersion in SupportedProtocolVersions)
        {
            if (string.Equals(supportedVersion, requestedProtocolVersion, StringComparison.Ordinal))
            {
                return supportedVersion;
            }
        }

        return CompatibleFallbackProtocolVersion;
    }
}
