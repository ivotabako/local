$ErrorActionPreference = 'Stop'

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

function Ensure-DevCertificate {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,
        [Parameter(Mandatory = $true)]
        [string]$Hostname
    )

    $certificatePath = Join-Path $PSScriptRoot "$Name.pfx"

    if (-not (Test-Path $certificatePath)) {
        Write-Host "Creating development certificate for $Hostname..." -ForegroundColor Cyan
        $password = ConvertTo-SecureString 'localenterprise-dev' -AsPlainText -Force
        $cert = New-SelfSignedCertificate -CertStoreLocation Cert:\CurrentUser\My -DnsName $Hostname -FriendlyName $Name -NotAfter (Get-Date).AddYears(1)
        Export-PfxCertificate -Cert $cert -FilePath $certificatePath -Password $password | Out-Null
        Write-Host "Certificate created at $certificatePath" -ForegroundColor Green
    }
    else {
        Write-Host "Certificate already exists at $certificatePath" -ForegroundColor Yellow
    }
}

function Ensure-CertificateTrusted {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name
    )

    $pfxPath = Join-Path $PSScriptRoot "$Name.pfx"
    $cerPath = Join-Path $PSScriptRoot "$Name.cer"
    $password = ConvertTo-SecureString 'localenterprise-dev' -AsPlainText -Force
    $pfxData = Get-PfxData -FilePath $pfxPath -Password $password
    $certificate = $pfxData.EndEntityCertificates | Select-Object -First 1

    if ($null -eq $certificate) {
        throw "Unable to read certificate from $pfxPath"
    }

    $isTrusted = Get-ChildItem Cert:\CurrentUser\Root |
        Where-Object { $_.Thumbprint -eq $certificate.Thumbprint } |
        Select-Object -First 1

    if ($null -eq $isTrusted) {
        Write-Host "Trusting certificate $Name in CurrentUser Root store..." -ForegroundColor Cyan
        Export-Certificate -Cert $certificate -FilePath $cerPath -Force | Out-Null
        Import-Certificate -FilePath $cerPath -CertStoreLocation Cert:\CurrentUser\Root | Out-Null
        Write-Host "Certificate trusted: $Name" -ForegroundColor Green
    }
    else {
        Write-Host "Certificate already trusted: $Name" -ForegroundColor Yellow
    }
}

Ensure-DevCertificate -Name 'LocalEnterprise.Api.Dev' -Hostname 'localhost'
Ensure-DevCertificate -Name 'LocalEnterprise.Web.Dev' -Hostname 'localhost'
Ensure-CertificateTrusted -Name 'LocalEnterprise.Api.Dev'
Ensure-CertificateTrusted -Name 'LocalEnterprise.Web.Dev'

Write-Host 'Ensuring .NET HTTPS development certificate is trusted...' -ForegroundColor Cyan
$dotnetPath = Resolve-CommandPath -CommandName 'dotnet'
& $dotnetPath dev-certs https --trust | Out-Null

$frontendPfxPath = Join-Path $PSScriptRoot 'LocalEnterprise.Web.Dev.pfx'
$frontendCrtPath = Join-Path $PSScriptRoot 'localhost.crt'
$frontendKeyPath = Join-Path $PSScriptRoot 'localhost.key'

if (-not (Test-Path $frontendCrtPath) -or -not (Test-Path $frontendKeyPath)) {
    Write-Host 'Exporting frontend HTTPS certificate and key for Angular dev server...' -ForegroundColor Cyan
    $openSslPath = Resolve-CommandPath -CommandName 'openssl'
    & $openSslPath pkcs12 -in $frontendPfxPath -clcerts -nokeys -out $frontendCrtPath -passin pass:localenterprise-dev | Out-Null
    & $openSslPath pkcs12 -in $frontendPfxPath -nocerts -nodes -out $frontendKeyPath -passin pass:localenterprise-dev | Out-Null
    Write-Host "Frontend certificate files created: $frontendCrtPath, $frontendKeyPath" -ForegroundColor Green
}
else {
    Write-Host "Frontend certificate files already exist: $frontendCrtPath, $frontendKeyPath" -ForegroundColor Yellow
}
