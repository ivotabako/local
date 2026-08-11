$ErrorActionPreference = "Stop"

$workspaceMcpPath = "c:\projects\local\.vscode\mcp.json"
$clineMcpPath = Join-Path $env:APPDATA "Code\User\globalStorage\saoudrizwan.claude-dev\settings\cline_mcp_settings.json"

if (-not (Test-Path $workspaceMcpPath)) {
    throw "Workspace MCP config not found: $workspaceMcpPath"
}

$workspaceMcpJson = Get-Content -Raw $workspaceMcpPath | ConvertFrom-Json
if (-not $workspaceMcpJson.mcpServers) {
    throw "Workspace MCP config has no mcpServers object: $workspaceMcpPath"
}

$clineParent = Split-Path -Parent $clineMcpPath
if (-not (Test-Path $clineParent)) {
    New-Item -ItemType Directory -Path $clineParent -Force | Out-Null
}

Get-Content -Raw $workspaceMcpPath | Set-Content -Path $clineMcpPath -Encoding UTF8

$serverNames = $workspaceMcpJson.mcpServers.PSObject.Properties.Name
Write-Host "Synced MCP config to Cline."
Write-Host "Cline MCP path: $clineMcpPath"
Write-Host "Servers synced: $($serverNames -join ', ')"
