# Scripts: LM Studio sync and install helpers

This folder contains helper scripts to keep the built `McpServer.Host.exe` binary and LM Studio MCP configuration in sync.

Files
- `install-lmstudio-mcp.ps1` — create or update `%USERPROFILE%\.lmstudio\mcp.json` with a server entry that points at the Release host exe by default.
- `copy-to-lmstudio.ps1` — legacy helper that copies a specified source exe into the LM Studio user-files folder.
- `sync-and-deploy.ps1` — convenience script that can optionally build the solution, copy Release exe into the Debug output path expected by the plugin, and copy the Release exe to LM Studio user-files.

- `watch-and-sync.ps1` — long-running watcher that mirrors changes from the Release exe into the Debug path and LM Studio `user-files`. Use `-Once` to run a single sync and exit.

Usage

From the repo root:

PowerShell (recommended):

```powershell
# Install or update LM Studio MCP config:
.\scripts\install-lmstudio-mcp.ps1

# Just copy the Release exe to LM Studio (no build):
.\scripts\copy-to-lmstudio.ps1

# Build Release, then sync and deploy:
.\scripts\sync-and-deploy.ps1 -BuildRelease
```

Notes
- The scripts use absolute default paths matching the repo layout on the dev machine. Adjust parameters if your workspace is located elsewhere.
- `sync-and-deploy.ps1` returns non-zero exit codes on failure (2=build failed, 3=release exe missing, 4=copy to debug failed, 5=copy to LM Studio failed).
