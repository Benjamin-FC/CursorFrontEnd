#!/usr/bin/env pwsh
# Script to set up development environment variables for CRM WebSPA Application

Write-Host "Setting up development environment variables..." -ForegroundColor Cyan

# Set mock/test CRM environment variables at Process level
[Environment]::SetEnvironmentVariable("CRM_BASEURL", "https://mock-crm-server.example.com", "Process")
[Environment]::SetEnvironmentVariable("CRM_TOKEN_URL", "https://mock-token-server.example.com/oauth/token", "Process")
[Environment]::SetEnvironmentVariable("CRM_CLIENT_ID", "dev-client-id", "Process")
[Environment]::SetEnvironmentVariable("CRM_CLIENT_SECRET", "dev-client-secret", "Process")
[Environment]::SetEnvironmentVariable("CRM_SCOPE", "read write", "Process")
[Environment]::SetEnvironmentVariable("CRM_USERNAME", "dev-user", "Process")
[Environment]::SetEnvironmentVariable("CRM_PASSWORD", "dev-password", "Process")

# Also set in current session
$env:CRM_BASEURL = "https://mock-crm-server.example.com"
$env:CRM_TOKEN_URL = "https://mock-token-server.example.com/oauth/token"
$env:CRM_CLIENT_ID = "dev-client-id"
$env:CRM_CLIENT_SECRET = "dev-client-secret"
$env:CRM_SCOPE = "read write"
$env:CRM_USERNAME = "dev-user"
$env:CRM_PASSWORD = "dev-password"

Write-Host "✓ Environment variables set:" -ForegroundColor Green
Write-Host "  CRM_BASEURL       = $env:CRM_BASEURL" -ForegroundColor Gray
Write-Host "  CRM_TOKEN_URL     = $env:CRM_TOKEN_URL" -ForegroundColor Gray
Write-Host "  CRM_CLIENT_ID     = $env:CRM_CLIENT_ID" -ForegroundColor Gray
Write-Host "  CRM_CLIENT_SECRET = $env:CRM_CLIENT_SECRET" -ForegroundColor Gray
Write-Host "  CRM_SCOPE         = $env:CRM_SCOPE" -ForegroundColor Gray
Write-Host "  CRM_USERNAME      = $env:CRM_USERNAME" -ForegroundColor Gray
Write-Host "  CRM_PASSWORD      = ****" -ForegroundColor Gray

Write-Host "`nNote: These are mock values for development only." -ForegroundColor Yellow
Write-Host "For production or real API testing, set actual values." -ForegroundColor Yellow
Write-Host "`nYou can now run: .\run-dev.ps1" -ForegroundColor Cyan
