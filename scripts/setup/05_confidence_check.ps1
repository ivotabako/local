$ErrorActionPreference = "Stop"

Write-Host "[1/5] Checking .NET SDK..."
$dotnetInfo = dotnet --info
if (-not $dotnetInfo) {
    throw "dotnet --info returned no output"
}

Write-Host "[2/5] Validating MCP package metadata..."
& "$PSScriptRoot\04_validate_mcp_commands.ps1"

Write-Host "[3/5] Verifying Microsoft Learn MCP endpoint..."
$doctorJson = npx -y @microsoft/learn-cli doctor --format json
$doctor = $doctorJson | ConvertFrom-Json
if (-not $doctor.ok -or -not $doctor.mcp.connected) {
    throw "Microsoft Learn MCP connectivity check failed"
}

Write-Host "[4/5] Building sample .NET API solution (if present)..."
$sampleSolution = "c:\projects\local\src\Backend\LocalEnterprise.slnx"
if (Test-Path $sampleSolution) {
    dotnet build $sampleSolution -c Debug | Out-Host
} else {
    $fallbackSolution = "c:\projects\local\BackendSmoke.slnx"
    if (Test-Path $fallbackSolution) {
        Write-Warning "Primary backend solution missing; falling back to BackendSmoke.slnx."
        dotnet build $fallbackSolution -c Debug | Out-Host
    } else {
        Write-Warning "No sample solution found. Skipping build check."
    }
}

Write-Host "[5/5] Checking MongoDB MCP connection placeholder..."
$mcpConfigPath = "c:\projects\local\.vscode\mcp.json"
$mcp = Get-Content -Raw $mcpConfigPath | ConvertFrom-Json
$mongoConn = $mcp.mcpServers.mongodb.env.MDB_MCP_CONNECTION_STRING
if ($mongoConn -eq "SET_ME") {
    Write-Warning "MongoDB MCP is configured but MDB_MCP_CONNECTION_STRING is still SET_ME."
} else {
    Write-Host "MongoDB MCP connection string is set."
}

Write-Host "Confidence checks completed successfully."
