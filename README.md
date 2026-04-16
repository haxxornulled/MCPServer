# McpServer

[![CI](https://github.com/haxxornulled/MCPServer/actions/workflows/ci.yml/badge.svg)](https://github.com/haxxornulled/MCPServer/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C%23 14](https://img.shields.io/badge/C%23-14-239120?logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Production-oriented MCP server starter for .NET 10 / C# 14 / Visual Studio 2026.

## Current Release

`v0.1.1` is the current validated release.
It includes the workspace path resolution fix that makes filesystem tool calls and `file:///workspace/...` resource reads work end to end.

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

Read that file back through MCP resources:

```json
{"jsonrpc":"2.0","id":3,"method":"resources/read","params":{"uri":"file:///workspace/smoke.txt"}}
```

Expected success shape:

```json
{"jsonrpc":"2.0","id":3,"result":{"contents":[{"uri":"file:///workspace/smoke.txt","mimeType":"text/plain","text":"hello from MCP"}]},"error":null}
```

## Documentation

- [Architecture](docs/architecture.md)
- [Method Summary](docs/method-summary.md)
- [Changelog](CHANGELOG.md)

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
