<#
.SYNOPSIS
    Runs the PriceHunt web app on http://localhost:4200.

.DESCRIPTION
    Installs the npm dependencies when they are missing or older than package-lock.json, then
    starts the Angular dev server, which forwards /api to the API on http://localhost:5080.
    Start the API first: .\pricehunt-backend\run.ps1
    Works on Windows PowerShell 5.1 and PowerShell 7+, from any current directory.

.EXAMPLE
    .\pricehunt-frontend\run.ps1

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File .\pricehunt-frontend\run.ps1
#>
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'

$requiredNode = '^22.22.3 || ^24.15.0 || >=26.0.0'
$appUrl = 'http://localhost:4200'
$healthUrl = 'http://localhost:5080/health'
$onWindows = $env:OS -eq 'Windows_NT'
$npm = if ($onWindows) { 'npm.cmd' } else { 'npm' }

function Test-NodeVersion([version]$version) {
    if ($version.Major -eq 22) { return $version -ge [version]'22.22.3' }
    if ($version.Major -eq 24) { return $version -ge [version]'24.15.0' }
    return $version.Major -ge 26
}

if (-not (Get-Command 'node' -ErrorAction SilentlyContinue)) {
    Write-Host "Node.js $requiredNode is required, but 'node' was not found on PATH." -ForegroundColor Red
    Write-Host 'Install it from https://nodejs.org (LTS) and run this script again.' -ForegroundColor Red
    exit 1
}

$nodeVersionText = (& node --version).TrimStart('v')
$nodeVersion = $null
if (-not [version]::TryParse(($nodeVersionText -replace '-.*$', ''), [ref]$nodeVersion) -or -not (Test-NodeVersion $nodeVersion)) {
    Write-Host "Node.js $requiredNode is required; found $nodeVersionText." -ForegroundColor Red
    Write-Host 'Install a supported version from https://nodejs.org (LTS) and run this script again.' -ForegroundColor Red
    exit 1
}

if (-not (Get-Command $npm -ErrorAction SilentlyContinue)) {
    Write-Host "npm was not found on PATH; it ships with Node.js $requiredNode." -ForegroundColor Red
    exit 1
}

Push-Location $PSScriptRoot
try {
    # npm writes node_modules/.package-lock.json on every install: older than the lock file means stale.
    $lockFile = Join-Path $PSScriptRoot 'package-lock.json'
    $installMarker = Join-Path $PSScriptRoot 'node_modules/.package-lock.json'
    $needsInstall = -not (Test-Path -LiteralPath $installMarker)
    if (-not $needsInstall) {
        $needsInstall = (Get-Item -LiteralPath $installMarker).LastWriteTimeUtc -lt (Get-Item -LiteralPath $lockFile).LastWriteTimeUtc
    }

    if ($needsInstall) {
        Write-Host 'Installing dependencies (npm ci)...'
        & $npm ci --no-audit --no-fund
        if ($LASTEXITCODE -ne 0) {
            Write-Host "npm ci failed with exit code $LASTEXITCODE." -ForegroundColor Red
            exit $LASTEXITCODE
        }
    }

    try {
        Invoke-WebRequest -UseBasicParsing -Uri $healthUrl -TimeoutSec 2 | Out-Null
    }
    catch {
        Write-Warning "The API is not answering at $healthUrl yet. Start it with .\pricehunt-backend\run.ps1; the app shows an error until it is up."
    }

    # No interactive prompts from the Angular CLI (autocompletion setup, analytics).
    $env:NG_FORCE_AUTOCOMPLETE = 'false'
    $env:NG_CLI_ANALYTICS = 'false'

    Write-Host ''
    Write-Host 'PriceHunt web app'
    Write-Host "  Open:  $appUrl"
    Write-Host '  /api is forwarded to http://localhost:5080'
    Write-Host '  Press Ctrl+C to stop.'
    Write-Host ''

    & $npm start
    $exitCode = $LASTEXITCODE
}
finally {
    Pop-Location
}

exit $exitCode
