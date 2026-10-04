<#
.SYNOPSIS
    Runs the PriceHunt API on http://localhost:5080.

.DESCRIPTION
    Restores, builds and starts the API with the "http" launch profile. The SQLite database
    and its schema are created automatically on first run.
    Works on Windows PowerShell 5.1 and PowerShell 7+, from any current directory.

.PARAMETER ResetDatabase
    Deletes the SQLite database the API uses (Database__Path, if set) before starting, so the API
    starts with an empty history.

.EXAMPLE
    .\pricehunt-backend\run.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\pricehunt-backend\run.ps1 -ResetDatabase
#>
[CmdletBinding()]
param(
    [switch]$ResetDatabase
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$requiredSdk = [version]'10.0.100'
$apiDirectory = Join-Path $PSScriptRoot 'src/PriceHunt.Api'
$projectPath = Join-Path $apiDirectory 'PriceHunt.Api.csproj'

if (-not (Get-Command 'dotnet' -ErrorAction SilentlyContinue)) {
    Write-Host "The .NET SDK $requiredSdk or later is required, but 'dotnet' was not found on PATH." -ForegroundColor Red
    Write-Host 'Install it from https://dotnet.microsoft.com/download and run this script again.' -ForegroundColor Red
    exit 1
}

$installedSdks = & dotnet --list-sdks
if ($LASTEXITCODE -ne 0) {
    Write-Host "'dotnet --list-sdks' failed with exit code $LASTEXITCODE." -ForegroundColor Red
    exit $LASTEXITCODE
}

$hasRequiredSdk = $false
foreach ($line in $installedSdks) {
    $versionText = ($line -split ' ')[0] -replace '-.*$', ''
    $version = $null
    if ([version]::TryParse($versionText, [ref]$version) -and $version -ge $requiredSdk) {
        $hasRequiredSdk = $true
    }
}

if (-not $hasRequiredSdk) {
    Write-Host "The .NET SDK $requiredSdk or later is required. Installed SDKs:" -ForegroundColor Red
    $installedSdks | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    Write-Host 'Install it from https://dotnet.microsoft.com/download and run this script again.' -ForegroundColor Red
    exit 1
}

if ($ResetDatabase) {
    # The file the API uses: Database__Path when set, else Database:Path in appsettings.json;
    # a relative path is relative to the API folder.
    $databasePath = $env:Database__Path
    if (-not $databasePath) {
        $settings = Get-Content -LiteralPath (Join-Path $apiDirectory 'appsettings.json') -Raw | ConvertFrom-Json
        $databasePath = $settings.Database.Path
    }
    if (-not [System.IO.Path]::IsPathRooted($databasePath)) {
        $databasePath = Join-Path $apiDirectory $databasePath
    }

    # The database file and its write-ahead-log files.
    $databaseFolder = Split-Path -Parent $databasePath
    $databaseFiles = @()
    if (Test-Path -LiteralPath $databaseFolder) {
        $databaseFiles = @(Get-ChildItem -LiteralPath $databaseFolder -Filter "$(Split-Path -Leaf $databasePath)*" -File)
    }

    if ($databaseFiles.Count -gt 0) {
        $databaseFiles | Remove-Item -Force
        Write-Host "Database deleted: $databasePath. It is recreated when the API starts."
    }
    else {
        Write-Host "No database to delete at $databasePath."
    }
}

Write-Host ''
Write-Host 'PriceHunt API'
Write-Host '  API:     http://localhost:5080'
Write-Host '  Health:  http://localhost:5080/health'
Write-Host '  OpenAPI: http://localhost:5080/openapi/v1.json'
Write-Host '  Start the frontend next: .\pricehunt-frontend\run.ps1, then open http://localhost:4200'
Write-Host '  Press Ctrl+C to stop.'
Write-Host ''

& dotnet run --project $projectPath --launch-profile http
exit $LASTEXITCODE
