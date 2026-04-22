using System.Text.Json;
using LanguageExt;
using McpServer.Application.Abstractions.Execution;
using McpServer.Application.Abstractions.Files;
using McpServer.Application.Abstractions.Ssh;
using McpServer.Application.Abstractions.Web;
using McpServer.Application.Execution.Commands;
using McpServer.Application.Execution.Results;
using McpServer.Application.Files.Commands;
using McpServer.Application.Files.Results;
using McpServer.Application.Mcp.Tools;
using McpServer.Application.Ssh.Commands;
using McpServer.Application.Ssh.Results;
using McpServer.Application.Web.Commands;
using McpServer.Application.Web.Results;
using McpServer.Contracts.Tools;
using McpServer.Protocol.Routing;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace McpServer.UnitTests.Protocol;

public sealed class ToolCallRouterTests
{
    [Fact]
    public void ListTools_Should_Expose_Current_LM_Studio_Tool_Names()
    {
        var router = CreateRouter();

        var result = router.ListTools();
        var names = result.Tools.Select(tool => tool.Name).ToArray();

        Assert.Equal(16, names.Length);
        Assert.Contains("fs.write_text", names);
        Assert.Contains("fs.append_text", names);
        Assert.Contains("fs.read_file", names);
        Assert.Contains("fs.list_directory", names);
        Assert.Contains("fs.create_directory", names);
        Assert.Contains("fs.move_path", names);
        Assert.Contains("fs.copy_path", names);
        Assert.Contains("fs.delete_path", names);
        Assert.Contains("workspace.set_root", names);
        Assert.Contains("workspace.select_folder", names);
        Assert.Contains("workspace.inspect", names);
        Assert.Contains("shell.exec", names);
        Assert.Contains("ssh.execute", names);
        Assert.Contains("ssh.write_text", names);
        Assert.Contains("web.fetch_url", names);
        Assert.Contains("web.search", names);
        Assert.DoesNotContain("ssh.exec", names);
        Assert.DoesNotContain("web.fetch", names);
    }

    [Fact]
    public void ListTools_Should_Use_Snake_Case_Schema_Fields_For_LM_Studio()
    {
        var router = CreateRouter();

        var result = router.ListTools();

        var sshExecute = Assert.Single(result.Tools, tool => tool.Name == "ssh.execute");
        var sshProperties = sshExecute.InputSchema.GetProperty("properties");
        Assert.True(sshProperties.TryGetProperty("working_directory", out _));

        var webFetch = Assert.Single(result.Tools, tool => tool.Name == "web.fetch_url");
        var webProperties = webFetch.InputSchema.GetProperty("properties");
        Assert.True(webProperties.TryGetProperty("timeout_seconds", out _));

        var movePath = Assert.Single(result.Tools, tool => tool.Name == "fs.move_path");
        var moveProperties = movePath.InputSchema.GetProperty("properties");
        Assert.True(moveProperties.TryGetProperty("source_path", out _));
        Assert.True(moveProperties.TryGetProperty("destination_path", out _));
    }

    [Fact]
    public void ListTools_Should_Reuse_Cached_Catalog()
    {
        var router = CreateRouter();

        var first = router.ListTools();
        var second = router.ListTools();

        Assert.Same(first.Tools, second.Tools);
    }

    [Fact]
    public async Task RouteAsync_Should_Dispatch_File_Write_And_Return_Structured_Result()
    {
        var fileSystem = Substitute.For<IFileSystemService>();
        fileSystem.WriteTextAsync(Arg.Any<WriteFileTextCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Fin<FileTextResult>>(new FileTextResult("workspace/note.txt", "hello")));

        var router = CreateRouter(fileSystemService: fileSystem);
        var arguments = JsonSerializer.SerializeToElement(new
        {
            path = "note.txt",
            content = "hello",
            encoding = "utf-8"
        });

        var result = await router.RouteAsync("fs.write_text", arguments, CancellationToken.None);

        Assert.True(result.IsSucc);
        var dto = result.Match(
            Succ: value => value,
            Fail: error => throw new InvalidOperationException(error.Message));

        Assert.False(dto.IsError);
        Assert.Single(dto.Content);
        Assert.Equal("Successfully wrote to file: note.txt", dto.Content[0].Text);
        var structured = Assert.IsType<FileTextResult>(dto.StructuredContent);
        Assert.Equal("hello", structured.Content);
    }

    [Fact]
    public async Task RouteAsync_Should_Dispatch_File_Read_And_Return_Structured_Result()
    {
        var fileSystem = Substitute.For<IFileSystemService>();
        fileSystem.ReadTextAsync(Arg.Any<ReadFileTextCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Fin<FileTextResult>>(new FileTextResult("workspace/note.txt", "hello")));

        var router = CreateRouter(fileSystemService: fileSystem);
        var arguments = JsonSerializer.SerializeToElement(new
        {
            path = "note.txt",
            encoding = "utf-8"
        });

        var result = await router.RouteAsync("fs.read_file", arguments, CancellationToken.None);

        Assert.True(result.IsSucc);
        var dto = result.Match(
            Succ: value => value,
            Fail: error => throw new InvalidOperationException(error.Message));

        Assert.False(dto.IsError);
        Assert.Single(dto.Content);
        Assert.Equal("hello", dto.Content[0].Text);
        var structured = Assert.IsType<FileTextResult>(dto.StructuredContent);
        Assert.Equal("hello", structured.Content);
    }

    [Fact]
    public async Task RouteAsync_Should_Dispatch_Directory_List_And_Return_Structured_Result()
    {
        var fileSystem = Substitute.For<IFileSystemService>();
        fileSystem.ListDirectoryAsync(Arg.Any<ListDirectoryCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValueTask<Fin<DirectoryListingResult>>(new DirectoryListingResult(
                "workspace",
                [
                    new McpServer.Application.Files.DirectoryEntry("src", true),
                    new McpServer.Application.Files.DirectoryEntry("README.md", false)
                ])));

        var router = CreateRouter(fileSystemService: fileSystem);
        var arguments = JsonSerializer.SerializeToElement(new
        {
            path = "workspace"
        });

        var result = await router.RouteAsync("fs.list_directory", arguments, CancellationToken.None);

        Assert.True(result.IsSucc);
        var dto = result.Match(
            Succ: value => value,
            Fail: error => throw new InvalidOperationException(error.Message));

        Assert.False(dto.IsError);
        Assert.Single(dto.Content);
        Assert.Contains("README.md", dto.Content[0].Text, StringComparison.Ordinal);
        var structured = Assert.IsType<DirectoryListingResult>(dto.StructuredContent);
        Assert.Contains(structured.Entries, entry => entry.Name == "src" && entry.IsDirectory);
    }

    [Fact]
    public async Task RouteAsync_Should_Dispatch_Workspace_Folder_Selection()
    {
        var root = Path.Combine(Path.GetTempPath(), "mcpserver-router-workspace-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "apps", "sub"));
        Directory.CreateDirectory(Path.Combine(root, "docs"));

        try
        {
            var fileSystem = new McpServer.Infrastructure.Files.FileSystemService(
                new McpServer.Infrastructure.Files.PathPolicy([root]),
                Substitute.For<IFileMutationLockProvider>(),
                Substitute.For<ILogger<McpServer.Infrastructure.Files.FileSystemService>>());

            var pathPolicy = new McpServer.Infrastructure.Files.PathPolicy([root]);
            var resourceTranslator = new McpServer.Infrastructure.Files.ResourcePathTranslator(root);

            var router = CreateRouter(
                fileSystemService: fileSystem,
                pathPolicy: pathPolicy,
                resourcePathTranslator: resourceTranslator);
            var arguments = JsonSerializer.SerializeToElement(new
            {
                path = "apps"
            });

            var result = await router.RouteAsync("workspace.select_folder", arguments, CancellationToken.None);

            Assert.True(result.IsSucc);
            var dto = result.Match(
                Succ: value => value,
                Fail: error => throw new InvalidOperationException(error.Message));

            var structured = Assert.IsType<WorkspaceSelectFolderResult>(dto.StructuredContent);
            Assert.Equal(Path.Combine(root, "apps"), structured.ProjectRoot);
            Assert.Contains(structured.Folders, folder => folder.Name == "sub");
        }
        finally
        {
            try
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, recursive: true);
                }
            }
            catch
            {
            }
        }
    }

    [Fact]
    public async Task RouteAsync_Should_Return_Error_For_Unknown_Tool()
    {
        var router = CreateRouter();
        var arguments = JsonSerializer.SerializeToElement(new { });

        var result = await router.RouteAsync("not-a-real-tool", arguments, CancellationToken.None);

        Assert.True(result.IsFail);
        var error = result.Match(
            Succ: _ => throw new InvalidOperationException("Expected an error."),
            Fail: value => value);

        Assert.Contains("Unknown tool", error.Message, StringComparison.Ordinal);
    }

    private static ToolCallRouter CreateRouter(
        IFileSystemService? fileSystemService = null,
        IProcessExecutionService? processExecutionService = null,
        IWebAccessService? webAccessService = null,
        ISshService? sshService = null,
        IPathPolicy? pathPolicy = null,
        IResourcePathTranslator? resourcePathTranslator = null)
    {
        fileSystemService ??= Substitute.For<IFileSystemService>();
        processExecutionService ??= Substitute.For<IProcessExecutionService>();
        webAccessService ??= Substitute.For<IWebAccessService>();
        sshService ??= Substitute.For<ISshService>();
        pathPolicy ??= Substitute.For<IPathPolicy>();
        resourcePathTranslator ??= Substitute.For<IResourcePathTranslator>();

        return new ToolCallRouter(
            new FsWriteTextToolHandler(fileSystemService, Substitute.For<ILogger<FsWriteTextToolHandler>>()),
            new FsAppendTextToolHandler(fileSystemService, Substitute.For<ILogger<FsAppendTextToolHandler>>()),
            new FsReadFileToolHandler(fileSystemService, Substitute.For<ILogger<FsReadFileToolHandler>>()),
            new FsListDirectoryToolHandler(fileSystemService, Substitute.For<ILogger<FsListDirectoryToolHandler>>()),
            new FsCreateDirectoryToolHandler(fileSystemService, Substitute.For<ILogger<FsCreateDirectoryToolHandler>>()),
            new FsMovePathToolHandler(fileSystemService, Substitute.For<ILogger<FsMovePathToolHandler>>()),
            new FsCopyPathToolHandler(fileSystemService, Substitute.For<ILogger<FsCopyPathToolHandler>>()),
            new FsDeletePathToolHandler(fileSystemService, Substitute.For<ILogger<FsDeletePathToolHandler>>()),
            new WorkspaceSetRootToolHandler(pathPolicy, resourcePathTranslator, Substitute.For<ILogger<WorkspaceSetRootToolHandler>>()),
            new WorkspaceSelectFolderToolHandler(fileSystemService, pathPolicy, resourcePathTranslator, Substitute.For<ILogger<WorkspaceSelectFolderToolHandler>>()),
            new WorkspaceInspectToolHandler(pathPolicy, Substitute.For<ILogger<WorkspaceInspectToolHandler>>()),
            new ShellExecToolHandler(processExecutionService, Substitute.For<ILogger<ShellExecToolHandler>>()),
            new SshExecuteToolHandler(sshService, Substitute.For<ILogger<SshExecuteToolHandler>>()),
            new SshWriteTextToolHandler(sshService, Substitute.For<ILogger<SshWriteTextToolHandler>>()),
            new WebFetchUrlToolHandler(webAccessService, Substitute.For<ILogger<WebFetchUrlToolHandler>>()),
            new WebSearchToolHandler(webAccessService, Substitute.For<ILogger<WebSearchToolHandler>>()));
    }
}
