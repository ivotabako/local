param(
    [string]$BackendRoot = "c:\projects\local\src\Backend"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $BackendRoot)) {
    throw "Backend root not found: $BackendRoot"
}

$violations = [System.Collections.Generic.List[string]]::new()

function Get-JsonPropertyValue {
    param(
        [Parameter()] $Object,
        [Parameter(Mandatory)] [string]$PropertyName
    )

    if ($null -eq $Object) {
        return $null
    }

    $property = $Object.PSObject.Properties[$PropertyName]
    if ($null -eq $property) {
        return $null
    }

    return $property.Value
}

$appsettingsFiles = Get-ChildItem -Path $BackendRoot -Filter "appsettings*.json" -File -Recurse |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }

foreach ($file in $appsettingsFiles) {
    $content = Get-Content -Raw $file.FullName | ConvertFrom-Json

    $jwtSettings = Get-JsonPropertyValue -Object $content -PropertyName "Jwt"
    if ($null -ne $jwtSettings) {
        $signingKey = [string](Get-JsonPropertyValue -Object $jwtSettings -PropertyName "SigningKey")
        if (-not [string]::IsNullOrWhiteSpace($signingKey)) {
            $violations.Add("$($file.FullName): Jwt:SigningKey must be empty in tracked appsettings files. Use user secrets or environment variables.")
        }
    }

    $mongoSettings = Get-JsonPropertyValue -Object $content -PropertyName "MongoDb"
    if ($null -ne $mongoSettings) {
        $connectionString = [string](Get-JsonPropertyValue -Object $mongoSettings -PropertyName "ConnectionString")
        if (-not [string]::IsNullOrWhiteSpace($connectionString)) {
            $violations.Add("$($file.FullName): MongoDb:ConnectionString must be empty in tracked appsettings files. Use user secrets or environment variables.")
        }
    }

    $authSettings = Get-JsonPropertyValue -Object $content -PropertyName "Auth"
    $authUsers = Get-JsonPropertyValue -Object $authSettings -PropertyName "Users"
    if ($null -ne $authUsers) {
        $violations.Add("$($file.FullName): Auth:Users must not be stored in tracked appsettings files. Use user secrets or environment variables.")
    }
}

$authProgramPath = Join-Path $BackendRoot "LocalEnterprise.Auth\Program.cs"
if (Test-Path $authProgramPath) {
    $authProgram = Get-Content -Raw $authProgramPath
    if ($authProgram -match 'string\.Equals\(x\.Password,\s*request\.Password') {
        $violations.Add("${authProgramPath}: raw configured password comparison is forbidden. Use a framework password hasher.")
    }

    if ($authProgram -match 'new\s+JwtSecurityToken\s*\(' -or $authProgram -match 'JwtSecurityTokenHandler\s*\(\)\.WriteToken') {
        $violations.Add("${authProgramPath}: manual JWT token construction is forbidden in LocalEnterprise.Auth. Use the configured OpenID/OAuth provider.")
    }
}

if ($violations.Count -gt 0) {
    throw (@(
        "Backend standards violations found:",
        ($violations | ForEach-Object { "- $_" })
    ) -join [Environment]::NewLine)
}

Write-Host "Backend standards check passed."