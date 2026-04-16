using McpServer.Contracts.Lifecycle;
using McpServer.Protocol.Lifecycle;
using McpServer.Protocol.Session;
using Xunit;

namespace McpServer.UnitTests.Protocol;

public sealed class InitializeHandlerTests
{
    [Fact]
    public void Handle_Should_Return_Typed_InitializeResultDto()
    {
        var provider = new CapabilityProvider();
        var handler = new InitializeHandler(provider);
        var session = new McpSession();

        var request = new InitializeRequestDto(
            ProtocolVersion: "2025-03-26",
            Capabilities: new ClientCapabilitiesDto(),
            ClientInfo: new ClientInfoDto("xunit", "1.0.0"));

        var result = handler.Handle(request, session);

        Assert.True(result.IsSucc);
        var dto = result.Match(
            Succ: value => value,
            Fail: error => throw new InvalidOperationException(error.Message));
        Assert.Equal("2025-03-26", dto.ProtocolVersion);
        Assert.Equal("McpServer.FileSystem", dto.ServerInfo.Name);
    }
}
