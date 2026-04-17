# McpServer

[![CI](https://github.com/haxxornulled/MCPServer/actions/workflows/ci.yml/badge.svg)](https://github.com/haxxornulled/MCPServer/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C%23 14](https://img.shields.io/badge/C%23-14-239120?logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Production-oriented MCP server starter for .NET 10 / C# 14 / Visual Studio 2026.

## Current Release

`v0.1.6` is the current validated release.
It adds GitHub Packages publishing for the `McpServer.Host` .NET tool, package install guidance, and the prior inference-driven MCP smoke testing and release-polish work.

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

## GitHub Packages

The host is also published as a .NET tool package to GitHub Packages on version tags.

Package details:

- Package ID: `McpServer.Host`
- Command name: `mcpserver`
- Feed: `https://nuget.pkg.github.com/haxxornulled/index.json`

Authentication requires a GitHub token with `read:packages`.

Add the feed:

```powershell
dotnet nuget add source https://nuget.pkg.github.com/haxxornulled/index.json --name github-haxxornulled --username YOUR_GITHUB_USERNAME --password YOUR_GITHUB_TOKEN --store-password-in-clear-text
```

Install the current tool version:

```powershell
dotnet tool install --global McpServer.Host --version 0.1.6 --add-source https://nuget.pkg.github.com/haxxornulled/index.json
```

Update an existing install:

```powershell
dotnet tool update --global McpServer.Host --version 0.1.6 --add-source https://nuget.pkg.github.com/haxxornulled/index.json
```

After installation, run `mcpserver`.

## GitHub Copilot

GitHub Copilot in Visual Studio can use the released `mcpserver` tool as a local MCP server.

Install the current release first:

```powershell
dotnet tool install --global McpServer.Host --version 0.1.6 --add-source https://nuget.pkg.github.com/haxxornulled/index.json
```

This repository includes a solution-scoped `.mcp.json` file that registers the released tool for Copilot:

```json
{
	"inputs": [],
	"servers": {
		"mcpserver-release": {
			"type": "stdio",
			"command": "mcpserver",
			"args": []
		}
	}
}
```

In Visual Studio, open the solution, then open GitHub Copilot Chat in **Agent** mode and enable the `mcpserver-release` server.
If you check the file into source control, keep it at the solution root so Visual Studio can discover it.

## LM Studio

LM Studio supports local MCP programs via `mcp.json`. This server uses the MCP stdio transport, so you can register it as a local program.

LM Studio 0.3.17+ can load MCP servers from `~/.lmstudio/mcp.json` on macOS/Linux or `%USERPROFILE%\.lmstudio\mcp.json` on Windows. It also has an "Add to LM Studio" deeplink flow if you want one-click setup.

Windows example:

```json
{
	"mcpServers": {
		"mcpserver-filesystem": {
			"command": "mcpserver"
		},
		"mcpserver-local-build": {
			"command": "D:/McpServerRepo/src/McpServer.Host/bin/Release/net10.0/McpServer.Host.exe"
		}
	}
}
```

Recommended workflow:

1. Install the `mcpserver` tool from GitHub Packages, or point LM Studio at the `Release` exe shown above.
2. In LM Studio, open the Program tab, choose `Install > Edit mcp.json`, and add the server entry.
3. Restart the MCP server from LM Studio after every rebuild so it picks up the latest executable.
4. If you prefer `Debug` during development, change the path explicitly to `bin/Debug/net10.0/McpServer.Host.exe` rather than relying on `dotnet run`.
5. On Windows, the helper script `scripts/install-lmstudio-mcp.ps1` can create or update `%USERPROFILE%\.lmstudio\mcp.json` for you.

If you install from GitHub Packages instead of using a local build, point LM Studio at the installed `mcpserver` command rather than the repository output path.

Compatibility notes:

- The server negotiates MCP protocol versions and falls back to `2025-03-26` for unknown client versions to stay compatible with current MCP hosts such as LM Studio.
- The server supports `ping`, which some MCP hosts use as a connection health check.
- The server exposes `shell.exec` for non-interactive command execution inside the configured workspace.
- The server can optionally expose `ssh.exec` and `ssh.write_text` when SSH profiles are enabled in configuration.
- `shell.exec` accepts both `workspace` and LM Studio's `/mcpserver-filesystem` alias as workspace roots for `workingDirectory`.
- Logs are written to stderr and `logs/`, not stdout, so stdio MCP traffic stays clean.

## Inference Tool Smoke Testing

The repository includes a PowerShell harness that tests the registered MCP tools through an OpenAI-compatible inference endpoint such as LM Studio running at `http://192.168.96.1:1234`.

Default command:

```powershell
pwsh -File .\scripts\Invoke-InferenceToolSmokeTest.ps1
```

What it does:

- starts the MCP host over stdio using the latest built host executable when available
- initializes the MCP session and reads `tools/list`
- converts each MCP tool schema into an OpenAI tool definition
- asks the inference model to call each tool using ordered scenarios from `scripts/inference-tool-scenarios.json`
- executes the resulting `tools/call` requests against the real MCP server and verifies the tool responses
- prints per-tool pass, fail, or skip results immediately so long-running suites stay debuggable

Notes:

- The default scenarios cover the core registered tools in the default host configuration: filesystem tools plus `shell.exec`.
- The default scenarios are ordered and dependency-aware, so downstream tools are skipped with an explicit reason if an earlier prerequisite scenario fails.
- Optional tools such as `ssh.exec`, `ssh.write_text`, `web.fetch_url`, and `web.search` can be tested by enabling them in host configuration and extending `scripts/inference-tool-scenarios.json`.
- If the Release host executable is missing, the script falls back to `dotnet run --project ... --no-build`.

Useful options:

```powershell
pwsh -File .\scripts\Invoke-InferenceToolSmokeTest.ps1 -ListToolsOnly
pwsh -File .\scripts\Invoke-InferenceToolSmokeTest.ps1 -IncludeTool shell.exec
pwsh -File .\scripts\Invoke-InferenceToolSmokeTest.ps1 -AllowMissingScenarios
pwsh -File .\scripts\Invoke-InferenceToolSmokeTest.ps1 -Model openai/gpt-oss-20b
pwsh -File .\scripts\Invoke-InferenceToolSmokeTest.ps1 -Model openai/gpt-oss-20b -InferenceTimeoutSeconds 180
```

## SSH Automation

For remote DevOps-style work, configure named SSH profiles in `appsettings.json` and keep secrets in environment variables.

Example:

```json
{
	"McpServer": {
		"Ssh": {
			"Enabled": true,
			"Profiles": [
				{
					"Name": "prod-web",
					"Host": "10.0.0.10",
					"Port": 22,
					"Username": "deploy",
					"PrivateKeyPath": "./secrets/prod-web-id_ed25519",
					"WorkingDirectory": "/home/deploy",
					"HostKeySha256": "SHA256:replace-with-server-host-key"
				},
				{
					"Name": "db-admin",
					"Host": "10.0.0.25",
					"Port": 22,
					"Username": "ops",
					"PasswordEnvironmentVariable": "MCPSERVER_DB_ADMIN_PASSWORD",
					"WorkingDirectory": "/home/ops",
					"HostKeySha256": "SHA256:replace-with-server-host-key"
				}
			]
		}
	}
}
```

Remote tooling notes:

- `ssh.exec` runs a non-interactive POSIX shell command on a configured profile and returns exit code, stdout, and stderr.
- `ssh.write_text` writes a remote text file over SFTP and can create parent directories and apply octal permissions.
- For package installs and service changes, configure passwordless `sudo` for the remote account if you need unattended automation.
- By default, host keys should be pinned with `HostKeySha256`. `AcceptUnknownHostKey` exists for controlled lab environments but is not recommended for production.

Example MCP calls:

Install and start nginx:

```json
{"jsonrpc":"2.0","id":5,"method":"tools/call","params":{"name":"ssh.exec","arguments":{"profile":"prod-web","command":"sudo apt-get update && sudo apt-get install -y nginx && sudo systemctl enable --now nginx","timeoutSeconds":900}}}
```

Write an nginx site config:

```json
{"jsonrpc":"2.0","id":6,"method":"tools/call","params":{"name":"ssh.write_text","arguments":{"profile":"prod-web","path":"/etc/nginx/sites-available/app.conf","content":"server { listen 80; server_name _; location / { proxy_pass http://127.0.0.1:5000; } }","permissions":"644"}}}
```

Install PostgreSQL and create a database:

```json
{"jsonrpc":"2.0","id":7,"method":"tools/call","params":{"name":"ssh.exec","arguments":{"profile":"db-admin","command":"sudo apt-get update && sudo apt-get install -y postgresql && sudo systemctl enable --now postgresql && sudo -u postgres createdb appdb","timeoutSeconds":900}}}
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
