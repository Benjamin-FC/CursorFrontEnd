# CRM Client Data Fetcher

A C# ASP.NET Core Web API backend with a React/Vite frontend that connects to an external CRM server to fetch client data.

## Project Structure

```
CrmClientApp/
├── Controllers/          # API controllers
│   └── CrmController.cs  # Handles client data requests
├── Services/             # Business logic services
│   ├── ICrmService.cs    # CRM service interface
│   └── CrmService.cs     # Implementation for calling external CRM server
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

### 3. Configure CRM Server

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

- CRM server URL: Configured in `Program.cs` (currently `https://www.crmserver.com/`)
- CORS: Configured to allow requests from `http://localhost:5173`
- Timeout: 30 seconds for CRM server requests

### Frontend Configuration

- API proxy: Configured in `vite.config.js` to proxy `/api` requests to `http://localhost:5000`

## Notes

- The CRM server endpoint structure may need to be adjusted based on the actual API structure of `www.crmserver.com`
- SSL certificate validation: If the CRM server uses a self-signed certificate, you may need to configure certificate validation in `CrmService.cs`
- Authentication: If the CRM server requires authentication, add the necessary headers or tokens in `CrmService.cs`
