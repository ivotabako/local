$ErrorActionPreference = "Stop"

Write-Host "[1/7] Checking .NET SDK..."
$dotnetInfo = dotnet --info
if (-not $dotnetInfo) {
    throw "dotnet --info returned no output"
}

Write-Host "[2/7] Validating MCP package metadata..."
& "$PSScriptRoot\04_validate_mcp_commands.ps1"

Write-Host "[3/7] Verifying Microsoft Learn MCP endpoint..."
$doctorJson = npx -y @microsoft/learn-cli doctor --format json
$doctor = $doctorJson | ConvertFrom-Json
if (-not $doctor.ok -or -not $doctor.mcp.connected) {
    throw "Microsoft Learn MCP connectivity check failed"
}

Write-Host "[4/7] Building sample .NET API solution (if present)..."
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

Write-Host "[5/7] Checking MongoDB MCP connection placeholder..."
$mcpConfigPath = "c:\projects\local\.vscode\mcp.json"
$mcp = Get-Content -Raw $mcpConfigPath | ConvertFrom-Json
$mongoConn = $mcp.mcpServers.mongodb.env.MDB_MCP_CONNECTION_STRING
if ($mongoConn -eq "SET_ME") {
    Write-Warning "MongoDB MCP is configured but MDB_MCP_CONNECTION_STRING is still SET_ME."
} else {
    Write-Host "MongoDB MCP connection string is set."
}

Write-Host "[6/7] Verifying model policy is installed in Ollama..."
$modelsPolicyPath = "c:\projects\local\config\models\models.policy.json"
$modelsPolicy = Get-Content -Raw $modelsPolicyPath | ConvertFrom-Json

$requiredPrimaryModels = @(
    $modelsPolicy.roles.coding.primary,
    $modelsPolicy.roles.reasoning.primary,
    $modelsPolicy.roles.embeddings.primary
) | Where-Object { $_ } | Select-Object -Unique

$requiredFallbackModels = @(
    $modelsPolicy.roles.coding.fallback,
    $modelsPolicy.roles.reasoning.fallback,
    $modelsPolicy.roles.embeddings.fallback
) | Where-Object { $_ } | Select-Object -Unique

$installedModelNames = (& ollama list | Select-Object -Skip 1 | ForEach-Object {
    $line = $_.Trim()
    if ($line) { ($line -split "\s+")[0] }
})

function Test-ModelInstalled {
    param([Parameter(Mandatory)] [string]$ModelName)

    if ($installedModelNames -contains $ModelName) {
        return $true
    }

    if ($ModelName -notmatch ":") {
        if ($installedModelNames -contains "$ModelName`:latest") {
            return $true
        }
    }

    return $false
}

$missingPrimaryModels = $requiredPrimaryModels | Where-Object { -not (Test-ModelInstalled -ModelName $_) }
if ($missingPrimaryModels.Count -gt 0) {
    throw "Missing primary policy models in Ollama: $($missingPrimaryModels -join ', ')"
}
Write-Host "All primary policy models are installed in Ollama."

$missingFallbackModels = $requiredFallbackModels | Where-Object { -not (Test-ModelInstalled -ModelName $_) }
if ($missingFallbackModels.Count -gt 0) {
    Write-Warning "Missing fallback policy models in Ollama: $($missingFallbackModels -join ', '). Consider pulling them for zero-downtime fallback."
} else {
    Write-Host "All fallback policy models are installed in Ollama."
}

Write-Host "[7/7] Verifying Cline MCP is synchronized..."
$clineMcpPath = Join-Path $env:APPDATA "Code\User\globalStorage\saoudrizwan.claude-dev\settings\cline_mcp_settings.json"
if (-not (Test-Path $clineMcpPath)) {
    throw "Cline MCP settings file not found: $clineMcpPath"
}

$clineMcp = Get-Content -Raw $clineMcpPath | ConvertFrom-Json
if (-not $clineMcp.mcpServers) {
    throw "Cline MCP settings file has no mcpServers object: $clineMcpPath"
}

$workspaceServers = $mcp.mcpServers.PSObject.Properties.Name | Sort-Object
$clineServers = $clineMcp.mcpServers.PSObject.Properties.Name | Sort-Object

$missingInCline = $workspaceServers | Where-Object { $clineServers -notcontains $_ }
if ($missingInCline.Count -gt 0) {
    throw "Cline MCP is not synchronized. Missing servers: $($missingInCline -join ', ')"
}
Write-Host "Cline MCP settings include all workspace MCP servers."

Write-Host "Confidence checks completed successfully."
