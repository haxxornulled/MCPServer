param(
    [string]$InferenceBaseUrl = 'http://172.18.32.1:1234',
    [string]$Model = '',
    [string]$WorkspaceRoot = 'D:\PF-World-Of-Warcraft-Framework',
    [string]$ExpectedEntry = 'PF_Framework.toc',
    [int]$InferenceTimeoutSeconds = 180
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scenarioPath = Join-Path -Path $PSScriptRoot -ChildPath 'inference-workspace-scenarios.json'

if (-not (Test-Path -LiteralPath $WorkspaceRoot)) {
    throw "Workspace root does not exist: $WorkspaceRoot"
}

if (-not (Test-Path -LiteralPath (Join-Path -Path $WorkspaceRoot -ChildPath $ExpectedEntry))) {
    throw "Expected entry '$ExpectedEntry' was not found under $WorkspaceRoot"
}

Write-Host "Testing LM Studio workspace access through $InferenceBaseUrl"
Write-Host "Workspace root: $WorkspaceRoot"
Write-Host "Expected entry: $ExpectedEntry"

$harness = Join-Path -Path $PSScriptRoot -ChildPath 'Invoke-InferenceToolSmokeTest.ps1'
$includeTools = @('workspace.set_root', 'fs.list_directory', 'workspace.inspect')

if ([string]::IsNullOrWhiteSpace($Model)) {
    & $harness `
        -InferenceBaseUrl $InferenceBaseUrl `
        -ScenarioPath $scenarioPath `
        -IncludeTool $includeTools `
        -InferenceTimeoutSeconds $InferenceTimeoutSeconds `
        -McpTimeoutSeconds 30 `
        -MaxConversationTurns 4
} else {
    & $harness `
        -InferenceBaseUrl $InferenceBaseUrl `
        -Model $Model `
        -ScenarioPath $scenarioPath `
        -IncludeTool $includeTools `
        -InferenceTimeoutSeconds $InferenceTimeoutSeconds `
        -McpTimeoutSeconds 30 `
        -MaxConversationTurns 4
}
