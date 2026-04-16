# Changelog

All notable changes to this project will be documented in this file.

## 0.1.4 - 2026-04-16

Patch release aligning repository metadata, LM Studio guidance, and agent-extension policy with the current validated server behavior.

### Changed

- Updated the LM Studio setup guidance to launch the built `McpServer.Host.exe` binary directly so local MCP registration uses the latest rebuilt executable.
- Removed the stale README note claiming the repository could not be compile-verified in its original generation environment.
- Added explicit extension-point guidance to repo-level agent instruction files so future changes follow the `IToolHandler<TRequest>`, `IResourceHandler`, `IPromptHandler`, Autofac, and application-abstraction seams already documented in the architecture guide.
- Updated GitHub repository topics to improve discoverability for MCP, LM Studio, GitHub Copilot, JSON-RPC, and SSH automation use cases.

### Validation

- `gh run view 24493206070 --json status,conclusion,url,jobs,displayTitle,headSha,updatedAt`

## 0.1.3 - 2026-04-15

Patch release adding profile-based SSH automation tools for remote DevOps workflows.

### Added

- Added optional SSH profile configuration with host, port, username, environment-variable-based credentials, host key pinning, and default working directory support.
- Added `ssh.exec` for remote non-interactive shell command execution on configured SSH hosts.
- Added `ssh.write_text` for writing remote configuration files over SFTP with optional parent-directory creation and octal permissions.
- Added focused unit coverage for SSH tool handlers and SSH profile validation behavior.

### Changed

- Updated dependency injection and tool routing so SSH tools are only exposed when SSH profiles are enabled and configured.
- Updated README and architecture/method documentation to describe the new remote automation path and production-safe SSH configuration patterns.

### Validation

- `dotnet build .\src\McpServer.Host\McpServer.Host.csproj -c Debug`
- `dotnet test .\tests\McpServer.UnitTests\McpServer.UnitTests.csproj -c Debug --filter "FullyQualifiedName~Ssh"`

## 0.1.2 - 2026-04-15

Patch release focused on LM Studio compatibility and general workspace-scoped command execution.

### Added

- Added the `shell.exec` MCP tool for non-interactive command execution inside the configured workspace, including structured exit code, stdout, stderr, timeout, and truncation details.
- Added unit and integration coverage for `shell.exec`, `ping`, JSON-RPC success response shape, protocol negotiation fallback, and LM Studio virtual workspace aliases.
- Added README guidance for registering the server in LM Studio and invoking the new command tool.

### Changed

- Updated MCP protocol negotiation to preserve supported versions and fall back to `2025-03-26` for unknown client versions so current LM Studio builds can connect successfully.
- Added `ping` handling in the stdio host for MCP clients that probe server health before using tools.
- Switched host content-root and workspace resolution to `AppContext.BaseDirectory` so the server still works when launched from unrelated working directories.
- Omitted null JSON-RPC response fields on successful calls to keep the transport compliant with stricter MCP hosts.
- Expanded workspace path handling to treat both `/workspace/...` and LM Studio's `/mcpserver-filesystem/...` alias as the same virtual root.

### Validation

- `dotnet build .\src\McpServer.Host\McpServer.Host.csproj -c Debug`
- `dotnet test .\tests\McpServer.UnitTests\McpServer.UnitTests.csproj -c Debug --filter "FullyQualifiedName~PathPolicyTests|FullyQualifiedName~InitializeHandlerTests|FullyQualifiedName~StdioMessageTransportTests|FullyQualifiedName~ShellExecToolHandlerTests"`
- `dotnet test .\tests\McpServer.IntegrationTests\McpServer.IntegrationTests.csproj -c Debug --filter "FullyQualifiedName~StdioLifecycleIntegrationTests"`

## 0.1.1 - 2026-04-15

Patch release fixing workspace path resolution for filesystem tools and resources.

### Fixed

- Aligned filesystem tool path handling with the configured workspace root so relative tool paths resolve correctly.
- Fixed resource URI translation for `file:///workspace/...` and `dir:///workspace` so MCP resource reads map into the host workspace.
- Registered path policy and resource URI translation from the same resolved workspace root to eliminate runtime path mismatches.

### Added

- Unit tests covering workspace-relative path normalization and resource URI translation.
- Integration coverage for an end-to-end `fs.write_text` plus `resources/read` round-trip.

### Validation

- `dotnet build .\McpServer.slnx -v minimal`
- `dotnet test .\tests\McpServer.UnitTests\McpServer.UnitTests.csproj -v minimal --no-build`
- `dotnet test .\tests\McpServer.IntegrationTests\McpServer.IntegrationTests.csproj -v minimal`

## 0.1.0 - 2026-04-15

Initial release of the MCP server scaffold.

### Added

- Cleanly layered solution structure across Host, Protocol, Application, Infrastructure, Contracts, and Domain projects.
- STDIO-based MCP host with JSON-RPC request/response transport.
- MCP lifecycle handling for initialize, initialized, shutdown, and exit flows.
- Filesystem MCP tools for write, append, create directory, move, copy, and delete operations.
- Filesystem MCP resources for file text, directory listing, and file metadata reads.
- Prompt support for summarizing files and reviewing directories.
- Optional web access services and MCP tools for URL fetch and web search.
- Autofac-based dependency injection and Serilog-based logging bootstrap.
- Unit and integration test coverage for transport, routing, lifecycle, and host startup flows.
- Architecture documentation in `docs/architecture.md`.
- Public API and method inventory in `docs/method-summary.md`.

### Changed

- Corrected shared MSBuild configuration so the solution loads, restores, and builds correctly under the installed .NET 10 SDK.
- Fixed multiple protocol, application, infrastructure, host, and test compilation issues across the initial scaffold.
- Replaced `FluentAssertions` with xUnit assertions to keep the test stack fully open source.

### Validation

- `dotnet build .\McpServer.slnx -v minimal`
- `dotnet test .\tests\McpServer.UnitTests\McpServer.UnitTests.csproj -v minimal`
- `dotnet test .\tests\McpServer.IntegrationTests\McpServer.IntegrationTests.csproj -v minimal`