$ErrorActionPreference = "Stop"

$requiredFiles = @(
    "config/models/models.policy.json",
    "config/routing/routing.policy.yaml",
    "config/security/security.policy.yaml",
    "config/mcp/mcp.servers.template.json"
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path $file)) {
        throw "Missing required policy/config file: $file"
    }
}

# Validate JSON files
Get-Content -Raw "config/models/models.policy.json" | ConvertFrom-Json | Out-Null
Get-Content -Raw "config/mcp/mcp.servers.template.json" | ConvertFrom-Json | Out-Null

Write-Host "Policy checks passed."
