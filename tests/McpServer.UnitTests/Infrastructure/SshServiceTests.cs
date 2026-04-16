using McpServer.Infrastructure.Ssh;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace McpServer.UnitTests.Infrastructure;

public sealed class SshServiceTests
{
    [Fact]
    public async Task ExecuteAsync_Should_Fail_For_Unknown_Profile_Before_Attempting_Connection()
    {
        var logger = Substitute.For<ILogger<SshService>>();
        var sut = new SshService(Array.Empty<ConfiguredSshProfile>(), AppContext.BaseDirectory, logger);

        var result = await sut.ExecuteAsync(new("missing", "hostname", null, 30, 4096), CancellationToken.None);

        Assert.True(result.IsFail);
        var error = result.Match(
            Succ: _ => throw new InvalidOperationException("Expected SSH execution to fail for an unknown profile."),
            Fail: failure => failure.Message);
        Assert.Contains("Unknown SSH profile", error, StringComparison.Ordinal);
    }
}