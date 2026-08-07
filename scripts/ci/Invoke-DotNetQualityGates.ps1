param(
    [string]$SolutionPath = "c:\projects\local\src\Backend\LocalEnterprise.slnx",
    [string]$Configuration = "Release",
    [string]$ResultsRoot = "c:\projects\local\artifacts\test-results"
)

$ErrorActionPreference = "Stop"

function Invoke-Checked {
    param(
        [Parameter(Mandatory)] [scriptblock]$Command,
        [Parameter(Mandatory)] [string]$StepName
    )

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$StepName failed with exit code $LASTEXITCODE"
    }
}

function Assert-CommandExists {
    param([Parameter(Mandatory)] [string]$Name)
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command not found: $Name"
    }
}

Assert-CommandExists -Name "dotnet"

if (-not (Test-Path $SolutionPath)) {
    throw "Solution not found: $SolutionPath"
}

if (-not (Test-Path $ResultsRoot)) {
    New-Item -ItemType Directory -Path $ResultsRoot | Out-Null
}

Write-Host "[1/6] Restoring solution..."
Invoke-Checked -StepName "dotnet restore" -Command {
    dotnet restore $SolutionPath | Out-Host
}

Write-Host "[2/6] Building solution with warnings as errors..."
Invoke-Checked -StepName "dotnet build" -Command {
    dotnet build $SolutionPath -c $Configuration -warnaserror | Out-Host
}

Write-Host "[3/6] Running tests with coverage..."
Invoke-Checked -StepName "dotnet test" -Command {
    dotnet test $SolutionPath -c $Configuration --no-build --collect:"XPlat Code Coverage" --results-directory $ResultsRoot --logger "trx" | Out-Host
}

Write-Host "[4/6] Verifying formatting and analyzers..."
Invoke-Checked -StepName "dotnet format" -Command {
    dotnet format $SolutionPath --verify-no-changes --severity warn | Out-Host
}

Write-Host "[5/6] Checking for vulnerable NuGet packages..."
$vulnOutput = dotnet list $SolutionPath package --vulnerable --include-transitive | Out-String
if ($LASTEXITCODE -ne 0) {
    throw "dotnet list package --vulnerable failed with exit code $LASTEXITCODE"
}
if ($vulnOutput -match "has the following vulnerable packages") {
    throw "Vulnerable NuGet packages detected. See output above."
}

Write-Host "[6/6] Running secret scan when gitleaks is available..."
if (Get-Command gitleaks -ErrorAction SilentlyContinue) {
    gitleaks detect --source c:\projects\local --no-git --verbose | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "gitleaks detect failed with exit code $LASTEXITCODE"
    }
} else {
    Write-Warning "gitleaks not found locally. CI workflow still enforces secret scanning."
}

Write-Host "DotNet backend quality gates passed."
