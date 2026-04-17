using LanguageExt;
using McpServer.Application.Abstractions.Ssh;
using McpServer.Application.Mcp.Tools;
using McpServer.Application.Ssh.Commands;
using McpServer.Application.Ssh.Results;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace McpServer.UnitTests.Application;

public sealed class SshExecToolHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Structured_Result_With_Command_Output()
    {
        var ssh = Substitute.For<ISshService>();
        var logger = Substitute.For<ILogger<SshExecuteToolHandler>>();

        ssh.ExecuteAsync(Arg.Any<ExecuteSshCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Fin<SshCommandResult>>(new SshCommandResult(
                Profile: "prod-web",
                Host: "10.0.0.10",
                Port: 22,
                Username: "deploy",
                Command: "nginx -v",
                WorkingDirectory: "/etc/nginx",
                ExitCode: 0,
                StandardOutput: "nginx version: nginx/1.26.0",
                StandardError: string.Empty,
                TimedOut: false,
                OutputTruncated: false)));

        var handler = new SshExecuteToolHandler(ssh, logger);
        var result = await handler.Handle(new SshExecuteRequest("prod-web", "nginx -v"), CancellationToken.None);

        Assert.True(result.IsSucc);

        var dto = result.Match(
            Succ: value => value,
            Fail: error => throw new InvalidOperationException(error.Message));

        Assert.Contains(dto.Content, item => item.Text.Contains("\"Profile\": \"prod-web\"", StringComparison.Ordinal));
        Assert.False(dto.IsError);
        Assert.NotNull(dto.StructuredContent);
    }
}
