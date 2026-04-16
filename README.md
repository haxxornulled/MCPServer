# McpServer

[![CI](https://github.com/haxxornulled/MCPServer/actions/workflows/ci.yml/badge.svg)](https://github.com/haxxornulled/MCPServer/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![C%23 14](https://img.shields.io/badge/C%23-14-239120?logo=csharp)](https://learn.microsoft.com/dotnet/csharp/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Production-oriented MCP server starter for .NET 10 / C# 14 / Visual Studio 2026.

## Current Release

`v0.1.4` is the current validated release.
It adds release-polish updates around LM Studio launch guidance, repository metadata, and agent extension-point instructions on top of the SSH and local tooling work.

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

## LM Studio

LM Studio supports local MCP programs via `mcp.json`. This server uses the MCP stdio transport, so you can register it as a local program.

Windows example:

```json
{
	"mcpServers": {
		"mcpserver-filesystem": {
			"command": "D:/McpServerRepo/src/McpServer.Host/bin/Release/net10.0/McpServer.Host.exe"
		}
	}
}
```

Recommended workflow:

1. Build the host in `Release` before connecting from LM Studio so the executable path points at the newest binary.
2. In LM Studio, open the MCP settings and add the server entry to `mcp.json`.
3. Restart the MCP server from LM Studio after every rebuild so it picks up the latest executable.
4. If you prefer `Debug` during development, change the path explicitly to `bin/Debug/net10.0/McpServer.Host.exe` rather than relying on `dotnet run`.

Compatibility notes:

- The server negotiates MCP protocol versions and falls back to `2025-03-26` for unknown client versions to stay compatible with current MCP hosts such as LM Studio.
- The server supports `ping`, which some MCP hosts use as a connection health check.
- The server exposes `shell.exec` for non-interactive command execution inside the configured workspace.
- The server can optionally expose `ssh.exec` and `ssh.write_text` when SSH profiles are enabled in configuration.
- `shell.exec` accepts both `workspace` and LM Studio's `/mcpserver-filesystem` alias as workspace roots for `workingDirectory`.
- Logs are written to stderr and `logs/`, not stdout, so stdio MCP traffic stays clean.

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
