param(
    [string]$WorkspaceRoot = (Resolve-Path "$PSScriptRoot\..\..").Path,
    [string]$CodingModel = "qwen2.5:7b-instruct",
    [string]$ReasoningModel = "llama3:latest",
    [string]$EmbeddingModel = "nomic-embed-text"
)

$ErrorActionPreference = "Stop"

Write-Host "[1/4] Validating environment..."
& "$PSScriptRoot\01_validate_environment.ps1"

Write-Host "[2/4] Installing recommended VS Code extension(s)..."
& "$PSScriptRoot\02_install_agent_extension.ps1"

Write-Host "[3/4] Pulling local models..."
& "$PSScriptRoot\03_pull_models.ps1" -Models @($CodingModel, $ReasoningModel, $EmbeddingModel)

Write-Host "[4/6] Verifying MCP server command paths via npx metadata check..."
& "$PSScriptRoot\04_validate_mcp_commands.ps1"

Write-Host "[5/6] Running confidence checks..."
& "$PSScriptRoot\05_confidence_check.ps1"

Write-Host "[6/6] Syncing workspace MCP to Cline MCP settings..."
& "$PSScriptRoot\06_sync_cline_mcp.ps1"

Write-Host "Bootstrap completed successfully."
