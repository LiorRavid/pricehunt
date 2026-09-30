<#
.SYNOPSIS
    Runs the PriceHunt API on http://localhost:5080.

.DESCRIPTION
    Restores, builds and starts the API with the "http" launch profile. The SQLite database
    and its schema are created automatically on first run.
    Works on Windows PowerShell 5.1 and PowerShell 7+, from any current directory.

.PARAMETER ResetDatabase
    Deletes the SQLite database before starting, so the API starts with an empty history.

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
$projectPath = Join-Path $PSScriptRoot 'src/PriceHunt.Api/PriceHunt.Api.csproj'
$databaseDirectory = Join-Path $PSScriptRoot 'src/PriceHunt.Api/App_Data'

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

if ($ResetDatabase -and (Test-Path -LiteralPath $databaseDirectory)) {
    Get-ChildItem -LiteralPath $databaseDirectory -Filter 'pricehunt.db*' | Remove-Item -Force
    Write-Host 'Database deleted; it is recreated when the API starts.'
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
