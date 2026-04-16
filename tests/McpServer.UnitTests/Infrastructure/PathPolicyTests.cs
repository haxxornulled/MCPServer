using McpServer.Infrastructure.Files;
using Xunit;

namespace McpServer.UnitTests.Infrastructure;

public sealed class PathPolicyTests
{
    [Fact]
    public void NormalizeAndValidateWritePath_Should_Map_Relative_Path_Into_Workspace()
    {
        var workspace = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "mcpserver-pathpolicy-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(workspace);

        var sut = new PathPolicy([workspace]);

        var result = sut.NormalizeAndValidateWritePath("smoke.txt");

        Assert.True(
            result.IsSucc,
            result.Match(
                Succ: value => value,
                Fail: error => error.Message));
        var normalized = result.Match(
            Succ: value => value,
            Fail: error => throw new InvalidOperationException(error.Message));
        Assert.Equal(Path.Combine(workspace, "smoke.txt"), normalized);
    }

    [Fact]
    public void NormalizeAndValidateWritePath_Should_Treat_Workspace_Prefix_As_Virtual_Root()
    {
        var workspace = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "mcpserver-pathpolicy-tests", Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(workspace);

        var sut = new PathPolicy([workspace]);

        var result = sut.NormalizeAndValidateWritePath("./workspace/nested/file.txt");

        Assert.True(
            result.IsSucc,
            result.Match(
                Succ: value => value,
                Fail: error => error.Message));
        var normalized = result.Match(
            Succ: value => value,
            Fail: error => throw new InvalidOperationException(error.Message));
        Assert.Equal(Path.Combine(workspace, "nested", "file.txt"), normalized);
    }
}