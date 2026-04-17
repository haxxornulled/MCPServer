# Sync release binary to Debug path and deploy to LM Studio user-files
param(
    [switch]$BuildRelease,
    [string]$SolutionPath = 'D:\McpServerRepo\McpServer.slnx',
    [string]$ReleaseExe = 'D:\McpServerRepo\src\McpServer.Host\bin\Release\net10.0\McpServer.Host.exe',
    [string]$DebugExe = 'D:\McpServerRepo\src\McpServer.Host\bin\Debug\net10.0\McpServer.Host.exe',
    [string]$LmStudioDest = "$env:USERPROFILE\.lmstudio\user-files\McpServer.Host.exe"
)

Write-Host "(sync-and-deploy) Starting"

if ($BuildRelease) {
    Write-Host "Building solution (Release)..."
    $build = dotnet build $SolutionPath -c Release --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Error "dotnet build failed"
        exit 2
    }
}

if (-not (Test-Path -Path $ReleaseExe)) {
    Write-Error "Release exe not found: $ReleaseExe"
    exit 3
}

$releaseDir = Split-Path -Path $ReleaseExe -Parent
if (-not (Test-Path -Path $releaseDir)) {
    Write-Error "Release directory not found: $releaseDir"
    exit 3
}

$debugDir = Split-Path -Path $DebugExe -Parent
if (-not (Test-Path -Path $debugDir)) {
    New-Item -ItemType Directory -Path $debugDir -Force | Out-Null
}

try {
    Copy-Item -Path (Join-Path $releaseDir '*') -Destination $debugDir -Force -Recurse -ErrorAction Stop
    Write-Host "Copied Release output -> Debug directory: $debugDir"
} catch {
    Write-Error "Failed to copy Release output to Debug: $($_.Exception.Message)"
    exit 4
}

$lmDir = Split-Path -Path $LmStudioDest -Parent
if (-not (Test-Path -Path $lmDir)) {
    New-Item -ItemType Directory -Path $lmDir -Force | Out-Null
}

try {
    $exePath = Join-Path $releaseDir 'McpServer.Host.exe'
    $dllPath = Join-Path $releaseDir 'McpServer.Host.dll'
    if (Test-Path $exePath) { Copy-Item -Path $exePath -Destination $LmStudioDest -Force -ErrorAction Stop; Write-Host "Copied EXE -> LM Studio: $LmStudioDest" }
    if (Test-Path $dllPath) { Copy-Item -Path $dllPath -Destination (Join-Path $lmDir 'McpServer.Host.dll') -Force -ErrorAction Stop; Write-Host "Copied DLL -> LM Studio: $($lmDir)\McpServer.Host.dll" }
    if (-not (Test-Path $exePath) -and -not (Test-Path $dllPath)) { Write-Warning "No exe or dll found in Release to copy to LM Studio" }
} catch {
    Write-Error "Failed to copy to LM Studio: $($_.Exception.Message)"
    exit 5
}

Write-Host "(sync-and-deploy) Done"
exit 0
