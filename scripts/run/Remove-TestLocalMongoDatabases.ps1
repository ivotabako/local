param(
    [string]$ConnectionString = $(if ($env:MONGODB_CONNECTION_STRING) { $env:MONGODB_CONNECTION_STRING } elseif ($env:MongoDb__ConnectionString) { $env:MongoDb__ConnectionString } else { $null }),
    [string]$Prefix = 'test-local',
    [string]$Username = $(if ($env:MONGODB_USERNAME) { $env:MONGODB_USERNAME } elseif ($env:MongoDb__Username) { $env:MongoDb__Username } else { $null }),
    [string]$Password = $(if ($env:MONGODB_PASSWORD) { $env:MONGODB_PASSWORD } elseif ($env:MongoDb__Password) { $env:MongoDb__Password } else { $null }),
    [string]$AuthenticationDatabase = 'admin',
    [switch]$ListOnly
)

$ErrorActionPreference = 'Stop'

function Resolve-MongoShell {
    foreach ($candidate in @('mongosh', 'mongo')) {
        $command = Get-Command $candidate -ErrorAction SilentlyContinue
        if ($null -ne $command) {
            return $command.Source
        }
    }

    throw "Neither 'mongosh' nor 'mongo' was found in PATH. Install the MongoDB shell and try again."
}

function Get-MongoArguments {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConnectionStringValue,
        [string]$UsernameValue,
        [string]$PasswordValue,
        [string]$AuthDatabaseValue
    )

    $arguments = New-Object System.Collections.Generic.List[string]
    if (-not [string]::IsNullOrWhiteSpace($ConnectionStringValue)) {
        $arguments.Add($ConnectionStringValue)
    }

    if (-not [string]::IsNullOrWhiteSpace($UsernameValue)) {
        $arguments.Add('--username')
        $arguments.Add($UsernameValue)
    }

    if (-not [string]::IsNullOrWhiteSpace($PasswordValue)) {
        $arguments.Add('--password')
        $arguments.Add($PasswordValue)
    }

    if (-not [string]::IsNullOrWhiteSpace($AuthDatabaseValue)) {
        $arguments.Add('--authenticationDatabase')
        $arguments.Add($AuthDatabaseValue)
    }

    return $arguments.ToArray()
}

function Get-MatchingDatabases {
    param(
        [Parameter(Mandatory = $true)]
        [string]$MongoShellPath,
        [Parameter(Mandatory = $true)]
        [string]$ConnectionStringValue,
        [Parameter(Mandatory = $true)]
        [string]$DatabasePrefix,
        [string]$UsernameValue,
        [string]$PasswordValue,
        [string]$AuthDatabaseValue
    )

    $js = @"
const prefix = '$DatabasePrefix';
const result = db.adminCommand({ listDatabases: 1, nameOnly: true });
const names = (result.databases || []).map(d => d.name).filter(n => n.startsWith(prefix));
print(names.join('\n'));
"@

    $arguments = Get-MongoArguments -ConnectionStringValue $ConnectionStringValue -UsernameValue $UsernameValue -PasswordValue $PasswordValue -AuthDatabaseValue $AuthDatabaseValue
    $arguments += @('--quiet', '--eval', $js)

    $output = & $MongoShellPath @arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to list databases from MongoDB: $output"
    }

    return @($output | Where-Object { $_ -and $_ -notmatch '^\s*$' })
}

function Remove-MatchingDatabases {
    param(
        [Parameter(Mandatory = $true)]
        [string]$MongoShellPath,
        [Parameter(Mandatory = $true)]
        [string]$ConnectionStringValue,
        [Parameter(Mandatory = $true)]
        [string[]]$DatabaseNames,
        [string]$UsernameValue,
        [string]$PasswordValue,
        [string]$AuthDatabaseValue
    )

    foreach ($databaseName in $DatabaseNames) {
        $js = @"
const databaseName = '$databaseName';
try {
  db.getSiblingDB(databaseName).dropDatabase();
  print('DROPPED ' + databaseName);
} catch (error) {
  print('FAILED ' + databaseName + ': ' + error.message);
  throw error;
}
"@

        $arguments = Get-MongoArguments -ConnectionStringValue $ConnectionStringValue -UsernameValue $UsernameValue -PasswordValue $PasswordValue -AuthDatabaseValue $AuthDatabaseValue
        $arguments += @('--quiet', '--eval', $js)

        $output = & $MongoShellPath @arguments 2>&1
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to drop database '$databaseName': $output"
        }

        $output | ForEach-Object { Write-Host $_ }
    }
}

if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    $ConnectionString = 'mongodb://127.0.0.1:27017'
}

$mongoShellPath = Resolve-MongoShell
$matchingDatabases = Get-MatchingDatabases -MongoShellPath $mongoShellPath -ConnectionStringValue $ConnectionString -DatabasePrefix $Prefix -UsernameValue $Username -PasswordValue $Password -AuthDatabaseValue $AuthenticationDatabase

if ($matchingDatabases.Count -eq 0) {
    Write-Host "No databases matching prefix '$Prefix' were found." -ForegroundColor Green
    return
}

Write-Host "Matching databases:" -ForegroundColor Cyan
$matchingDatabases | ForEach-Object { Write-Host " - $_" }

if ($ListOnly) {
    Write-Host "Listing only; no databases were dropped." -ForegroundColor Yellow
    return
}

Write-Host "Dropping matching databases..." -ForegroundColor Yellow
Remove-MatchingDatabases -MongoShellPath $mongoShellPath -ConnectionStringValue $ConnectionString -DatabaseNames $matchingDatabases -UsernameValue $Username -PasswordValue $Password -AuthDatabaseValue $AuthenticationDatabase

Write-Host "Completed cleanup of databases matching prefix '$Prefix'." -ForegroundColor Green
