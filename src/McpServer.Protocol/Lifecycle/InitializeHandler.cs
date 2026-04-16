using LanguageExt;
using LanguageExt.Common;
using McpServer.Contracts.Lifecycle;
using McpServer.Protocol.Session;

namespace McpServer.Protocol.Lifecycle;

public sealed class InitializeHandler(CapabilityProvider capabilityProvider)
{
    private static readonly System.Collections.Generic.HashSet<string> SupportedProtocolVersions =
    [
        "2025-03-26",
        "2024-11-05"
    ];

    public Fin<InitializeResultDto> Handle(InitializeRequestDto request, McpSession session)
    {
        if (!SupportedProtocolVersions.Contains(request.ProtocolVersion))
        {
            return Error.New($"Unsupported protocol version: {request.ProtocolVersion}");
        }

        var init = session.CompleteInitialize(request.ProtocolVersion, request.Capabilities);
        if (init.IsFail)
        {
            return init.Match<Fin<InitializeResultDto>>(
                Succ: _ => throw new InvalidOperationException("Expected session initialization to fail."),
                Fail: error => error);
        }

        return new InitializeResultDto(
            ProtocolVersion: request.ProtocolVersion,
            Capabilities: capabilityProvider.GetCapabilities(),
            ServerInfo: new ServerInfoDto(Name: "McpServer.FileSystem", Version: "0.1.0"));
    }
}
