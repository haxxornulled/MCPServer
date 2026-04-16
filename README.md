# McpServer

Production-oriented MCP server starter for .NET 10 / C# 14 / Visual Studio 2026.

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

## Documentation

- [Architecture](docs/architecture.md)
- [Method Summary](docs/method-summary.md)
- [Changelog](CHANGELOG.md)
