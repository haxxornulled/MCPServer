param(
    [string]$ServerName = 'mcpserver-release',
    [string]$Command = 'D:\McpServerRepo\src\McpServer.Host\bin\Release\net10.0\McpServer.Host.exe',
    [string[]]$Args = @()
)

$lmStudioDir = Join-Path -Path $env:USERPROFILE -ChildPath '.lmstudio'
$mcpJsonPath = Join-Path -Path $lmStudioDir -ChildPath 'mcp.json'

if (-not (Test-Path -Path $lmStudioDir)) {
    New-Item -ItemType Directory -Path $lmStudioDir -Force | Out-Null
}

if (Test-Path -Path $mcpJsonPath) {
    $config = Get-Content -Path $mcpJsonPath -Raw | ConvertFrom-Json -AsHashtable
} else {
    $config = @{}
}

if (-not $config.ContainsKey('mcpServers')) {
    $config['mcpServers'] = @{}
}

$serverConfig = @{
    command = $Command
}

if ($Args.Count -gt 0) {
    $serverConfig['args'] = $Args
}

$config['mcpServers'][$ServerName] = $serverConfig
$config | ConvertTo-Json -Depth 10 | Set-Content -Path $mcpJsonPath -Encoding UTF8

Write-Host "Installed or updated '$ServerName' in $mcpJsonPath"
