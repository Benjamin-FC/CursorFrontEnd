# CRM Client Data Fetcher

A C# ASP.NET Core Web API backend with a React/Vite frontend that connects to an external CRM server to fetch client data.

## Project Structure

```
CrmClientApp/
├── Controllers/          # API controllers
│   └── CrmController.cs  # Handles client data requests
├── Services/             # Business logic services
│   ├── ICrmService.cs    # CRM service interface
│   ├── CrmService.cs     # Implementation for calling external CRM server
│   ├── ITokenService.cs  # Token service interface
│   └── TokenService.cs   # Token generation service
├── ClientApp/            # React/Vite frontend
│   ├── src/
│   │   ├── App.jsx       # Main React component with form
│   │   └── App.css       # Styling
│   └── vite.config.js    # Vite configuration with proxy
└── Program.cs            # Application entry point
```

## Prerequisites

- .NET 8.0 SDK
- Node.js (v16 or higher)
- npm or yarn

## Setup Instructions

### 1. Restore .NET Dependencies

```bash
cd CrmClientApp
dotnet restore
```

### 2. Install Frontend Dependencies

```bash
cd ClientApp
npm install
```

### 3. Configure Environment Variables

The external API requires authentication using dynamically generated tokens. Set the following environment variables:

```bash
export CRM_USER_ID="your-user-id"
export CRM_PASSWORD="your-password"
```

**Windows (PowerShell):**
```powershell
$env:CRM_USER_ID="your-user-id"
$env:CRM_PASSWORD="your-password"
```

**Windows (Command Prompt):**
```cmd
set CRM_USER_ID=your-user-id
set CRM_PASSWORD=your-password
```

### 4. Configure CRM Server

The backend is configured to connect to `https://www.crmserver.com/`. The service calls the endpoint:
- `GET /api/GetClientData?id={clientId}`

If your CRM server uses a different endpoint structure, update the `CrmService.cs` file accordingly.

## Running the Application

### Development Mode

You can run the frontend and backend separately:

**Terminal 1 - Backend:**
```bash
cd CrmClientApp
dotnet run
```
The backend will run on `http://localhost:5000` and `https://localhost:5001`

**Terminal 2 - Frontend:**
```bash
cd CrmClientApp/ClientApp
npm run dev
```
The frontend will run on `http://localhost:5173`

The Vite dev server is configured to proxy `/api` requests to the backend.

### Production Build

1. Build the React app:
```bash
cd CrmClientApp/ClientApp
npm run build
```

2. Run the .NET application:
```bash
cd CrmClientApp
dotnet run
```

The built React app will be served from the `wwwroot` directory.

## API Endpoints

### GET /api/Crm/GetClientData

Fetches client data from the external CRM server.

**Query Parameters:**
- `id` (required): The client ID to fetch data for

**Response:**
```json
{
  "data": "..."
}
```

**Error Response:**
```json
{
  "error": "Error message",
  "message": "Detailed error message"
}
```

## Frontend

The React frontend provides a simple form to:
1. Enter a client ID
2. Submit to fetch client data from the CRM server
3. Display the returned data or error messages

## Configuration

### Backend Configuration

#### Environment Variables (Required)
- `CRM_USER_ID`: User ID for token generation (required)
- `CRM_PASSWORD`: Password for token generation (required)

#### appsettings.json Configuration

The `appsettings.json` file contains the following configuration:

```json
{
  "ExternalApi": {
    "CrmServer": {
      "BaseUrl": "https://www.crmserver.com/",
      "TimeoutSeconds": 30
    },
    "Token": {
      "Algorithm": "SHA256",
      "Secret": "your-token-secret-key-here",
      "ExpiryMinutes": 60,
      "IncludeTimestamp": true,
      "HeaderName": "Authorization",
      "HeaderFormat": "Bearer {0}"
    }
  }
}
```

**Token Configuration Options:**
- `Algorithm`: Token hashing algorithm (SHA256, SHA512, or MD5). Default: SHA256
- `Secret`: Secret key used in token generation (required)
- `ExpiryMinutes`: Token expiry time in minutes. Default: 60
- `IncludeTimestamp`: Whether to include timestamp in token format. Default: true
- `HeaderName`: HTTP header name for the token. Default: Authorization
- `HeaderFormat`: Format string for the token header (use {0} as token placeholder). Default: "Bearer {0}"

#### Other Configuration
- CRM server URL: Configured in `appsettings.json` under `ExternalApi:CrmServer:BaseUrl`
- CORS: Configured to allow requests from `http://localhost:5173`
- Timeout: Configurable in `appsettings.json` under `ExternalApi:CrmServer:TimeoutSeconds` (default: 30 seconds)

### Frontend Configuration

- API proxy: Configured in `vite.config.js` to proxy `/api` requests to `http://localhost:5000`

## Authentication

The application uses dynamic token-based authentication for the external CRM API:

1. **Token Generation**: Tokens are generated dynamically using:
   - User ID and Password from environment variables (`CRM_USER_ID`, `CRM_PASSWORD`)
   - Secret key from configuration (`ExternalApi:Token:Secret`)
   - Configurable hashing algorithm (SHA256, SHA512, or MD5)
   - Optional timestamp for expiry

2. **Token Format**: The token is generated by hashing a payload containing userid, password, secret, and timestamp. The format can be customized via configuration.

3. **HTTP Headers**: The generated token is automatically added to each API request using the configured header name and format (default: `Authorization: Bearer {token}`).

## Notes

- The CRM server endpoint structure may need to be adjusted based on the actual API structure of `www.crmserver.com`
- SSL certificate validation: If the CRM server uses a self-signed certificate, you may need to configure certificate validation in `CrmService.cs`
- **Security**: Never commit environment variables or the token secret to version control. Use secure configuration management in production environments.
