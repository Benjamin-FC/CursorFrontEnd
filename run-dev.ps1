#!/usr/bin/env pwsh
# Development run script for CRM WebSPA Application
# This script runs both the .NET backend and React frontend in development mode

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
$requiredVars = @("CRM_BASEURL", "CRM_TOKEN_URL", "CRM_CLIENT_ID", "CRM_CLIENT_SECRET", "CRM_SCOPE", "CRM_USERNAME", "CRM_PASSWORD")
$missingVars = @()

foreach ($var in $requiredVars) {
    if ([string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($var))) {
        $missingVars += $var
    }
}

if ($missingVars.Count -gt 0) {
    Write-Host "`nError: The following required environment variables are not set:" -ForegroundColor Red
    foreach ($var in $missingVars) {
        Write-Host "  - $var" -ForegroundColor Red
    }
    Write-Host "`nThe application cannot start without these variables." -ForegroundColor Yellow
    Write-Host "Please set the environment variables and try again." -ForegroundColor Yellow
    exit 1
}

# Function to kill processes on a specific port
function Stop-ProcessOnPort {
    param([int]$Port)
    
    $connections = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
    if ($connections) {
        Write-Host "Found process(es) using port $Port, stopping them..." -ForegroundColor Yellow
        foreach ($conn in $connections) {
            $process = Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue
            if ($process) {
                Write-Host "  Stopping process: $($process.ProcessName) (PID: $($process.Id))" -ForegroundColor Gray
                Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
            }
        }
        Start-Sleep -Seconds 1
    }
}

# Check and kill processes on ports 5000 and 5173
Write-Host "`nChecking ports..." -ForegroundColor Yellow
Stop-ProcessOnPort -Port 5000
Stop-ProcessOnPort -Port 5173

Write-Host "`nStarting both frontend and backend..." -ForegroundColor Yellow
Write-Host "Frontend: http://localhost:5173" -ForegroundColor Cyan
Write-Host "Backend:  http://localhost:5000" -ForegroundColor Cyan
Write-Host "Swagger:  http://localhost:5000/swagger" -ForegroundColor Cyan
Write-Host "`nPress Ctrl+C to stop both processes`n" -ForegroundColor Gray

# Start backend in a new PowerShell window
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

# Wait a bit for backend to start
Start-Sleep -Seconds 3

# Start frontend in foreground
Write-Host "Starting frontend..." -ForegroundColor Yellow
Push-Location $clientAppPath
try {
    npm run dev
}
finally {
    Pop-Location
}
