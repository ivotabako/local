param(
    [Parameter(Mandatory)]
    [string[]]$Model,
    [string]$TaskFile = (Resolve-Path "$PSScriptRoot\..\..\benchmarks\tasks.json").Path,
    [string]$OutputDir = (Resolve-Path "$PSScriptRoot\..\..").Path + "\\artifacts"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir | Out-Null
}

$tasks = Get-Content -Raw -Path $TaskFile | ConvertFrom-Json
$results = @()

foreach ($m in $Model) {
    foreach ($t in $tasks) {
        $body = @{
            model = $m
            prompt = $t.prompt
            stream = $false
        } | ConvertTo-Json -Depth 6

        $sw = [System.Diagnostics.Stopwatch]::StartNew()
        try {
            $response = Invoke-RestMethod -Method Post -Uri "http://127.0.0.1:11434/api/generate" -ContentType "application/json" -Body $body
            $sw.Stop()

            $results += [pscustomobject]@{
                timestamp = (Get-Date).ToString("s")
                model = $m
                taskId = $t.id
                latencyMs = $sw.ElapsedMilliseconds
                responseChars = ($response.response | Out-String).Length
                evalCount = $response.eval_count
                evalDurationNs = $response.eval_duration
            }
        }
        catch {
            $sw.Stop()
            $results += [pscustomobject]@{
                timestamp = (Get-Date).ToString("s")
                model = $m
                taskId = $t.id
                latencyMs = $sw.ElapsedMilliseconds
                error = $_.Exception.Message
            }
        }
    }
}

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outFile = Join-Path $OutputDir "ollama-benchmark-$stamp.json"
$results | ConvertTo-Json -Depth 6 | Set-Content -Path $outFile

Write-Host "Benchmark completed: $outFile"
$results | Format-Table -AutoSize | Out-Host
