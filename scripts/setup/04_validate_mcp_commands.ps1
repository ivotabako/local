$ErrorActionPreference = "Stop"

$packages = @(
    "@modelcontextprotocol/server-filesystem",
    "@cyanheads/git-mcp-server",
    "@modelcontextprotocol/server-github",
    "@playwright/mcp",
    "@angular/cli",
    "roslyn-codelens-mcp",
    "mcp-remote",
    "@microsoft/learn-cli",
    "mongodb-mcp-server",
    "mcp-docker-server",
    "@modelcontextprotocol/server-postgres"
)

foreach ($p in $packages) {
    Write-Host "Checking npm metadata for $p"
    npm view $p version | Out-Host
}

Write-Host "MCP package metadata checks completed."
