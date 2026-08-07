$ErrorActionPreference = "Stop"

$extensions = @(
    "saoudrizwan.claude-dev"
)

foreach ($ext in $extensions) {
    Write-Host "Installing VS Code extension: $ext"
    code --install-extension $ext --force | Out-Host
}

Write-Host "Extension install step completed."
