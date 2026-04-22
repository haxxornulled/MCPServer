using McpServer.Application.Mcp.Tools;
using McpServer.Contracts.Tools;
using McpServer.Infrastructure.Files;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace McpServer.UnitTests.Application;

public sealed class WorkspaceSetRootToolHandlerTests
{
    [Fact]
    public async Task Handle_Should_Replace_Workspace_Root_And_Reset_Project_Root()
    {
        using var originalWorkspace = new TempWorkspace("mcpserver-workspace-set-root-original");
        using var newWorkspace = new TempWorkspace("mcpserver-workspace-set-root-new");
        Directory.CreateDirectory(Path.Combine(newWorkspace.Root, "src"));

        var pathPolicy = new PathPolicy([originalWorkspace.Root]);
        var resourceTranslator = new ResourcePathTranslator(originalWorkspace.Root);
        var changeFeed = new WorkspaceChangeFeed();

        var handler = new WorkspaceSetRootToolHandler(
            pathPolicy,
            resourceTranslator,
            Substitute.For<ILogger<WorkspaceSetRootToolHandler>>(),
            changeFeed);

        var result = await handler.Handle(new WorkspaceSetRootRequest(newWorkspace.Root), CancellationToken.None);

        Assert.True(result.IsSucc);
        var dto = result.Match(
            Succ: value => value,
            Fail: error => throw new InvalidOperationException(error.Message));

        var structured = Assert.IsType<WorkspaceSetRootResult>(dto.StructuredContent);
        Assert.Equal(newWorkspace.Root, structured.WorkspaceRoot);
        Assert.Equal(newWorkspace.Root, structured.ProjectRoot);
        Assert.True(structured.Changed);

        var normalizedProjectPath = pathPolicy.NormalizeAndValidateReadPath("src");
        Assert.True(normalizedProjectPath.IsSucc);
        Assert.Equal(
            Path.Combine(newWorkspace.Root, "src"),
            normalizedProjectPath.Match(Succ: value => value, Fail: error => throw new InvalidOperationException(error.Message)));

        var translatedWorkspacePath = resourceTranslator.TryTranslateToLocalPath("dir:///workspace/src");
        Assert.True(translatedWorkspacePath.IsSucc);
        Assert.Equal(
            Path.Combine(newWorkspace.Root, "src"),
            translatedWorkspacePath.Match(Succ: value => value, Fail: error => throw new InvalidOperationException(error.Message)));

        var changes = changeFeed.GetRecentChanges();
        Assert.Single(changes);
        Assert.Equal("set_workspace_root", changes[0].Operation);
        Assert.Equal(newWorkspace.Root, changes[0].Path);
    }

    [Fact]
    public async Task Handle_Should_Return_Error_When_Directory_Does_Not_Exist()
    {
        using var workspace = new TempWorkspace("mcpserver-workspace-set-root-missing");
        var missing = Path.Combine(workspace.Root, "missing");

        var handler = new WorkspaceSetRootToolHandler(
            new PathPolicy([workspace.Root]),
            new ResourcePathTranslator(workspace.Root),
            Substitute.For<ILogger<WorkspaceSetRootToolHandler>>());

        var result = await handler.Handle(new WorkspaceSetRootRequest(missing), CancellationToken.None);

        Assert.True(result.IsFail);
    }

    private sealed class TempWorkspace : IDisposable
    {
        public string Root { get; }

        public TempWorkspace(string prefix)
        {
            Root = Path.Combine(Path.GetTempPath(), prefix, Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, recursive: true);
                }
            }
            catch
            {
            }
        }
    }
}
