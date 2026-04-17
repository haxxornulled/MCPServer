using LanguageExt;
using McpServer.Application.Abstractions.Execution;
using McpServer.Application.Execution.Commands;
using McpServer.Application.Execution.Results;
using McpServer.Application.Mcp.Tools;
using McpServer.Contracts.Tools;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace McpServer.UnitTests.Application;

public sealed class ShellExecToolHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Structured_Result_With_Command_Output()
    {
        var processExecution = Substitute.For<IProcessExecutionService>();
        var logger = Substitute.For<ILogger<ShellExecToolHandler>>();

        processExecution.RunAsync(Arg.Any<RunProcessCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Fin<ProcessExecutionResult>>(new ProcessExecutionResult(
                Command: "dotnet",
                Arguments: ["--version"],
                WorkingDirectory: "D:/workspace",
                ExitCode: 0,
                StandardOutput: "10.0.201",
                StandardError: string.Empty,
                TimedOut: false,
                OutputTruncated: false)));

        var handler = new ShellExecToolHandler(processExecution, logger);
        var result = await handler.Handle(new ShellExecRequest("dotnet", ["--version"]), CancellationToken.None);

        Assert.True(result.IsSucc);

        var dto = result.Match(
            Succ: value => value,
            Fail: error => throw new InvalidOperationException(error.Message));

        Assert.Contains(dto.Content, item => item.Text.Contains("Exit Code: 0", StringComparison.Ordinal));
        Assert.False(dto.IsError);
        Assert.NotNull(dto.StructuredContent);
    }
}
