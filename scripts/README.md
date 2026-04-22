# Scripts: LM Studio sync and install helpers

This folder contains helper scripts to keep the built `McpServer.Host.exe` binary and LM Studio MCP configuration in sync.

Files
- `install-lmstudio-mcp.ps1` — create or update `%USERPROFILE%\.lmstudio\mcp.json` with a server entry that points at the Release host exe by default, or falls back to the installed `mcpserver` tool if needed.
- `Disable-LmStudioSandbox.ps1` — remove LM Studio's `js-code-sandbox` plugin from active settings and move the plugin folder aside so MCP-only agent runs stay cleaner.
- `Enable-LmStudioAgentMode.ps1` — disable the sandbox, add the release server wildcard skip pattern, and prepare LM Studio for MCP agent runs.
- `Test-LmStudioAgentMode.ps1` — check that LM Studio is configured for MCP tool use, confirm the MCP handshake in the latest LM Studio server log, and warn if unrelated sandbox plugins or context overflows are still active.
- `Test-LmStudioWorkspaceAccess.ps1` — run a model-mediated workspace access probe through the OpenAI-compatible endpoint and require `workspace.set_root` plus `fs.list_directory` to succeed.
- `Invoke-LmStudioGpuWorkout.ps1` — use LM Studio's native v1 API to load a model with a larger context, parallel sessions, and concurrent stateful chat requests for a heavier GPU workout.
- `Invoke-LmStudioStructuredOutputProbe.ps1` — call `/v1/chat/completions` with a JSON schema and verify structured output on the LM Studio server.
- `Watch-ServerLogs.ps1` — follow the newest `logs/mcp-server-*.log` file and stream new entries to the console.
- `copy-to-lmstudio.ps1` — legacy helper that copies a specified source exe into the LM Studio user-files folder.
- `sync-and-deploy.ps1` — convenience script that can optionally build the solution, copy Release exe into the Debug output path expected by the plugin, and copy the Release exe to LM Studio user-files.

- `watch-and-sync.ps1` — long-running watcher that mirrors changes from the Release exe into the Debug path and LM Studio `user-files`. Use `-Once` to run a single sync and exit.

Usage

From the repo root:

PowerShell (recommended):

```powershell
# Install or update LM Studio MCP config:
.\scripts\install-lmstudio-mcp.ps1

# Disable the JS sandbox so MCP-only runs stay clean:
.\scripts\Disable-LmStudioSandbox.ps1

# Enable MCP agent mode for the release server:
.\scripts\Enable-LmStudioAgentMode.ps1

# Verify LM Studio is ready for MCP/agent runs:
.\scripts\Test-LmStudioAgentMode.ps1

# Verify a model can reach the configured workspace through MCP tools:
.\scripts\Test-LmStudioWorkspaceAccess.ps1 -Model google/gemma-4-e4b

# Run a heavier LM Studio API workout:
.\scripts\Invoke-LmStudioGpuWorkout.ps1 -WorkerCount 2 -TurnsPerWorker 2

# Verify structured output support:
.\scripts\Invoke-LmStudioStructuredOutputProbe.ps1 -PrettyPrint

# Watch the server logs in real time:
.\scripts\Watch-ServerLogs.ps1

# Just copy the Release exe to LM Studio (no build):
.\scripts\copy-to-lmstudio.ps1

# Build Release, then sync and deploy:
.\scripts\sync-and-deploy.ps1 -BuildRelease
```

Notes
- The scripts use absolute default paths matching the repo layout on the dev machine. Adjust parameters if your workspace is located elsewhere.
- `install-lmstudio-mcp.ps1` accepts `-WorkspaceRoot` to pass `McpServer:Workspace:RootPath` through to the host when you want LM Studio to open a specific project folder instead of the build-output workspace.
- `sync-and-deploy.ps1` returns non-zero exit codes on failure (2=build failed, 3=release exe missing, 4=copy to debug failed, 5=copy to LM Studio failed).
