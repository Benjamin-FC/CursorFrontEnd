#!/usr/bin/env pwsh
# Build script for CRM WebSPA Application

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building CRM WebSPA Application" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Get the script directory
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = $scriptPath
$clientAppPath = Join-Path $projectRoot "CrmClientApp\ClientApp"
$backendPath = Join-Path $projectRoot "CrmClientApp"

# Step 1: Install and build frontend
Write-Host "`n[1/3] Building React Frontend..." -ForegroundColor Yellow
Push-Location $clientAppPath
try {
    Write-Host "Installing npm packages..." -ForegroundColor Gray
    npm install
    if ($LASTEXITCODE -ne 0) {
        throw "npm install failed"
    }

    Write-Host "Building React application..." -ForegroundColor Gray
    npm run build
    if ($LASTEXITCODE -ne 0) {
        throw "npm build failed"
    }
    Write-Host "✓ Frontend build completed successfully" -ForegroundColor Green
}
finally {
    Pop-Location
}

# Step 2: Restore .NET dependencies
Write-Host "`n[2/3] Restoring .NET dependencies..." -ForegroundColor Yellow
Push-Location $projectRoot
try {
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet restore failed"
    }
    Write-Host "✓ Dependencies restored successfully" -ForegroundColor Green
}
finally {
    Pop-Location
}

# Step 3: Build .NET application
Write-Host "`n[3/3] Building .NET Backend..." -ForegroundColor Yellow
Push-Location $projectRoot
try {
    dotnet build --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed"
    }
    Write-Host "✓ Backend build completed successfully" -ForegroundColor Green
}
finally {
    Pop-Location
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Build completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "`nTo run the application, execute: .\run.ps1" -ForegroundColor Gray
