# Copies the latest McpServer.Host.exe to the current user's LM Studio user-files folder
param(
    [string]$SourcePath = 'D:\McpServerRepo\src\McpServer.Host\bin\Release\net10.0\McpServer.Host.exe',
    [string]$DestinationRelative = '.lmstudio\user-files\McpServer.Host.exe'
)

$dst = Join-Path -Path $env:USERPROFILE -ChildPath $DestinationRelative

if (-not (Test-Path -Path $SourcePath)) {
    Write-Error "Source not found: $SourcePath"
    exit 1
}

try {
    Copy-Item -Path $SourcePath -Destination $dst -Force -ErrorAction Stop
    Write-Host "Copied $SourcePath to $dst"
    exit 0
} catch {
    Write-Error "Failed to copy: $($_.Exception.Message)"
    exit 1
}
