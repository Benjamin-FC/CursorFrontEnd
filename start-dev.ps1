#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Start development servers for backend and frontend
.DESCRIPTION
    Starts both .NET backend and React frontend dev servers
    Requires CRM_* environment variables to be set before running
#>

# Validate environment variables
$requiredVars = @(
    "CRM_BASEURL",
    "CRM_TOKEN_URL",
    "CRM_CLIENT_ID",
    "CRM_CLIENT_SECRET",
    "CRM_SCOPE",
    "CRM_USERNAME",
    "CRM_PASSWORD"
)

$missingVars = @()
foreach ($var in $requiredVars) {
    if (-not (Test-Path "env:$var")) {
        $missingVars += $var
    }
}

if ($missingVars.Count -gt 0) {
    Write-Host "Error: The following required environment variables are not set:" -ForegroundColor Red
    $missingVars | ForEach-Object { Write-Host "  - $_" -ForegroundColor Red }
    Write-Host ""
    Write-Host "Set them before running this script, or use set-dev-env.ps1 for mock values:" -ForegroundColor Yellow
    Write-Host "  . .\set-dev-env.ps1" -ForegroundColor Cyan
    exit 1
}

# Get paths
$backendPath = Join-Path $PSScriptRoot "CrmClientApp"
$frontendPath = Join-Path $backendPath "ClientApp"

# Check if paths exist
if (-not (Test-Path $backendPath)) {
    Write-Host "Error: Backend directory not found at: $backendPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $frontendPath)) {
    Write-Host "Error: Frontend directory not found at: $frontendPath" -ForegroundColor Red
    exit 1
}

# Check for port conflicts
$backendPort = 5000
$frontendPort = 5173

function Test-PortInUse {
    param([int]$Port)
    $connections = Get-NetTCPConnection -State Listen -ErrorAction SilentlyContinue | Where-Object { $_.LocalPort -eq $Port }
    return $connections.Count -gt 0
}

if (Test-PortInUse -Port $backendPort) {
    Write-Host "Warning: Port $backendPort is already in use. Backend may fail to start." -ForegroundColor Yellow
}

if (Test-PortInUse -Port $frontendPort) {
    Write-Host "Warning: Port $frontendPort is already in use. Frontend may fail to start." -ForegroundColor Yellow
}

Write-Host "Starting backend in new window..." -ForegroundColor Yellow
$backendScript = @"
`$env:ASPNETCORE_ENVIRONMENT = 'Development'
`$env:ASPNETCORE_URLS = 'http://localhost:5000'
`$env:CRM_BASEURL = '$env:CRM_BASEURL'
`$env:CRM_TOKEN_URL = '$env:CRM_TOKEN_URL'
`$env:CRM_CLIENT_ID = '$env:CRM_CLIENT_ID'
`$env:CRM_CLIENT_SECRET = '$env:CRM_CLIENT_SECRET'
`$env:CRM_SCOPE = '$env:CRM_SCOPE'
`$env:CRM_USERNAME = '$env:CRM_USERNAME'
`$env:CRM_PASSWORD = '$env:CRM_PASSWORD'
Set-Location '$backendPath'
Write-Host 'Backend starting...' -ForegroundColor Green
dotnet run
"@

Start-Process pwsh -ArgumentList "-NoExit", "-Command", $backendScript

# Give backend time to start
Write-Host "Waiting for backend to initialize..." -ForegroundColor Yellow
Start-Sleep -Seconds 3

# Start frontend in current window
Write-Host "Starting frontend..." -ForegroundColor Yellow
Set-Location $frontendPath
npm run dev
