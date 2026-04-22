param(
    [string]$ServerName = 'mcpserver-release',
    [string]$Command = 'D:\McpServerRepo\src\McpServer.Host\bin\Release\net10.0\McpServer.Host.exe',
    [string]$WorkspaceRoot = '',
    [string[]]$Args = @()
)

function Resolve-LmStudioCommand {
    param([Parameter(Mandatory = $true)][string]$Candidate)

    if ([string]::IsNullOrWhiteSpace($Candidate)) {
        throw 'A command value must be provided.'
    }

    if ([System.IO.Path]::IsPathRooted($Candidate)) {
        $resolvedPath = [System.IO.Path]::GetFullPath($Candidate)
        if (Test-Path -Path $resolvedPath) {
            return $resolvedPath
        }

        if ($Candidate.EndsWith('.exe', [System.StringComparison]::OrdinalIgnoreCase)) {
            $toolCommand = Get-Command -Name 'mcpserver' -ErrorAction SilentlyContinue
            if ($null -ne $toolCommand) {
                Write-Warning "Release host exe was not found at '$resolvedPath'. Falling back to the installed 'mcpserver' command."
                return 'mcpserver'
            }
        }

        throw "LM Studio command path was not found: $resolvedPath"
    }

    $resolvedCommand = Get-Command -Name $Candidate -ErrorAction SilentlyContinue
    if ($null -ne $resolvedCommand) {
        return $resolvedCommand.Name
    }

    return $Candidate
}

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
    command = (Resolve-LmStudioCommand -Candidate $Command)
}

$effectiveArgs = @($Args)
if (-not [string]::IsNullOrWhiteSpace($WorkspaceRoot)) {
    $effectiveArgs = @('--McpServer:Workspace:RootPath', $WorkspaceRoot) + $effectiveArgs
}

if ($effectiveArgs.Count -gt 0) {
    $serverConfig['args'] = $effectiveArgs
}

$config['mcpServers'][$ServerName] = $serverConfig
$config | ConvertTo-Json -Depth 10 | Set-Content -Path $mcpJsonPath -Encoding UTF8

Write-Host "Installed or updated '$ServerName' in $mcpJsonPath"
