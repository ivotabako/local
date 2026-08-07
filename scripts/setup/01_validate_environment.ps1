$ErrorActionPreference = "Stop"

function Test-Command {
    param([Parameter(Mandatory)] [string]$Name)
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if (-not $cmd) {
        throw "Required command not found: $Name"
    }
}

$required = @("git", "node", "npm", "ollama", "code")
foreach ($r in $required) {
    Test-Command -Name $r
}

$ramGb = [math]::Round((Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory / 1GB, 2)
if ($ramGb -lt 16) {
    Write-Warning "RAM below 16GB. Recommended for smoother local LLM use: 16GB+"
}

$gpu = Get-CimInstance Win32_VideoController | Where-Object { $_.Name -match "NVIDIA|AMD|Intel" } | Select-Object -First 1
if (-not $gpu) {
    Write-Warning "No common GPU detected. CPU-only inference may be slower."
}

Write-Host "Environment validation passed."
Write-Host "RAM(GB): $ramGb"
if ($gpu) {
    Write-Host "GPU: $($gpu.Name)"
}
