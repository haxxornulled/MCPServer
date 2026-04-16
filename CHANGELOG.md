# Changelog

All notable changes to this project will be documented in this file.

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