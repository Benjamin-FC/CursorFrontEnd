#!/usr/bin/env pwsh
# Run script for CRM WebSPA Application

param(
    [Parameter(Mandatory=$false)]
    [switch]$Development,
    
    [Parameter(Mandatory=$false)]
    [switch]$NoBuild,
    
    [Parameter(Mandatory=$false)]
    [string]$Port = "5000"
)

$ErrorActionPreference = "Stop"

$environment = if ($Development) { "Development" } else { "Production" }

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Running CRM WebSPA Application" -ForegroundColor Cyan
Write-Host "Environment: $environment" -ForegroundColor Cyan
Write-Host "Port: $Port" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Get the script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = $scriptPath
$projectPath = Join-Path $projectRoot "CrmClientApp\CrmClientApp.csproj"

# Build if not skipped
if (-not $NoBuild) {
    Write-Host "`nBuilding application..." -ForegroundColor Yellow
    $buildConfig = if ($Development) { "Debug" } else { "Release" }
    & "$scriptPath\build.ps1" -Configuration $buildConfig
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Build failed. Exiting." -ForegroundColor Red
        exit 1
    }
}

# Check for required environment variables
Write-Host "`nChecking environment variables..." -ForegroundColor Yellow
$requiredVars = @("OAUTH_CLIENT_ID", "OAUTH_CLIENT_SECRET")
$missingVars = @()

foreach ($var in $requiredVars) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($var))) {
        $missingVars += $var
    }
}

if ($missingVars.Count -gt 0) {
    Write-Host "`nWarning: The following environment variables are not set:" -ForegroundColor Red
    foreach ($var in $missingVars) {
        Write-Host "  - $var" -ForegroundColor Red
    }
    Write-Host "`nThe application may fail to start without these variables." -ForegroundColor Yellow
    Write-Host "Set them in your environment or create a .env file." -ForegroundColor Yellow
    
    $response = Read-Host "`nDo you want to continue anyway? (y/N)"
    if ($response -ne 'y' -and $response -ne 'Y') {
        Write-Host "Exiting." -ForegroundColor Gray
        exit 1
    }
}

# Run the application
Write-Host "`nStarting application..." -ForegroundColor Yellow
Write-Host "Press Ctrl+C to stop the server`n" -ForegroundColor Gray

$env:ASPNETCORE_ENVIRONMENT = $environment
$env:ASPNETCORE_URLS = "http://localhost:$Port"

Push-Location $projectRoot
try {
    dotnet run --project $projectPath --no-build
}
finally {
    Pop-Location
}
