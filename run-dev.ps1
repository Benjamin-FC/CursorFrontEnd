#!/usr/bin/env pwsh
# Development run script for CRM WebSPA Application
# This script runs both the .NET backend and React frontend in development mode

param(
    [Parameter(Mandatory=$false)]
    [switch]$BackendOnly,
    
    [Parameter(Mandatory=$false)]
    [switch]$FrontendOnly
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Running CRM WebSPA in Development Mode" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Get the script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = $scriptPath
$clientAppPath = Join-Path $projectRoot "CrmClientApp\ClientApp"
$backendPath = Join-Path $projectRoot "CrmClientApp"

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
}

if ($FrontendOnly) {
    # Run only frontend
    Write-Host "`nStarting React development server..." -ForegroundColor Yellow
    Write-Host "Frontend will be available at: http://localhost:5173" -ForegroundColor Cyan
    Write-Host "Press Ctrl+C to stop`n" -ForegroundColor Gray
    
    Push-Location $clientAppPath
    try {
        npm run dev
    }
    finally {
        Pop-Location
    }
}
elseif ($BackendOnly) {
    # Run only backend
    Write-Host "`nStarting .NET backend..." -ForegroundColor Yellow
    Write-Host "Backend API will be available at: http://localhost:5000" -ForegroundColor Cyan
    Write-Host "Swagger UI: http://localhost:5000/swagger" -ForegroundColor Cyan
    Write-Host "Press Ctrl+C to stop`n" -ForegroundColor Gray
    
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = "http://localhost:5000"
    
    Push-Location $backendPath
    try {
        dotnet run
    }
    finally {
        Pop-Location
    }
}
else {
    # Run both
    Write-Host "`nThis will start both frontend and backend." -ForegroundColor Yellow
    Write-Host "Frontend: http://localhost:5173" -ForegroundColor Cyan
    Write-Host "Backend:  http://localhost:5000" -ForegroundColor Cyan
    Write-Host "Swagger:  http://localhost:5000/swagger" -ForegroundColor Cyan
    Write-Host "`nNote: You'll need to run these in separate terminal windows." -ForegroundColor Yellow
    Write-Host "`nOptions:" -ForegroundColor Gray
    Write-Host "  .\run-dev.ps1 -BackendOnly   # Run only backend" -ForegroundColor Gray
    Write-Host "  .\run-dev.ps1 -FrontendOnly  # Run only frontend" -ForegroundColor Gray
    Write-Host "`nStarting backend in this window..." -ForegroundColor Yellow
    Write-Host "Open another terminal and run: .\run-dev.ps1 -FrontendOnly" -ForegroundColor Yellow
    Write-Host "`nPress Ctrl+C to stop`n" -ForegroundColor Gray
    
    Start-Sleep -Seconds 2
    
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = "http://localhost:5000"
    
    Push-Location $backendPath
    try {
        dotnet run
    }
    finally {
        Pop-Location
    }
}
