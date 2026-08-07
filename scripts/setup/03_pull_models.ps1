param(
    [Parameter(Mandatory)]
    [string[]]$Models
)

$ErrorActionPreference = "Stop"

$installed = (& ollama list | Select-Object -Skip 1 | ForEach-Object {
    $line = $_.Trim()
    if ($line) { ($line -split "\s+")[0] }
})

foreach ($model in $Models) {
    if ($installed -contains $model) {
        Write-Host "Model already present: $model"
        continue
    }

    Write-Host "Pulling model: $model"
    ollama pull $model | Out-Host
}

Write-Host "Model sync completed."
