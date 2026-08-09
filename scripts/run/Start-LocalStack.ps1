param(
    [switch]$SkipFrontend,
    [switch]$SkipBackend,
    [switch]$SkipAuth,
    [switch]$SkipCertificateSetup
)

$ErrorActionPreference = 'Stop'
$workspaceRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

function Resolve-CommandPath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$CommandName
    )

    $command = Get-Command $CommandName -ErrorAction SilentlyContinue
    if ($null -eq $command) {
        throw "Required command '$CommandName' was not found in PATH."
    }

    return $command.Source
}

function Start-ProcessInNewWindow {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FilePath,
        [Parameter(Mandatory = $true)]
        [string]$WorkingDirectory,
        [string[]]$ArgumentList = @(),
        [string]$EnvironmentName = 'Development'
    )

    Start-Process -FilePath $FilePath -WorkingDirectory $WorkingDirectory -ArgumentList $ArgumentList -PassThru | Out-Null
}

if (-not $SkipCertificateSetup) {
    Write-Host 'Ensuring development certificates exist...' -ForegroundColor Cyan
    & (Join-Path $PSScriptRoot 'Create-DevCertificates.ps1')
}

$dotnetPath = Resolve-CommandPath -CommandName 'dotnet'

if (-not $SkipAuth) {
    Write-Host 'Starting authorization server...' -ForegroundColor Cyan
    Start-ProcessInNewWindow -FilePath $dotnetPath -WorkingDirectory (Join-Path $workspaceRoot 'src/Backend/LocalEnterprise.Auth') -ArgumentList @('run', '--launch-profile', 'https', '--project', 'LocalEnterprise.Auth.csproj') -EnvironmentName 'Development'
}

if (-not $SkipBackend) {
    Write-Host 'Starting backend API...' -ForegroundColor Cyan
    Start-ProcessInNewWindow -FilePath $dotnetPath -WorkingDirectory (Join-Path $workspaceRoot 'src/Backend/LocalEnterprise.Api') -ArgumentList @('run', '--launch-profile', 'https', '--project', 'LocalEnterprise.Api.csproj') -EnvironmentName 'Development'
}

if (-not $SkipFrontend) {
    $frontendPath = Join-Path $workspaceRoot 'src/Frontend/localenterprise-web'
    if (-not (Test-Path $frontendPath)) {
        throw "Frontend directory not found: $frontendPath"
    }

    Write-Host 'Starting frontend client...' -ForegroundColor Cyan
    $npmPath = Resolve-CommandPath -CommandName 'npm.cmd'
    $sslCertPath = Join-Path $PSScriptRoot 'localhost.crt'
    $sslKeyPath = Join-Path $PSScriptRoot 'localhost.key'
    if (-not (Test-Path $sslCertPath)) {
        throw "Frontend certificate file not found: $sslCertPath"
    }

    if (-not (Test-Path $sslKeyPath)) {
        throw "Frontend key file not found: $sslKeyPath"
    }

    $frontendCommand = "set SSL_CRT_FILE=$sslCertPath && set SSL_KEY_FILE=$sslKeyPath && set NODE_OPTIONS=--openssl-legacy-provider && `"$npmPath`" start -- --ssl --ssl-cert `"$sslCertPath`" --ssl-key `"$sslKeyPath`""

    Start-Process -FilePath 'cmd.exe' -WorkingDirectory $frontendPath -ArgumentList @('/c', $frontendCommand) -PassThru | Out-Null

    Write-Host "Frontend launched with cert: $sslCertPath" -ForegroundColor DarkGray
}

Write-Host 'Startup command completed.' -ForegroundColor Green
Write-Host 'Auth server expected at: https://localhost:7081' -ForegroundColor Yellow
Write-Host 'Backend expected at: https://localhost:7243' -ForegroundColor Yellow
Write-Host 'Frontend expected at: https://localhost:4200' -ForegroundColor Yellow
Write-Host 'Assumption: MongoDB is already running locally.' -ForegroundColor Yellow
