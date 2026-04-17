<#
Watch the Release output for `McpServer.Host.exe` and mirror changes to the Debug path
and LM Studio `user-files` automatically.

Usage:
  # Run continuously (CTRL+C to stop):
  .\scripts\watch-and-sync.ps1

  # Run once (perform initial sync and exit):
  .\scripts\watch-and-sync.ps1 -Once

Notes:
- Defaults assume repo at D:\McpServerRepo; pass `-ReleaseExe`, `-DebugExe`, or `-LmStudioDest` to override paths.
#>

param(
    [switch]$Once,
    [string]$ReleaseExe = 'D:\McpServerRepo\src\McpServer.Host\bin\Release\net10.0\McpServer.Host.exe',
    [string]$DebugExe = 'D:\McpServerRepo\src\McpServer.Host\bin\Debug\net10.0\McpServer.Host.exe',
    [string]$LmStudioDest = "$env:USERPROFILE\.lmstudio\user-files\McpServer.Host.exe",
    [int]$DebounceMs = 500
)

function Copy-Targets {
    param($src)
    if (-not (Test-Path -Path $src)) {
        Write-Warning "Source missing: $src"
        return
    }

    $releaseDir = Split-Path -Path $src -Parent
    $debugDir = Split-Path -Path $DebugExe -Parent
    if (-not (Test-Path -Path $debugDir)) { New-Item -ItemType Directory -Path $debugDir -Force | Out-Null }

    $lmDir = Split-Path -Path $LmStudioDest -Parent
    if (-not (Test-Path -Path $lmDir)) { New-Item -ItemType Directory -Path $lmDir -Force | Out-Null }

    try {
        Copy-Item -Path (Join-Path $releaseDir '*') -Destination $debugDir -Force -Recurse -ErrorAction Stop
        Write-Host "[watch] Copied Release output -> Debug: $debugDir"
    } catch { Write-Error "[watch] Failed copy->Debug: $($_.Exception.Message)" }

    try {
        $exePath = Join-Path $releaseDir 'McpServer.Host.exe'
        $dllPath = Join-Path $releaseDir 'McpServer.Host.dll'
        if (Test-Path $exePath) { Copy-Item -Path $exePath -Destination (Join-Path $lmDir 'McpServer.Host.exe') -Force -ErrorAction Stop; Write-Host "[watch] Copied EXE -> LM Studio: $lmDir\McpServer.Host.exe" }
        if (Test-Path $dllPath) { Copy-Item -Path $dllPath -Destination (Join-Path $lmDir 'McpServer.Host.dll') -Force -ErrorAction Stop; Write-Host "[watch] Copied DLL -> LM Studio: $lmDir\McpServer.Host.dll" }
    } catch { Write-Error "[watch] Failed copy->LM Studio: $($_.Exception.Message)" }
}

# Initial sync
Write-Host "[watch] Starting. Initial sync from: $ReleaseExe"
Copy-Targets -src $ReleaseExe

if ($Once) { Write-Host '[watch] -Once specified, exiting after initial sync.'; exit 0 }

$releaseDir = Split-Path -Path $ReleaseExe -Parent
if (-not (Test-Path -Path $releaseDir)) { Write-Error "Release directory not found: $releaseDir"; exit 2 }

$fsw = New-Object System.IO.FileSystemWatcher $releaseDir
$fsw.Filter = (Split-Path -Path $ReleaseExe -Leaf)
$fsw.NotifyFilter = [System.IO.NotifyFilters]'LastWrite,FileName'

$timer = $null

$onChanged = {
    if ($timer) { $timer.Stop(); $timer.Dispose() }
    $timer = New-Object System.Timers.Timer $DebounceMs
    $timer.AutoReset = $false
    $timer.Add_Elapsed({
        try {
            Write-Host "[watch] Detected change, syncing..."
            Copy-Targets -src $ReleaseExe
        } catch {
            Write-Error "[watch] Error during sync: $($_.Exception.Message)"
        }
    })
    $timer.Start()
}

[void]$fsw.add_Changed($onChanged)
[void]$fsw.add_Created($onChanged)
[void]$fsw.add_Renamed($onChanged)

Write-Host "[watch] Listening for changes in: $releaseDir (filter=$($fsw.Filter)). Press Ctrl+C to stop."

try {
    while ($true) { Start-Sleep -Seconds 1 }
} finally {
    if ($timer) { $timer.Dispose() }
    $fsw.Dispose()
    Write-Host '[watch] Exiting.'
}
