# McpServer

[![CI](https://github.com/haxxornulled/MCPServer/actions/workflows/ci.yml/badge.svg)](https://github.com/haxxornulled/MCPServer/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C%23 14](https://img.shields.io/badge/C%23-14-239120?logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Production-oriented MCP server starter for .NET 10 / C# 14 / Visual Studio 2026.

## Current Release

`v0.1.2` is the current validated release.
It adds LM Studio compatibility fixes, `shell.exec` for workspace-scoped command execution, and more robust host startup/path handling for external MCP clients.

## Highlights

- Clean Architecture
- Autofac
- LanguageExt `Fin<T>`
- Serilog
- STDIO MCP transport
- Filesystem tools/resources
- Prompt support
- Optional web tools
- xUnit + NSubstitute tests

## Notes

This repo was generated in an environment without the .NET SDK installed, so the source tree could not be compile-verified here. The design and project graph were written to be buildable in Visual Studio 2026 / .NET 10.

## First run

1. Open `McpServer.slnx` in Visual Studio 2026.
2. Restore NuGet packages.
3. Build.
4. Start `McpServer.Host`.

## Quick Start

Run the host:

```powershell
dotnet run --project .\src\McpServer.Host\McpServer.Host.csproj
```

## LM Studio

LM Studio supports local MCP programs via `mcp.json`. This server uses the MCP stdio transport, so you can register it as a local program.

Windows example:

```json
{
	"mcpServers": {
		"mcpserver-filesystem": {
			"command": "dotnet",
			"args": [
				"run",
				"--project",
				"D:/McpServerRepo/src/McpServer.Host/McpServer.Host.csproj",
				"--no-build"
			]
		}
	}
}
```

Recommended workflow:

1. Build the host once before connecting from LM Studio.
2. In LM Studio, open the MCP settings and add the server entry to `mcp.json`.
3. Restart the MCP server from LM Studio after rebuilding.

Compatibility notes:

- The server negotiates MCP protocol versions and falls back to `2025-03-26` for unknown client versions to stay compatible with current MCP hosts such as LM Studio.
- The server supports `ping`, which some MCP hosts use as a connection health check.
- The server exposes `shell.exec` for non-interactive command execution inside the configured workspace.
- `shell.exec` accepts both `workspace` and LM Studio's `/mcpserver-filesystem` alias as workspace roots for `workingDirectory`.
- Logs are written to stderr and `logs/`, not stdout, so stdio MCP traffic stays clean.

Then send newline-delimited JSON-RPC messages over stdio.

Initialize the MCP session:

```json
{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"manual-client","version":"1.0.0"}}}
{"jsonrpc":"2.0","method":"notifications/initialized"}
```

Write a file into the configured workspace:

```json
{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"fs.write_text","arguments":{"path":"smoke.txt","content":"hello from MCP","overwrite":true}}}
```

Run a workspace-scoped command:

```json
{"jsonrpc":"2.0","id":4,"method":"tools/call","params":{"name":"shell.exec","arguments":{"command":"dotnet","args":["--version"],"timeoutSeconds":30}}}
```

Read that file back through MCP resources:

```json
{"jsonrpc":"2.0","id":3,"method":"resources/read","params":{"uri":"file:///workspace/smoke.txt"}}
```

Expected success shape:

```json
{"jsonrpc":"2.0","id":3,"result":{"contents":[{"uri":"file:///workspace/smoke.txt","mimeType":"text/plain","text":"hello from MCP"}]}}
```

## Documentation

- [Architecture](docs/architecture.md)
- [Method Summary](docs/method-summary.md)
- [Changelog](CHANGELOG.md)

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
