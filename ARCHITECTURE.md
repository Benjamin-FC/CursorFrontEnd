# Architecture Documentation

## Overview

The CRM Client Data Fetcher is a full-stack web application consisting of:
- **Backend**: ASP.NET Core 8.0 Web API (C#)
- **Frontend**: React 18 with Vite
- **Authentication**: OAuth 2.0 Client Credentials Flow

## System Architecture

```
┌─────────────────┐
│   React Client  │
│   (Port 5173)   │
└────────┬────────┘
         │ HTTP/HTTPS
         │ /api/*
         ▼
┌─────────────────┐
│  ASP.NET Core   │
│  Web API        │
│  (Port 5000)    │
└────────┬────────┘
         │
         ├─────────────────┐
         │                 │
         ▼                 ▼
┌─────────────────┐  ┌─────────────────┐
│  Token Service  │  │   CRM Service   │
│  (OAuth)        │  │                 │
└────────┬────────┘  └────────┬────────┘
         │                    │
         │                    │
         ▼                    ▼
┌─────────────────┐  ┌─────────────────┐
│  Token Server   │  │   CRM Server    │
│  (OAuth)        │  │  (External API) │
└─────────────────┘  └─────────────────┘
```

## Component Overview

### Backend Components

#### 1. **CrmController** (`Controllers/CrmController.cs`)
- **Purpose**: REST API endpoint for client data retrieval
- **Responsibilities**:
  - Accept HTTP GET requests with client ID parameter
  - Validate input parameters
  - Invoke CRM service to fetch data
  - Handle errors and return appropriate HTTP status codes
- **Route**: `/api/Crm/GetClientData`

#### 2. **CrmService** (`Services/CrmService.cs`)
- **Purpose**: Business logic for interacting with external CRM server
- **Responsibilities**:
  - Retrieve OAuth token from TokenService
  - Construct HTTP requests to CRM server
  - Apply OAuth token to request headers
  - Handle HTTP responses and errors
- **Dependencies**: `ITokenService`, `HttpClient`, `IConfiguration`

#### 3. **TokenService** (`Services/TokenService.cs`)
- **Purpose**: OAuth 2.0 token management
- **Responsibilities**:
  - Retrieve OAuth tokens from token server
  - Cache tokens in memory
  - Automatically refresh tokens before expiration
  - Thread-safe token retrieval (prevents concurrent requests)
- **Features**:
  - Token caching with 5-minute buffer before expiration
  - Semaphore-based locking for thread safety
  - Support for Basic Authentication and form-encoded requests
  - Configurable token header format

#### 4. **Program.cs**
- **Purpose**: Application startup and dependency injection configuration
- **Responsibilities**:
  - Configure services and middleware
  - Set up HTTP clients with base URLs and timeouts
  - Configure CORS for React frontend
  - Validate required configuration and environment variables

### Frontend Components

#### 1. **App.jsx**
- **Purpose**: Main React component with user interface
- **Features**:
  - Form for entering client ID
  - HTTP request to backend API
  - Display of client data or error messages
  - Loading states during API calls

#### 2. **Vite Configuration** (`vite.config.js`)
- **Purpose**: Development server and build configuration
- **Features**:
  - Proxy configuration for API requests
  - Development server on port 5173
  - Production build output to `wwwroot`

## Data Flow

### Client Data Retrieval Flow

1. **User Input**: User enters client ID in React form
2. **Frontend Request**: React app sends GET request to `/api/Crm/GetClientData?id={clientId}`
3. **Controller Validation**: `CrmController` validates the client ID parameter
4. **Token Retrieval**: `CrmService` requests OAuth token from `TokenService`
5. **Token Service Logic**:
   - Check if cached token exists and is valid
   - If not, acquire semaphore lock
   - Double-check cache (another thread may have refreshed it)
   - If still invalid, fetch new token from token server
   - Cache token with expiration time
6. **CRM Request**: `CrmService` constructs HTTP request with OAuth token in headers
7. **External API Call**: Request sent to external CRM server
8. **Response Handling**: Response parsed and returned to controller
9. **API Response**: Controller returns JSON response to frontend
10. **UI Update**: React app displays data or error message

## Authentication Flow

### OAuth 2.0 Client Credentials Flow

```
┌─────────────┐                    ┌─────────────┐
│   App       │                    │   Token     │
│             │                    │   Server    │
└──────┬──────┘                    └──────┬──────┘
       │                                   │
       │ 1. POST /oauth/token              │
       │    grant_type=client_credentials  │
       │    client_id=...                  │
       │    client_secret=...              │
       │──────────────────────────────────>│
       │                                   │
       │ 2. Response:                     │
       │    {                              │
       │      "access_token": "...",       │
       │      "expires_in": 3600           │
       │    }                              │
       │<──────────────────────────────────│
       │                                   │
       │ 3. Cache token                    │
       │    (expires_at = now + 3540s)     │
       │                                   │
       └───────────────────────────────────┘
```

## Configuration Management

### Environment Variables
- **OAUTH_CLIENT_ID**: OAuth client identifier (required)
- **OAUTH_CLIENT_SECRET**: OAuth client secret (required)

### appsettings.json Structure
```json
{
  "ExternalApi": {
    "CrmServer": {
      "BaseUrl": "https://www.crmserver.com/",
      "TimeoutSeconds": 30
    },
    "Token": {
      "Endpoint": "https://www.tokenserver.com/oauth/token",
      "GrantType": "client_credentials",
      "Scope": "",
      "UseBasicAuth": false,
      "HeaderName": "Authorization",
      "HeaderFormat": "Bearer {0}"
    }
  }
}
```

## Security Considerations

1. **Environment Variables**: OAuth credentials stored in environment variables, never in code or configuration files
2. **Token Caching**: Tokens cached in memory only (not persisted to disk)
3. **HTTPS**: All external API calls use HTTPS
4. **CORS**: Configured to allow only specific origins (localhost:5173 in development)
5. **Input Validation**: Client IDs validated before processing
6. **Error Handling**: Errors logged without exposing sensitive information

## Thread Safety

The `TokenService` implements thread-safe token retrieval using:
- **SemaphoreSlim**: Ensures only one token request at a time
- **Double-Check Locking**: Prevents race conditions when checking cache
- **Immutable Cache**: Token cache updated atomically

## Error Handling Strategy

### Backend Error Handling
- **Validation Errors**: Return 400 Bad Request
- **CRM Server Errors**: Return 503 Service Unavailable
- **Unexpected Errors**: Return 500 Internal Server Error
- **All Errors**: Logged with appropriate log levels

### Frontend Error Handling
- Display user-friendly error messages
- Show loading states during API calls
- Handle network errors gracefully

## Testing Architecture

### Unit Tests
- **TokenServiceTests**: Mock HTTP client, test token caching and refresh logic
- **CrmServiceTests**: Mock token service and HTTP client, test request construction
- **CrmControllerTests**: Mock CRM service, test controller logic

### Integration Tests
- **LiveApiIntegrationTests**: Real HTTP requests to external servers
- Requires environment variables for OAuth credentials
- Tests end-to-end workflow

## Deployment Architecture

### Development
- Backend and frontend run separately
- Vite dev server proxies API requests
- Hot module replacement for frontend

### Production
- Frontend built and served from `wwwroot`
- Single ASP.NET Core application
- Static file middleware serves React app
- Fallback routing for SPA

## Scalability Considerations

1. **Stateless Design**: No server-side session state
2. **Token Caching**: Reduces token server load
3. **HTTP Client Reuse**: HttpClient instances reused via HttpClientFactory
4. **Async/Await**: Non-blocking I/O operations
5. **Configuration**: Timeout and retry policies configurable

## Future Enhancements

Potential improvements:
- Token refresh retry logic
- Circuit breaker pattern for external API calls
- Distributed token caching (Redis)
- Request/response logging middleware
- API rate limiting
- Health check endpoints
- OpenAPI/Swagger documentation generation
