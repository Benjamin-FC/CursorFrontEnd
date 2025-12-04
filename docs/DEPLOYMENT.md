# Deployment Guide

This guide covers deploying the CRM Client Data Fetcher application to various environments.

## Prerequisites

- .NET 8.0 SDK installed on the deployment server
- Node.js 16+ and npm (for building the frontend)
- Access to the external CRM server and token server
- OAuth client credentials (client ID and secret)

## Pre-Deployment Checklist

- [ ] OAuth client ID and secret obtained
- [ ] External API endpoints verified and accessible
- [ ] Environment variables configured
- [ ] `appsettings.json` configured with correct server URLs
- [ ] CORS settings updated for production domain
- [ ] SSL certificates configured (for HTTPS)

## Build Process

### 1. Build Frontend

```bash
cd CrmClientApp/ClientApp
npm install
npm run build
```

This creates a production build in the `dist` directory, which will be copied to `wwwroot` during the backend build.

### 2. Build Backend

```bash
cd CrmClientApp
dotnet publish -c Release -o ./publish
```

The published application will be in the `publish` directory.

## Environment Configuration

### Environment Variables

Set the following environment variables on the deployment server:

**Linux/macOS**:
```bash
export OAUTH_CLIENT_ID="your-client-id"
export OAUTH_CLIENT_SECRET="your-client-secret"
```

**Windows (PowerShell)**:
```powershell
$env:OAUTH_CLIENT_ID="your-client-id"
$env:OAUTH_CLIENT_SECRET="your-client-secret"
```

**Windows (Command Prompt)**:
```cmd
set OAUTH_CLIENT_ID=your-client-id
set OAUTH_CLIENT_SECRET=your-client-secret
```

### appsettings.json

Update `appsettings.json` with production values:

```json
{
  "ExternalApi": {
    "CrmServer": {
      "BaseUrl": "https://production-crm-server.com/",
      "TimeoutSeconds": 30
    },
    "Token": {
      "Endpoint": "https://production-token-server.com/oauth/token",
      "GrantType": "client_credentials",
      "Scope": "read write",
      "UseBasicAuth": false,
      "HeaderName": "Authorization",
      "HeaderFormat": "Bearer {0}"
    }
  }
}
```

### CORS Configuration

Update CORS settings in `Program.cs` for production:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("https://your-production-domain.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```

## Deployment Options

### Option 1: Self-Hosted (Windows Service / Linux Systemd)

#### Windows Service

1. Install the application as a Windows Service using a tool like NSSM or sc.exe
2. Configure environment variables in the service configuration
3. Start the service

**Using NSSM**:
```bash
nssm install CrmClientApp "C:\path\to\CrmClientApp.exe"
nssm set CrmClientApp AppEnvironmentExtra OAUTH_CLIENT_ID=your-id OAUTH_CLIENT_SECRET=your-secret
nssm start CrmClientApp
```

#### Linux Systemd

Create a systemd service file `/etc/systemd/system/crmclientapp.service`:

```ini
[Unit]
Description=CRM Client Data Fetcher
After=network.target

[Service]
Type=notify
ExecStart=/usr/bin/dotnet /opt/crmclientapp/CrmClientApp.dll
Restart=always
RestartSec=10
Environment=OAUTH_CLIENT_ID=your-client-id
Environment=OAUTH_CLIENT_SECRET=your-client-secret
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000

[Install]
WantedBy=multi-user.target
```

Enable and start the service:
```bash
sudo systemctl enable crmclientapp
sudo systemctl start crmclientapp
sudo systemctl status crmclientapp
```

### Option 2: IIS (Windows)

1. Install .NET 8.0 Hosting Bundle on the IIS server
2. Create an application pool targeting .NET CLR Version "No Managed Code"
3. Create a new website or application in IIS
4. Point the physical path to the published application directory
5. Configure environment variables in `web.config` or via IIS Application Pool settings

**web.config** (optional, for additional configuration):
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified" />
    </handlers>
    <aspNetCore processPath="dotnet" 
                arguments=".\CrmClientApp.dll" 
                stdoutLogEnabled="false" 
                stdoutLogFile=".\logs\stdout" 
                hostingModel="inprocess">
      <environmentVariables>
        <environmentVariable name="OAUTH_CLIENT_ID" value="your-client-id" />
        <environmentVariable name="OAUTH_CLIENT_SECRET" value="your-client-secret" />
      </environmentVariables>
    </aspNetCore>
  </system.webServer>
</configuration>
```

### Option 3: Docker

#### Dockerfile

Create a `Dockerfile` in the project root:

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files
COPY CrmClientApp/CrmClientApp.csproj CrmClientApp/
COPY CrmClientApp.Tests/CrmClientApp.Tests.csproj CrmClientApp.Tests/
RUN dotnet restore CrmClientApp/CrmClientApp.csproj

# Copy source code
COPY . .

# Build frontend
WORKDIR /src/CrmClientApp/ClientApp
RUN npm install
RUN npm run build

# Build backend
WORKDIR /src/CrmClientApp
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "CrmClientApp.dll"]
```

#### Build and Run

```bash
# Build image
docker build -t crmclientapp:latest .

# Run container
docker run -d \
  -p 5000:80 \
  -e OAUTH_CLIENT_ID=your-client-id \
  -e OAUTH_CLIENT_SECRET=your-client-secret \
  --name crmclientapp \
  crmclientapp:latest
```

#### Docker Compose

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  crmclientapp:
    build: .
    ports:
      - "5000:80"
    environment:
      - OAUTH_CLIENT_ID=${OAUTH_CLIENT_ID}
      - OAUTH_CLIENT_SECRET=${OAUTH_CLIENT_SECRET}
      - ASPNETCORE_ENVIRONMENT=Production
    restart: unless-stopped
```

Run with:
```bash
docker-compose up -d
```

### Option 4: Azure App Service

1. Create an Azure App Service (Linux or Windows)
2. Configure Application Settings:
   - `OAUTH_CLIENT_ID`: Your OAuth client ID
   - `OAUTH_CLIENT_SECRET`: Your OAuth client secret
   - `ASPNETCORE_ENVIRONMENT`: Production
3. Deploy using:
   - Azure DevOps Pipelines
   - GitHub Actions
   - Visual Studio Publish
   - Azure CLI: `az webapp deployment source config-zip`

### Option 5: AWS Elastic Beanstalk

1. Create an Elastic Beanstalk application
2. Create an environment (choose .NET Core platform)
3. Upload the published application as a ZIP file
4. Configure environment properties:
   - `OAUTH_CLIENT_ID`
   - `OAUTH_CLIENT_SECRET`
5. Deploy

## Reverse Proxy Setup (Nginx)

Example Nginx configuration:

```nginx
server {
    listen 80;
    server_name your-domain.com;

    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

## SSL/TLS Configuration

### Using Let's Encrypt (Certbot)

```bash
sudo certbot --nginx -d your-domain.com
```

### Using IIS SSL Certificate

1. Import certificate in IIS
2. Bind certificate to the website
3. Configure HTTPS redirect

## Health Checks

Add a health check endpoint in `Program.cs`:

```csharp
builder.Services.AddHealthChecks();

// ...

app.MapHealthChecks("/health");
```

Monitor at: `https://your-domain.com/health`

## Logging

### Application Insights (Azure)

Add to `Program.cs`:
```csharp
builder.Services.AddApplicationInsightsTelemetry();
```

### Serilog (File Logging)

Install Serilog:
```bash
dotnet add package Serilog.AspNetCore
```

Configure in `Program.cs`:
```csharp
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();
```

## Monitoring and Maintenance

### Application Monitoring

- Set up application performance monitoring (APM)
- Configure alerting for errors and performance issues
- Monitor OAuth token refresh failures
- Track API response times

### Log Rotation

Configure log rotation to prevent disk space issues:
- Use rolling file appenders
- Set maximum log file size
- Configure retention policies

### Backup Strategy

- Backup `appsettings.json` (without secrets)
- Document environment variable values (securely)
- Version control configuration templates

## Troubleshooting

### Common Issues

1. **OAuth Token Errors**
   - Verify environment variables are set correctly
   - Check token server endpoint is accessible
   - Verify client credentials are valid

2. **CRM Server Connection Errors**
   - Verify CRM server URL is correct
   - Check network connectivity
   - Verify SSL certificates are valid

3. **CORS Errors**
   - Update CORS configuration for production domain
   - Verify frontend URL matches CORS allowed origins

4. **Static Files Not Serving**
   - Verify `wwwroot` directory exists and contains built frontend
   - Check `UseStaticFiles()` is called in `Program.cs`

## Security Best Practices

1. **Never commit secrets** to version control
2. **Use secure secret management** (Azure Key Vault, AWS Secrets Manager, etc.)
3. **Enable HTTPS** in production
4. **Keep dependencies updated** for security patches
5. **Implement rate limiting** if needed
6. **Use firewall rules** to restrict access
7. **Regular security audits** of dependencies

## Rollback Procedure

1. Keep previous deployment artifacts
2. Document deployment versions
3. Test rollback procedure in staging
4. Have rollback scripts ready

## Post-Deployment Verification

- [ ] Application starts without errors
- [ ] Health check endpoint responds
- [ ] OAuth token retrieval works
- [ ] CRM API calls succeed
- [ ] Frontend loads correctly
- [ ] CORS configured correctly
- [ ] SSL certificate valid
- [ ] Logging working
- [ ] Monitoring configured
