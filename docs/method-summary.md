# Method Summary

## Scope

This document summarizes the public method surface of the production code in `src/` and calls out public data-only contract files separately.
Primary constructors are not listed as methods.

## McpServer.Host

### `Transport/Stdio/StdioMessageTransport.cs`

| Type | Member | Summary |
| --- | --- | --- |
| `StdioMessageTransport` | `ReadRequestAsync(CancellationToken)` | Reads one newline-delimited JSON-RPC request from stdin and deserializes it into `JsonRpcRequest`. |
| `StdioMessageTransport` | `WriteResponseAsync(JsonRpcResponse, CancellationToken)` | Serializes and writes one JSON-RPC response line to stdout. |
| `StdioMessageTransport` | `WriteNotificationAsync(JsonRpcNotification, CancellationToken)` | Serializes and writes one JSON-RPC notification line to stdout. |
| `StdioMessageTransport` | `SendRequestAsync(string, object?, CancellationToken)` | Sends a server-initiated JSON-RPC request over stdio and waits for the matching response. |
| `StdioMessageTransport` | `DisposeAsync()` | Disposes the underlying reader and writer resources. |

### `Transport/Stdio/StdioServerHostedService.cs`

| Type | Member | Summary |
| --- | --- | --- |
| `StdioServerHostedService` | Inherits `BackgroundService` | Hosted service that owns the stdio request loop, request dispatch, and application shutdown coordination. |

### `DependencyInjection/AutofacRootModule.cs`

| Type | Member | Summary |
| --- | --- | --- |
| `AutofacRootModule` | `Load(ContainerBuilder)` | Registers protocol, application, and infrastructure services, handlers, routers, and optional web features. |

### `Configuration/McpServerOptions.cs`

| Type | Member | Summary |
| --- | --- | --- |
| `McpServerOptions` | `SectionName` | Configuration root section name. |
| `McpServerOptions` | `Workspace` | Workspace root configuration. |
| `McpServerOptions` | `WebAccess` | Optional web feature configuration. |
| `WorkspaceOptions` | `RootPath` | Filesystem root exposed to the server. |
| `WebAccessOptions` | `Enabled` | Enables or disables web tools/services. |
| `WebAccessOptions` | `AllowedHosts` | Host allowlist for outbound web access. |

### `Program.cs`

| Area | Behavior |
| --- | --- |
| Host bootstrap | Creates the generic host, configures Serilog, registers the hosted service, configures named `HttpClient` instances, and loads Autofac registrations. |

## McpServer.Protocol

### `Lifecycle/CapabilityProvider.cs`

| Type | Member | Summary |
| --- | --- | --- |
| `CapabilityProvider` | `GetCapabilities()` | Returns the server capability DTO advertised during MCP initialization. |

### `Lifecycle/InitializeHandler.cs`

| Type | Member | Summary |
| --- | --- | --- |
| `InitializeHandler` | `Handle(InitializeRequestDto, McpSession)` | Negotiates the requested protocol version, updates session state, and returns the initialize response payload. |

### `Lifecycle/ShutdownHandler.cs`

| Type | Member | Summary |
| --- | --- | --- |
| `ShutdownHandler` | `Handle(McpSession)` | Marks shutdown as requested in the session. |

### `Lifecycle/ExitHandler.cs`

| Type | Member | Summary |
| --- | --- | --- |
| `ExitHandler` | `Handle(McpSession)` | Determines whether process exit should proceed and logs exit conditions. |

### `Session/McpSession.cs`

| Type | Member | Summary |
| --- | --- | --- |
| `McpSession` | `ProtocolVersion` | Protocol version captured from initialize. |
| `McpSession` | `ClientCapabilities` | Client capabilities captured from initialize. |
| `McpSession` | `ClientRoots` | Client-provided roots captured from `roots/list`. |
| `McpSession` | `IsInitialized` | Indicates whether initialize completed. |
| `McpSession` | `IsReady` | Indicates whether `notifications/initialized` has been processed. |
| `McpSession` | `IsShutdownRequested` | Indicates whether shutdown has been requested. |
| `McpSession` | `SupportsRoots` | Indicates whether the client advertised roots support during initialize. |
| `McpSession` | `CompleteInitialize(string, ClientCapabilitiesDto?)` | Stores initialize data and transitions the session into initialized state. |
| `McpSession` | `UpdateClientRoots(IReadOnlyList<RootDto>)` | Stores the client roots returned from the roots handshake. |
| `McpSession` | `MarkReady()` | Marks the session ready after the initialized notification. |
| `McpSession` | `RequestShutdown()` | Marks shutdown requested. |

### `Routing/ToolCallRouter.cs`

| Type | Member | Summary |
| --- | --- | --- |
| `ToolCallRouter` | `ListTools()` | Returns the currently registered tools as MCP DTOs. |
| `ToolCallRouter` | `RouteAsync(string, JsonElement, CancellationToken)` | Dispatches a tool call by tool name, deserializes arguments, executes the application handler, and converts the result to protocol DTO shape. |

### `Routing/ResourceReadRouter.cs`

| Type | Member | Summary |
| --- | --- | --- |
| `ResourceReadRouter` | `ListResources()` | Lists registered resource descriptors as MCP DTOs. |
| `ResourceReadRouter` | `RouteAsync(string, CancellationToken)` | Resolves a resource handler by URI scheme, reads the resource, and maps it to protocol DTOs. |

### `Routing/PromptRouter.cs`

| Type | Member | Summary |
| --- | --- | --- |
| `PromptRouter` | `ListPrompts()` | Lists registered prompts as MCP DTOs. |
| `PromptRouter` | `GetAsync(string, JsonElement?, CancellationToken)` | Dispatches a prompt lookup by prompt name and converts the application prompt result into protocol DTOs. |

### `JsonRpcErrorFactory.cs`

| Type | Member | Summary |
| --- | --- | --- |
| `JsonRpcErrorFactory` | `MethodNotFound(JsonElement?, string)` | Builds a standard JSON-RPC method-not-found response. |
| `JsonRpcErrorFactory` | `InvalidParams(JsonElement?, string)` | Builds a standard invalid-params response. |
| `JsonRpcErrorFactory` | `InvalidRequest(JsonElement?, string)` | Builds a standard invalid-request response. |
| `JsonRpcErrorFactory` | `InternalError(JsonElement?, string)` | Builds a standard internal-error response. |
| `JsonRpcErrorFactory` | `ServerError(JsonElement?, string)` | Builds a server-error response. |
| `JsonRpcErrorFactory` | `SessionNotReady(JsonElement?)` | Builds the session-not-ready response used before initialization completes. |

### `JsonRpc/JsonRpcModels.cs`

| File | Summary |
| --- | --- |
| `JsonRpcModels.cs` | Defines public JSON-RPC records: `JsonRpcRequest`, `JsonRpcNotification`, `JsonRpcResponse`, and `JsonRpcError`. These are data carriers only. |

## McpServer.Application

### Abstractions: MCP

| Type | Member | Summary |
| --- | --- | --- |
| `IToolHandler<TRequest>` | `Name` | MCP tool name. |
| `IToolHandler<TRequest>` | `Description` | Human-readable tool description. |
| `IToolHandler<TRequest>` | `GetInputSchema()` | Returns the JSON schema describing tool input. |
| `IToolHandler<TRequest>` | `Handle(TRequest, CancellationToken)` | Executes the tool request and returns a `Fin<CallToolResult>`. |
| `IResourceHandler` | `UriScheme` | URI scheme handled by the resource handler. |
| `IResourceHandler` | `Name` | Resource name advertised to MCP clients. |
| `IResourceHandler` | `Description` | Human-readable resource description. |
| `IResourceHandler` | `Describe()` | Returns the advertised resource descriptor. |
| `IResourceHandler` | `ReadAsync(string, CancellationToken)` | Reads resource content for the supplied URI. |
| `IPromptHandler` | `Name` | Prompt name advertised to clients. |
| `IPromptHandler` | `Description` | Human-readable prompt description. |
| `IPromptHandler` | `Describe()` | Returns the prompt descriptor and argument metadata. |
| `IPromptHandler` | `GetAsync(JsonElement?, CancellationToken)` | Builds a prompt result from prompt arguments. |
| `PromptMessageContent` | `FromText(string)` | Convenience factory for text prompt message content. |

### Abstractions: Files

| Type | Member | Summary |
| --- | --- | --- |
| `IPathPolicy` | `NormalizeAndValidateReadPath(string)` | Validates and normalizes a read path within allowed roots. |
| `IPathPolicy` | `NormalizeAndValidateWritePath(string)` | Validates and normalizes a write path within allowed roots. |
| `IPathPolicy` | `SetAllowedRoots(IEnumerable<string>)` | Replaces the active allowed roots for the current session context. |
| `IPathPolicy` | `SetProjectRoot(string)` | Updates the active project root used for relative paths. |
| `IResourcePathTranslator` | `TryTranslateToLocalPath(string)` | Converts a resource URI to a local path. |
| `IResourcePathTranslator` | `SetWorkspaceRoot(string)` | Replaces the active workspace root used for resource URIs. |
| `IResourcePathTranslator` | `SetProjectRoot(string)` | Updates the active project root used for `/project/...` resource URIs. |
| `IWorkspaceChangeFeed` | `RecordChange(string, string, string?)` | Records a file mutation or project-root change. |
| `IWorkspaceChangeFeed` | `GetRecentChanges(int)` | Returns the most recent recorded workspace changes. |
| `IWorkspaceFileWatcher` | `SetProjectRoot(string)` | Updates the active file watcher to the current project root. |
| `IFileMutationLockProvider` | `AcquireAsync(string, CancellationToken)` | Acquires an async lock for one normalized path. |
| `IFileMutationLockProvider` | `AcquireManyAsync(IEnumerable<string>, CancellationToken)` | Acquires async locks for multiple normalized paths. |
| `IFileSystemService` | `ReadTextAsync(ReadFileTextCommand, CancellationToken)` | Reads file text. |
| `IFileSystemService` | `ListDirectoryAsync(ListDirectoryCommand, CancellationToken)` | Lists directory contents. |
| `IFileSystemService` | `GetMetadataAsync(GetMetadataCommand, CancellationToken)` | Reads file or directory metadata. |
| `IFileSystemService` | `WriteTextAsync(WriteFileTextCommand, CancellationToken)` | Writes file text. |
| `IFileSystemService` | `AppendTextAsync(AppendFileTextCommand, CancellationToken)` | Appends file text. |
| `IFileSystemService` | `CreateDirectoryAsync(CreateDirectoryCommand, CancellationToken)` | Creates a directory. |
| `IFileSystemService` | `MovePathAsync(MovePathCommand, CancellationToken)` | Moves a file or directory. |
| `IFileSystemService` | `CopyPathAsync(CopyPathCommand, CancellationToken)` | Copies a file or directory. |
| `IFileSystemService` | `DeletePathAsync(DeletePathCommand, CancellationToken)` | Deletes a file or directory. |
| `IProcessExecutionService` | `RunAsync(RunProcessCommand, CancellationToken)` | Runs a non-interactive process within the validated project root and returns captured execution details. |

### Abstractions: Web

| Type | Member | Summary |
| --- | --- | --- |
| `IWebPolicy` | `MaxResponseBytes` | Maximum allowed response payload size. |
| `IWebPolicy` | `DefaultTimeout` | Default timeout for outbound web operations. |
| `IWebPolicy` | `MaxRedirects` | Maximum redirect hops. |
| `IWebPolicy` | `ValidateUrl(string)` | Validates URL shape and scheme. |
| `IWebPolicy` | `ValidateHost(string)` | Validates host allowlist rules. |
| `IWebAccessService` | `FetchUrlAsync(FetchUrlCommand, CancellationToken)` | Fetches a URL and returns extracted page data. |
| `IWebAccessService` | `SearchWebAsync(SearchWebCommand, CancellationToken)` | Executes a web search and returns summarized hits. |

### Abstractions: SSH

| Type | Member | Summary |
| --- | --- | --- |
| `ISshService` | `ExecuteAsync(ExecuteSshCommand, CancellationToken)` | Executes a non-interactive shell command against a configured SSH profile. |
| `ISshService` | `WriteTextAsync(WriteSshTextCommand, CancellationToken)` | Writes a remote text file over SFTP against a configured SSH profile. |

### Prompt handlers

| Type | Member | Summary |
| --- | --- | --- |
| `SummarizeFilePromptHandler` | `Name` | `prompt.summarize_file`. |
| `SummarizeFilePromptHandler` | `Description` | Describes the summarize-file prompt intent. |
| `SummarizeFilePromptHandler` | `Describe()` | Returns prompt metadata and required arguments. |
| `SummarizeFilePromptHandler` | `GetAsync(JsonElement?, CancellationToken)` | Builds prompt messages that ask the model to summarize a file resource. |
| `ReviewDirectoryPromptHandler` | `Name` | `prompt.review_directory`. |
| `ReviewDirectoryPromptHandler` | `Description` | Describes the review-directory prompt intent. |
| `ReviewDirectoryPromptHandler` | `Describe()` | Returns prompt metadata and required arguments. |
| `ReviewDirectoryPromptHandler` | `GetAsync(JsonElement?, CancellationToken)` | Builds prompt messages that ask the model to review a directory resource. |

### Resource handlers

| Type | Member | Summary |
| --- | --- | --- |
| `FsFileTextResourceHandler` | `UriScheme` / `Name` / `Description` | Advertises the `file` resource. |
| `FsFileTextResourceHandler` | `Describe()` | Returns the public resource descriptor for file text. |
| `FsFileTextResourceHandler` | `ReadAsync(string, CancellationToken)` | Reads text content for a `file://` resource URI. |
| `FsDirectoryResourceHandler` | `UriScheme` / `Name` / `Description` | Advertises the `dir` resource. |
| `FsDirectoryResourceHandler` | `Describe()` | Returns the public resource descriptor for directory listings. |
| `FsDirectoryResourceHandler` | `ReadAsync(string, CancellationToken)` | Reads directory listing content for a `dir://` URI. |
| `FsFileMetadataResourceHandler` | `UriScheme` / `Name` / `Description` | Advertises the `filemeta` resource. |
| `FsFileMetadataResourceHandler` | `Describe()` | Returns the public resource descriptor for metadata reads. |
| `FsFileMetadataResourceHandler` | `ReadAsync(string, CancellationToken)` | Reads metadata content for a `filemeta://` URI. |
| `WorkspaceChangesResourceHandler` | `UriScheme` / `Name` / `Description` | Advertises the `changes` resource for recent file mutations. |
| `WorkspaceChangesResourceHandler` | `Describe()` | Returns the public resource descriptor for the change feed. |
| `WorkspaceChangesResourceHandler` | `ReadAsync(string, CancellationToken)` | Reads recent project or workspace change events. |
| `WorkspaceTreeResourceHandler` | `UriScheme` / `Name` / `Description` | Advertises the `tree` resource for recursive workspace snapshots. |
| `WorkspaceTreeResourceHandler` | `Describe()` | Returns the public resource descriptor for the tree snapshot. |
| `WorkspaceTreeResourceHandler` | `ReadAsync(string, CancellationToken)` | Reads recursive project or workspace tree snapshots. |

### Tool handlers

| Type | Member | Summary |
| --- | --- | --- |
| `FsWriteTextToolHandler` | `Name` / `Description` | Advertises the `fs.write_text` tool. |
| `FsWriteTextToolHandler` | `GetInputSchema()` | Returns the JSON schema for write-text requests. |
| `FsWriteTextToolHandler` | `Handle(WriteFileTextRequest, CancellationToken)` | Writes text to a file within allowed roots. |
| `FsAppendTextToolHandler` | `Name` / `Description` | Advertises the `fs.append_text` tool. |
| `FsAppendTextToolHandler` | `GetInputSchema()` | Returns the append-text input schema. |
| `FsAppendTextToolHandler` | `Handle(AppendFileTextRequest, CancellationToken)` | Appends text to a file within allowed roots. |
| `FsReadFileToolHandler` | `Name` / `Description` | Advertises the `fs.read_file` tool. |
| `FsReadFileToolHandler` | `GetInputSchema()` | Returns the read-file input schema. |
| `FsReadFileToolHandler` | `Handle(FsReadFileRequest, CancellationToken)` | Reads text from a file within allowed roots. |
| `FsListDirectoryToolHandler` | `Name` / `Description` | Advertises the `fs.list_directory` tool. |
| `FsListDirectoryToolHandler` | `GetInputSchema()` | Returns the list-directory input schema. |
| `FsListDirectoryToolHandler` | `Handle(FsListDirectoryRequest, CancellationToken)` | Lists directory entries within allowed roots. |
| `FsCreateDirectoryToolHandler` | `Name` / `Description` | Advertises the `fs.create_directory` tool. |
| `FsCreateDirectoryToolHandler` | `GetInputSchema()` | Returns the create-directory input schema. |
| `FsCreateDirectoryToolHandler` | `Handle(CreateDirectoryRequest, CancellationToken)` | Creates a directory within allowed roots. |
| `FsMovePathToolHandler` | `Name` / `Description` | Advertises the `fs.move_path` tool. |
| `FsMovePathToolHandler` | `GetInputSchema()` | Returns the move-path input schema. |
| `FsMovePathToolHandler` | `Handle(MovePathRequest, CancellationToken)` | Moves a file or directory within allowed roots. |
| `FsCopyPathToolHandler` | `Name` / `Description` | Advertises the `fs.copy_path` tool. |
| `FsCopyPathToolHandler` | `GetInputSchema()` | Returns the copy-path input schema. |
| `FsCopyPathToolHandler` | `Handle(CopyPathRequest, CancellationToken)` | Copies a file or directory within allowed roots. |
| `FsDeletePathToolHandler` | `Name` / `Description` | Advertises the `fs.delete_path` tool. |
| `FsDeletePathToolHandler` | `GetInputSchema()` | Returns the delete-path input schema. |
| `FsDeletePathToolHandler` | `Handle(DeletePathRequest, CancellationToken)` | Deletes a file or directory within allowed roots. |
| `WorkspaceSetRootToolHandler` | `Name` / `Description` | Advertises the `workspace.set_root` tool. |
| `WorkspaceSetRootToolHandler` | `GetInputSchema()` | Returns the workspace-root input schema. |
| `WorkspaceSetRootToolHandler` | `Handle(WorkspaceSetRootRequest, CancellationToken)` | Replaces the active workspace root and resets the active project root. |
| `WorkspaceSelectFolderToolHandler` | `Name` / `Description` | Advertises the `workspace.select_folder` tool. |
| `WorkspaceSelectFolderToolHandler` | `GetInputSchema()` | Returns the folder-selection input schema. |
| `WorkspaceSelectFolderToolHandler` | `Handle(WorkspaceSelectFolderRequest, CancellationToken)` | Browses the current project folder and updates the active project root when a folder is selected. |
| `WorkspaceInspectToolHandler` | `Name` / `Description` | Advertises the `workspace.inspect` tool. |
| `WorkspaceInspectToolHandler` | `GetInputSchema()` | Returns the workspace inspection input schema. |
| `WorkspaceInspectToolHandler` | `Handle(WorkspaceInspectRequest, CancellationToken)` | Returns a bounded review snapshot with tree entries and likely entry-file contents, including line-numbered content. |
| `ShellExecToolHandler` | `Name` / `Description` | Advertises the `shell.exec` tool. |
| `ShellExecToolHandler` | `GetInputSchema()` | Returns the command-execution input schema. |
| `ShellExecToolHandler` | `Handle(ShellExecRequest, CancellationToken)` | Executes a non-interactive command in the validated project root and returns structured output. |
| `SshExecToolHandler` | `Name` / `Description` | Advertises the `ssh.exec` tool. |
| `SshExecToolHandler` | `GetInputSchema()` | Returns the SSH command-execution input schema. |
| `SshExecToolHandler` | `Handle(SshExecRequest, CancellationToken)` | Executes a non-interactive shell command over SSH and returns structured output. |
| `SshWriteTextToolHandler` | `Name` / `Description` | Advertises the `ssh.write_text` tool. |
| `SshWriteTextToolHandler` | `GetInputSchema()` | Returns the remote file-write input schema. |
| `SshWriteTextToolHandler` | `Handle(SshWriteTextRequest, CancellationToken)` | Writes a remote text file over SFTP and returns a structured summary. |
| `WebFetchToolHandler` | `Name` / `Description` | Advertises the `web.fetch_url` tool. |
| `WebFetchToolHandler` | `GetInputSchema()` | Returns the fetch-url input schema. |
| `WebFetchToolHandler` | `Handle(WebFetchUrlRequest, CancellationToken)` | Fetches a URL and maps the result into MCP content. |
| `WebSearchToolHandler` | `Name` / `Description` | Advertises the `web.search` tool. |
| `WebSearchToolHandler` | `GetInputSchema()` | Returns the web-search input schema. |
| `WebSearchToolHandler` | `Handle(WebSearchRequest, CancellationToken)` | Executes a web search and maps the result into a structured search summary. |

### Commands and result models

| File | Summary |
| --- | --- |
| `Files/Commands/FileCommands.cs` | Public command records for file service operations. Data carriers only. |
| `Files/Results/FileResults.cs` | Public result records returned by file operations. Data carriers only. |
| `Execution/Commands/ProcessCommands.cs` | Public command records for process execution requests. Data carriers only. |
| `Execution/Results/ProcessResults.cs` | Public result records returned by process execution. Data carriers only. |
| `Ssh/Commands/SshCommands.cs` | Public command records for SSH execution and remote file writes. Data carriers only. |
| `Ssh/Results/SshResults.cs` | Public result records returned by SSH operations. Data carriers only. |
| `Web/Commands/WebCommands.cs` | Public command records for web operations. Data carriers only. |
| `Web/Results/WebResults.cs` | Public result records returned by web operations. Data carriers only. |

## McpServer.Infrastructure

| Type | Member | Summary |
| --- | --- | --- |
| `PathPolicy` | `NormalizeAndValidateReadPath(string)` | Normalizes and validates read paths against allowed roots. Relative paths resolve from the active project root. |
| `PathPolicy` | `NormalizeAndValidateWritePath(string)` | Normalizes and validates write paths against allowed roots. Relative paths resolve from the active project root. |
| `PathPolicy` | `SetProjectRoot(string)` | Updates the active project root used for relative path resolution. |
| `ResourcePathTranslator` | `TryTranslateToLocalPath(string)` | Converts `file`, `dir`, and `filemeta` URIs into local paths. |
| `ResourcePathTranslator` | `SetWorkspaceRoot(string)` | Updates the workspace root used for `/workspace/...` URIs. |
| `ResourcePathTranslator` | `SetProjectRoot(string)` | Updates the active project root used for `/project/...` URIs. |
| `WorkspaceChangeFeed` | `RecordChange(string, string, string?)` | Appends a change event to the recent mutation feed. |
| `WorkspaceChangeFeed` | `GetRecentChanges(int)` | Returns the most recent recorded change events. |
| `WorkspaceFileWatcher` | `SetProjectRoot(string)` | Rebinds the filesystem watcher to a new project root. |
| `FileMutationLockProvider` | `AcquireAsync(string, CancellationToken)` | Acquires a lock for one normalized path. |
| `FileMutationLockProvider` | `AcquireManyAsync(IEnumerable<string>, CancellationToken)` | Acquires locks for multiple normalized paths in a stable order. |
| `FileSystemService` | `ReadTextAsync(ReadFileTextCommand, CancellationToken)` | Reads file text with path validation. |
| `FileSystemService` | `ListDirectoryAsync(ListDirectoryCommand, CancellationToken)` | Enumerates top-level directory contents and returns metadata. |
| `FileSystemService` | `GetMetadataAsync(GetMetadataCommand, CancellationToken)` | Returns metadata for files or directories. |
| `FileSystemService` | `WriteTextAsync(WriteFileTextCommand, CancellationToken)` | Writes text to a file. |
| `FileSystemService` | `AppendTextAsync(AppendFileTextCommand, CancellationToken)` | Appends text to a file. |
| `FileSystemService` | `CreateDirectoryAsync(CreateDirectoryCommand, CancellationToken)` | Creates a directory. |
| `FileSystemService` | `MovePathAsync(MovePathCommand, CancellationToken)` | Moves files or directories. |
| `FileSystemService` | `CopyPathAsync(CopyPathCommand, CancellationToken)` | Copies files or directories. |
| `FileSystemService` | `DeletePathAsync(DeletePathCommand, CancellationToken)` | Deletes files or directories. |
| `ProcessExecutionService` | `RunAsync(RunProcessCommand, CancellationToken)` | Executes a non-interactive process with validated working directory, timeout, and output capture. |
| `SshService` | `ExecuteAsync(ExecuteSshCommand, CancellationToken)` | Executes a non-interactive command against a configured SSH profile and captures output. |
| `SshService` | `WriteTextAsync(WriteSshTextCommand, CancellationToken)` | Uploads text content to a remote file over SFTP and can apply permissions. |
| `WebPolicy` | `MaxResponseBytes` | Max response size allowed for web fetches. |
| `WebPolicy` | `DefaultTimeout` | Default outbound timeout. |
| `WebPolicy` | `MaxRedirects` | Max redirect hops. |
| `WebPolicy` | `ValidateUrl(string)` | Validates URL scheme and shape. |
| `WebPolicy` | `ValidateHost(string)` | Validates host allowlist rules. |
| `WebAccessService` | `FetchUrlAsync(FetchUrlCommand, CancellationToken)` | Fetches content from a URL, optionally extracts readable text, and returns metadata. |
| `WebAccessService` | `SearchWebAsync(SearchWebCommand, CancellationToken)` | Performs a simple web search and returns summarized hit results. |
| `HtmlTextExtractor` | `ExtractReadableText(string)` | Strips HTML to approximate readable text. |
| `HtmlTextExtractor` | `ExtractTitle(string)` | Extracts the document title. |
| `HtmlTextExtractor` | `ExtractLinks(string, Uri)` | Extracts links from HTML relative to a base URI. |
| `PathComparison` | `Comparer` | OS-aware string comparer for paths. |
| `PathComparison` | `Comparison` | OS-aware `StringComparison` value for paths. |
| `SerilogBootstrap` | `Configure(LoggerConfiguration, IConfiguration)` | Applies server logging configuration and default sinks. |

## McpServer.Contracts

Most contract files are data-only records with no behavior. The public helper/factory methods are:

| Type | Member | Summary |
| --- | --- | --- |
| `PromptMessageContentDto` | `FromText(string)` | Convenience factory for text prompt message DTOs. |
| `TextContentDto` | `Create(string)` | Convenience factory for text content DTOs. |

Data-only contract groups:

| File group | Summary |
| --- | --- |
| `Lifecycle/*.cs` | MCP initialize/shutdown request and response DTOs plus advertised capability records. |
| `Roots/*.cs` | MCP filesystem root DTOs used for the client roots handshake. |
| `Tools/*.cs` | Tool request payloads and tool list/call DTOs. |
| `Resources/*.cs` | Resource list/read DTOs. |
| `Prompts/*.cs` | Prompt list/get DTOs and prompt argument payloads. |
| `Shared/McpContentDtos.cs` | Shared text/blob content DTOs used by tool/resource responses. |

## McpServer.Domain

| Type | Member | Summary |
| --- | --- | --- |
| `Placeholder` | None | Reserved placeholder type for future domain logic. |

## Tests

The test projects expose public test classes but do not define reusable application API surface.
They validate:

- stdio request/response framing
- initialize, ping, and prompt routing behavior
- project-scoped shell command execution
- SSH command and remote file tool behavior
- path comparison semantics
- web tool mapping and web result parsing
- file change feed/resource exposure
- push file-change notifications over stdio
- workspace folder selection
- end-to-end stdio initialize response shape
