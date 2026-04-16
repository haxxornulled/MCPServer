Add GitHub Packages NuGet source helper
=====================================

This folder contains `add-github-packages-source.ps1`, a helper script that registers
the `mcpserver-gh` NuGet source using the `GH_PACKAGE_TOKEN` environment variable.

Usage (local):

1. Create a new GitHub Personal Access Token (PAT) with `read:packages` (and `write:packages` if you publish).
2. Set it for the current PowerShell session:

```powershell
$env:GH_PACKAGE_TOKEN = '<NEW_PAT>'
```

3. Run the helper (it prefers `nuget.exe` for secure storage, falls back to `dotnet`):

```powershell
.\scripts\add-github-packages-source.ps1 -Username <your-github-username>
```

4. Verify and restore:

```powershell
dotnet nuget list source
dotnet restore
```

Security notes:
- Do NOT commit tokens or echo them in logs.
- Revoke tokens you accidentally shared and create a new one.
- Prefer `nuget.exe` on Windows so credentials are stored securely.
