# Architecture Documentation

## Overview

The CRM Client Data Fetcher is a full-stack web application consisting of:
- **Backend**: ASP.NET Core 8.0 Web API (C#)
- **Frontend**: React 18 with Vite
- **Authentication**: OAuth 2.0 Client Credentials Flow

## System Architecture

```mermaid
graph TB
    subgraph Frontend["🌐 Frontend Layer"]
        React[React Client<br/>Port 5173<br/>⚛️ React + Vite]
    end
    
    subgraph Backend["⚙️ Backend Layer"]
        API[ASP.NET Core Web API<br/>Port 5000<br/>🔷 C# .NET 8.0]
        
        subgraph Services["🔧 Services"]
            TokenSvc[Token Service<br/>🔐 OAuth Management]
            CrmSvc[CRM Service<br/>📊 Data Retrieval]
        end
    end
    
    subgraph External["🌍 External Services"]
        TokenServer[Token Server<br/>🔑 OAuth Provider]
        CrmServer[CRM Server<br/>📈 External API]
    end
    
    React -->|HTTP/HTTPS<br/>/api/*| API
    API --> TokenSvc
    API --> CrmSvc
    TokenSvc -->|OAuth Token Request| TokenServer
    TokenServer -->|Access Token| TokenSvc
    CrmSvc -->|Authenticated Request<br/>Bearer Token| CrmServer
    CrmServer -->|Client Data| CrmSvc
    CrmSvc -->|JSON Response| API
    API -->|JSON Response| React
    
    classDef frontend fill:#61dafb,stroke:#20232a,stroke-width:2px,color:#000
    classDef backend fill:#512bd4,stroke:#fff,stroke-width:2px,color:#fff
    classDef service fill:#007acc,stroke:#fff,stroke-width:2px,color:#fff
    classDef external fill:#ff6b6b,stroke:#fff,stroke-width:2px,color:#fff
    
    class React frontend
    class API backend
    class TokenSvc,CrmSvc service
    class TokenServer,CrmServer external
```

## Component Overview

### Component Relationships

```mermaid
graph LR
    subgraph Frontend["🌐 Frontend"]
        AppJSX[App.jsx<br/>⚛️ React Component]
        ViteConfig[Vite Config<br/>⚡ Build Tool]
    end
    
    subgraph Backend["⚙️ Backend"]
        Program[Program.cs<br/>🚀 Startup]
        Controller[CrmController<br/>🎮 API Controller]
        
        subgraph Services["🔧 Services"]
            ICrmSvc[ICrmService<br/>📋 Interface]
            CrmSvc[CrmService<br/>📊 Implementation]
            ITokenSvc[ITokenService<br/>📋 Interface]
            TokenSvc[TokenService<br/>🔐 Implementation]
        end
        
        subgraph Config["⚙️ Configuration"]
            AppSettings[appsettings.json<br/>📝 Config]
            EnvVars[Environment Variables<br/>🔒 Secrets]
        end
    end
    
    AppJSX -->|HTTP Request| Controller
    ViteConfig -->|Proxy /api| Controller
    Program -->|DI Registration| Controller
    Program -->|DI Registration| Services
    Controller -->|Uses| ICrmSvc
    ICrmSvc -.->|Implemented by| CrmSvc
    CrmSvc -->|Uses| ITokenSvc
    ITokenSvc -.->|Implemented by| TokenSvc
    CrmSvc -->|Reads| AppSettings
    TokenSvc -->|Reads| AppSettings
    TokenSvc -->|Reads| EnvVars
    Program -->|Configures| Config
    
    classDef frontend fill:#61dafb,stroke:#20232a,stroke-width:2px,color:#000
    classDef backend fill:#512bd4,stroke:#fff,stroke-width:2px,color:#fff
    classDef service fill:#007acc,stroke:#fff,stroke-width:2px,color:#fff
    classDef interface fill:#4ec9b0,stroke:#fff,stroke-width:2px,color:#000
    classDef config fill:#ffa500,stroke:#fff,stroke-width:2px,color:#000
    
    class AppJSX,ViteConfig frontend
    class Program,Controller backend
    class CrmSvc,TokenSvc service
    class ICrmSvc,ITokenSvc interface
    class AppSettings,EnvVars config
```

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

```mermaid
sequenceDiagram
    participant User as 👤 User
    participant React as ⚛️ React Frontend<br/>(Port 5173)
    participant Controller as 🎮 CrmController<br/>(API Endpoint)
    participant CrmService as 📊 CrmService
    participant TokenService as 🔐 TokenService
    participant TokenServer as 🔑 Token Server
    participant CrmServer as 📈 CRM Server
    
    User->>React: Enter client ID
    React->>Controller: GET /api/Crm/GetClientData?id={clientId}
    activate Controller
    
    Controller->>Controller: Validate client ID
    alt Invalid client ID
        Controller-->>React: 400 Bad Request ❌
        deactivate Controller
        React-->>User: Display error message
    else Valid client ID
        Controller->>CrmService: GetClientDataAsync(clientId)
        activate CrmService
        
        CrmService->>TokenService: GetTokenAsync()
        activate TokenService
        
        TokenService->>TokenService: Check token cache
        alt Token cached and valid
            TokenService-->>CrmService: Return cached token ✅
        else Token expired or missing
            TokenService->>TokenService: Acquire semaphore lock 🔒
            TokenService->>TokenServer: POST /oauth/token<br/>(client credentials)
            activate TokenServer
            TokenServer-->>TokenService: Access token + expires_in
            deactivate TokenServer
            TokenService->>TokenService: Cache token<br/>(expires_at = now + 3540s)
            TokenService->>TokenService: Release semaphore lock 🔓
            TokenService-->>CrmService: Return token ✅
        end
        deactivate TokenService
        
        CrmService->>CrmService: Construct HTTP request<br/>with Bearer token header
        CrmService->>CrmServer: GET /api/GetClientData?id={clientId}<br/>Authorization: Bearer {token}
        activate CrmServer
        
        alt CRM Server Success
            CrmServer-->>CrmService: 200 OK + Client Data
            deactivate CrmServer
            CrmService-->>Controller: Client data string
            deactivate CrmService
            Controller-->>React: 200 OK<br/>{ "data": "..." }
            deactivate Controller
            React-->>User: Display client data ✅
        else CRM Server Error
            CrmServer-->>CrmService: Error response
            deactivate CrmServer
            CrmService-->>Controller: HttpRequestException
            deactivate CrmService
            Controller-->>React: 503 Service Unavailable ❌
            deactivate Controller
            React-->>User: Display error message
        end
    end
```

## Authentication Flow

### OAuth 2.0 Client Credentials Flow

```mermaid
sequenceDiagram
    participant App as 🔷 Application<br/>(TokenService)
    participant Cache as 💾 Token Cache<br/>(In-Memory)
    participant Server as 🔑 Token Server<br/>(OAuth Provider)
    
    Note over App,Server: OAuth 2.0 Client Credentials Flow
    
    App->>Cache: Check cached token
    alt Token exists and valid (>5 min remaining)
        Cache-->>App: Return cached token ✅
    else Token expired or missing
        App->>App: Acquire semaphore lock 🔒
        App->>Cache: Double-check cache
        alt Still invalid
            App->>Server: POST /oauth/token<br/>grant_type=client_credentials<br/>client_id={id}<br/>client_secret={secret}
            activate Server
            Server-->>App: 200 OK<br/>{<br/>  "access_token": "...",<br/>  "token_type": "Bearer",<br/>  "expires_in": 3600<br/>}
            deactivate Server
            App->>Cache: Store token<br/>(expires_at = now + 3540s)
            Cache-->>App: Token cached ✅
        else Token refreshed by another thread
            Cache-->>App: Return refreshed token ✅
        end
        App->>App: Release semaphore lock 🔓
    end
    
    Note over App,Cache: Token automatically refreshed<br/>1 minute before expiration
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

### Development Architecture

```mermaid
graph TB
    subgraph Dev["🛠️ Development Environment"]
        DevReact[React Dev Server<br/>⚛️ Port 5173<br/>Hot Module Replacement]
        DevAPI[ASP.NET Core API<br/>🔷 Port 5000<br/>Swagger Enabled]
    end
    
    DevReact -->|Proxy /api/*| DevAPI
    DevAPI -->|Serves API| DevReact
    
    classDef dev fill:#61dafb,stroke:#20232a,stroke-width:2px,color:#000
    class DevReact,DevAPI dev
```

### Production Architecture

```mermaid
graph TB
    subgraph Prod["🚀 Production Environment"]
        LoadBalancer[Load Balancer<br/>⚖️ Nginx/IIS]
        
        subgraph AppServer["Application Server"]
            DotNetApp[ASP.NET Core App<br/>🔷 Single Process]
            
            subgraph StaticFiles["Static Files"]
                ReactBuild[React Build<br/>📦 wwwroot/<br/>Static Assets]
            end
            
            subgraph Services["Services"]
                TokenSvcProd[Token Service<br/>🔐 OAuth]
                CrmSvcProd[CRM Service<br/>📊 Data]
            end
        end
        
        subgraph External["External Services"]
            TokenServerProd[Token Server<br/>🔑 OAuth Provider]
            CrmServerProd[CRM Server<br/>📈 External API]
        end
    end
    
    LoadBalancer --> DotNetApp
    DotNetApp --> ReactBuild
    DotNetApp --> TokenSvcProd
    DotNetApp --> CrmSvcProd
    TokenSvcProd --> TokenServerProd
    CrmSvcProd --> CrmServerProd
    
    classDef prod fill:#512bd4,stroke:#fff,stroke-width:2px,color:#fff
    classDef static fill:#61dafb,stroke:#20232a,stroke-width:2px,color:#000
    classDef service fill:#007acc,stroke:#fff,stroke-width:2px,color:#fff
    classDef external fill:#ff6b6b,stroke:#fff,stroke-width:2px,color:#fff
    
    class LoadBalancer,DotNetApp prod
    class ReactBuild static
    class TokenSvcProd,CrmSvcProd service
    class TokenServerProd,CrmServerProd external
```

### Development vs Production

| Aspect | Development | Production |
|-------|------------|------------|
| **Frontend** | Separate Vite dev server (Port 5173) | Built and served from `wwwroot` |
| **Backend** | ASP.NET Core API (Port 5000) | Single ASP.NET Core application |
| **API Proxy** | Vite dev server proxies `/api/*` | Same origin, no proxy needed |
| **Hot Reload** | ✅ Enabled | ❌ Disabled |
| **Swagger** | ✅ Enabled | ❌ Disabled (optional) |
| **Static Files** | Served by Vite | Served by ASP.NET Core middleware |
| **Routing** | Client-side routing via Vite | Fallback to `index.html` for SPA |

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
