using System.Text.Json;
using LanguageExt;
using LanguageExt.Common;
using McpServer.Application.Abstractions.Mcp;
using McpServer.Application.Mcp.Tools;
using McpServer.Contracts.Shared;
using McpServer.Contracts.Tools;

namespace McpServer.Protocol.Routing;

public sealed class ToolCallRouter
{
    private readonly FsWriteTextToolHandler _writeTextHandler;
    private readonly FsAppendTextToolHandler _appendTextHandler;
    private readonly FsReadFileToolHandler _readFileHandler;
    private readonly FsCreateDirectoryToolHandler _createDirectoryHandler;
    private readonly FsMovePathToolHandler _movePathHandler;
    private readonly FsCopyPathToolHandler _copyPathHandler;
    private readonly FsDeletePathToolHandler _deletePathHandler;
    private readonly ShellExecToolHandler _shellExecHandler;
    private readonly SshExecuteToolHandler? _sshExecuteHandler;
    private readonly SshWriteTextToolHandler? _sshWriteTextHandler;
    private readonly WebFetchUrlToolHandler? _webFetchHandler;
    private readonly WebSearchToolHandler? _webSearchHandler;
    private readonly ToolDto[] _tools;

    public ToolCallRouter(
        FsWriteTextToolHandler writeTextHandler,
        FsAppendTextToolHandler appendTextHandler,
        FsReadFileToolHandler readFileHandler,
        FsCreateDirectoryToolHandler createDirectoryHandler,
        FsMovePathToolHandler movePathHandler,
        FsCopyPathToolHandler copyPathHandler,
        FsDeletePathToolHandler deletePathHandler,
        ShellExecToolHandler shellExecHandler,
        SshExecuteToolHandler? sshExecuteHandler = null,
        SshWriteTextToolHandler? sshWriteTextHandler = null,
        WebFetchUrlToolHandler? webFetchHandler = null,
        WebSearchToolHandler? webSearchHandler = null)
    {
        _writeTextHandler = writeTextHandler;
        _appendTextHandler = appendTextHandler;
        _readFileHandler = readFileHandler;
        _createDirectoryHandler = createDirectoryHandler;
        _movePathHandler = movePathHandler;
        _copyPathHandler = copyPathHandler;
        _deletePathHandler = deletePathHandler;
        _shellExecHandler = shellExecHandler;
        _sshExecuteHandler = sshExecuteHandler;
        _sshWriteTextHandler = sshWriteTextHandler;
        _webFetchHandler = webFetchHandler;
        _webSearchHandler = webSearchHandler;
        _tools = BuildTools(
            _writeTextHandler,
            _appendTextHandler,
            _readFileHandler,
            _createDirectoryHandler,
            _movePathHandler,
            _copyPathHandler,
            _deletePathHandler,
            _shellExecHandler,
            _sshExecuteHandler,
            _sshWriteTextHandler,
            _webFetchHandler,
            _webSearchHandler);
    }

    public ListToolsResult ListTools()
    {
        return new ListToolsResult(Tools: _tools, NextCursor: null);
    }

    public async ValueTask<Fin<CallToolResultDto>> RouteAsync(string name, JsonElement arguments, CancellationToken ct)
    {
        var appResult = name switch
        {
            "fs.write_text" => await CallWriteTextAsync(arguments, ct).ConfigureAwait(false),
            "fs.append_text" => await CallAppendTextAsync(arguments, ct).ConfigureAwait(false),
            "fs.read_file" => await CallReadFileAsync(arguments, ct).ConfigureAwait(false),
            "fs.create_directory" => await CallCreateDirectoryAsync(arguments, ct).ConfigureAwait(false),
            "fs.move_path" => await CallMovePathAsync(arguments, ct).ConfigureAwait(false),
            "fs.copy_path" => await CallCopyPathAsync(arguments, ct).ConfigureAwait(false),
            "fs.delete_path" => await CallDeletePathAsync(arguments, ct).ConfigureAwait(false),
            "shell.exec" => await CallShellExecAsync(arguments, ct).ConfigureAwait(false),
            "ssh.execute" when _sshExecuteHandler is not null => await CallSshExecuteAsync(arguments, ct).ConfigureAwait(false),
            "ssh.write_text" when _sshWriteTextHandler is not null => await CallSshWriteTextAsync(arguments, ct).ConfigureAwait(false),
            "web.fetch_url" when _webFetchHandler is not null => await CallWebFetchAsync(arguments, ct).ConfigureAwait(false),
            "web.search" when _webSearchHandler is not null => await CallWebSearchAsync(arguments, ct).ConfigureAwait(false),
            _ => Error.New($"Unknown tool: {name}")
        };

        return appResult.Map(ToCallToolDto);
    }

    private static ToolDto ToToolDto<TRequest>(IToolHandler<TRequest> handler) =>
        new(Name: handler.Name, Title: null, Description: handler.Description, InputSchema: handler.GetInputSchema());

    private static ToolDto[] BuildTools(
        FsWriteTextToolHandler writeTextHandler,
        FsAppendTextToolHandler appendTextHandler,
        FsReadFileToolHandler readFileHandler,
        FsCreateDirectoryToolHandler createDirectoryHandler,
        FsMovePathToolHandler movePathHandler,
        FsCopyPathToolHandler copyPathHandler,
        FsDeletePathToolHandler deletePathHandler,
        ShellExecToolHandler shellExecHandler,
        SshExecuteToolHandler? sshExecuteHandler,
        SshWriteTextToolHandler? sshWriteTextHandler,
        WebFetchUrlToolHandler? webFetchHandler,
        WebSearchToolHandler? webSearchHandler)
    {
        var tools = new List<ToolDto>
        {
            ToToolDto(writeTextHandler),
            ToToolDto(appendTextHandler),
            ToToolDto(readFileHandler),
            ToToolDto(createDirectoryHandler),
            ToToolDto(movePathHandler),
            ToToolDto(copyPathHandler),
            ToToolDto(deletePathHandler),
            ToToolDto(shellExecHandler)
        };

        if (sshExecuteHandler is not null)
        {
            tools.Add(ToToolDto(sshExecuteHandler));
        }

        if (sshWriteTextHandler is not null)
        {
            tools.Add(ToToolDto(sshWriteTextHandler));
        }

        if (webFetchHandler is not null)
        {
            tools.Add(ToToolDto(webFetchHandler));
        }

        if (webSearchHandler is not null)
        {
            tools.Add(ToToolDto(webSearchHandler));
        }

        return tools.ToArray();
    }

    private static CallToolResultDto ToCallToolDto(CallToolResult result) =>
        new(
            Content: result.Content.Select(x => TextContentDto.Create(x.Text)).ToArray(),
            StructuredContent: result.StructuredContent,
            IsError: result.IsError);

    private async ValueTask<Fin<CallToolResult>> CallWriteTextAsync(JsonElement arguments, CancellationToken ct)
    {
        var request = arguments.Deserialize<FsWriteTextRequest>();
        return request is null
            ? Error.New("Invalid arguments for fs.write_text")
            : await _writeTextHandler.Handle(request, ct).ConfigureAwait(false);
    }

    private async ValueTask<Fin<CallToolResult>> CallAppendTextAsync(JsonElement arguments, CancellationToken ct)
    {
        var request = arguments.Deserialize<AppendFileTextRequest>();
        return request is null
            ? Error.New("Invalid arguments for fs.append_text")
            : await _appendTextHandler.Handle(request, ct).ConfigureAwait(false);
    }

    private async ValueTask<Fin<CallToolResult>> CallReadFileAsync(JsonElement arguments, CancellationToken ct)
    {
        var request = arguments.Deserialize<FsReadFileRequest>();
        return request is null
            ? Error.New("Invalid arguments for fs.read_file")
            : await _readFileHandler.Handle(request, ct).ConfigureAwait(false);
    }

    private async ValueTask<Fin<CallToolResult>> CallCreateDirectoryAsync(JsonElement arguments, CancellationToken ct)
    {
        var request = arguments.Deserialize<CreateDirectoryRequest>();
        return request is null
            ? Error.New("Invalid arguments for fs.create_directory")
            : await _createDirectoryHandler.Handle(request, ct).ConfigureAwait(false);
    }

    private async ValueTask<Fin<CallToolResult>> CallMovePathAsync(JsonElement arguments, CancellationToken ct)
    {
        var request = arguments.Deserialize<FsMovePathRequest>();
        return request is null
            ? Error.New("Invalid arguments for fs.move_path")
            : await _movePathHandler.Handle(request, ct).ConfigureAwait(false);
    }

    private async ValueTask<Fin<CallToolResult>> CallCopyPathAsync(JsonElement arguments, CancellationToken ct)
    {
        var request = arguments.Deserialize<FsCopyPathRequest>();
        return request is null
            ? Error.New("Invalid arguments for fs.copy_path")
            : await _copyPathHandler.Handle(request, ct).ConfigureAwait(false);
    }

    private async ValueTask<Fin<CallToolResult>> CallDeletePathAsync(JsonElement arguments, CancellationToken ct)
    {
        var request = arguments.Deserialize<DeletePathRequest>();
        return request is null
            ? Error.New("Invalid arguments for fs.delete_path")
            : await _deletePathHandler.Handle(request, ct).ConfigureAwait(false);
    }

    private async ValueTask<Fin<CallToolResult>> CallShellExecAsync(JsonElement arguments, CancellationToken ct)
    {
        var request = arguments.Deserialize<ShellExecRequest>();
        return request is null
            ? Error.New("Invalid arguments for shell.exec")
            : await _shellExecHandler.Handle(request, ct).ConfigureAwait(false);
    }

    private async ValueTask<Fin<CallToolResult>> CallSshExecuteAsync(JsonElement arguments, CancellationToken ct)
    {
        var request = arguments.Deserialize<SshExecuteRequest>();
        return request is null || _sshExecuteHandler is null
            ? Error.New("Invalid arguments for ssh.execute")
            : await _sshExecuteHandler.Handle(request, ct).ConfigureAwait(false);
    }

    private async ValueTask<Fin<CallToolResult>> CallSshWriteTextAsync(JsonElement arguments, CancellationToken ct)
    {
        var request = arguments.Deserialize<SshWriteTextRequest>();
        return request is null || _sshWriteTextHandler is null
            ? Error.New("Invalid arguments for ssh.write_text")
            : await _sshWriteTextHandler.Handle(request, ct).ConfigureAwait(false);
    }

    private async ValueTask<Fin<CallToolResult>> CallWebFetchAsync(JsonElement arguments, CancellationToken ct)
    {
        var request = arguments.Deserialize<WebFetchUrlRequest>();
        return request is null || _webFetchHandler is null
            ? Error.New("Invalid arguments for web.fetch_url")
            : await _webFetchHandler.Handle(request, ct).ConfigureAwait(false);
    }

    private async ValueTask<Fin<CallToolResult>> CallWebSearchAsync(JsonElement arguments, CancellationToken ct)
    {
        var request = arguments.Deserialize<WebSearchRequest>();
        return request is null || _webSearchHandler is null
            ? Error.New("Invalid arguments for web.search")
            : await _webSearchHandler.Handle(request, ct).ConfigureAwait(false);
    }
}
