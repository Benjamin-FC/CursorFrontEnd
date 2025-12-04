# API Documentation

## Base URL

- **Development**: `http://localhost:5000` or `https://localhost:5001`
- **Production**: Configured per deployment environment

## Authentication

All API requests to the external CRM server are authenticated using OAuth 2.0 Bearer tokens. The backend automatically retrieves and includes the token in requests. No authentication is required for the API endpoints themselves (they are public endpoints that proxy to the authenticated external API).

## Endpoints

### GET /api/Crm/GetClientData

Retrieves client data from the external CRM server for the specified client ID.

#### Request

**URL**: `/api/Crm/GetClientData`

**Method**: `GET`

**Query Parameters**:
| Parameter | Type   | Required | Description                    |
|-----------|--------|----------|--------------------------------|
| `id`      | string | Yes      | The unique identifier of the client |

**Example Request**:
```http
GET /api/Crm/GetClientData?id=12345 HTTP/1.1
Host: localhost:5000
```

#### Response

**Success Response** (200 OK):
```json
{
  "data": "Client data content from CRM server"
}
```

**Error Responses**:

**400 Bad Request** - Invalid or missing client ID:
```json
{
  "error": "Client ID is required"
}
```

**503 Service Unavailable** - CRM server unreachable or returned an error:
```json
{
  "error": "Unable to connect to CRM server",
  "message": "Detailed error message from HTTP request"
}
```

**500 Internal Server Error** - Unexpected error:
```json
{
  "error": "An error occurred while retrieving client data",
  "message": "Detailed error message"
}
```

#### Example Usage

**cURL**:
```bash
curl -X GET "http://localhost:5000/api/Crm/GetClientData?id=12345"
```

**JavaScript (Fetch API)**:
```javascript
const response = await fetch('http://localhost:5000/api/Crm/GetClientData?id=12345');
const data = await response.json();
console.log(data);
```

**C# (HttpClient)**:
```csharp
using var client = new HttpClient();
var response = await client.GetAsync("http://localhost:5000/api/Crm/GetClientData?id=12345");
var content = await response.Content.ReadAsStringAsync();
```

## Error Codes

| Status Code | Description                                    |
|-------------|------------------------------------------------|
| 200         | Success - Client data retrieved successfully   |
| 400         | Bad Request - Invalid or missing parameters    |
| 503         | Service Unavailable - External CRM server error|
| 500         | Internal Server Error - Unexpected error       |

## Rate Limiting

Currently, no rate limiting is implemented. Rate limiting may be added in future versions.

## CORS

Cross-Origin Resource Sharing (CORS) is configured to allow requests from:
- `http://localhost:5173` (development)

For production, update CORS configuration in `Program.cs` to allow your frontend domain.

## Swagger/OpenAPI

Swagger UI is available in development mode at:
- `http://localhost:5000/swagger` (HTTP)
- `https://localhost:5001/swagger` (HTTPS)

## External API Integration

### Token Server

The backend automatically retrieves OAuth tokens from the configured token server:

**Endpoint**: Configured in `appsettings.json` under `ExternalApi:Token:Endpoint`
**Default**: `https://www.tokenserver.com/oauth/token`

**Request Format**:
```
POST /oauth/token
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&client_id={client_id}&client_secret={client_secret}&scope={scope}
```

**Response Format**:
```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "read write"
}
```

### CRM Server

The backend makes authenticated requests to the CRM server:

**Base URL**: Configured in `appsettings.json` under `ExternalApi:CrmServer:BaseUrl`
**Default**: `https://www.crmserver.com/`

**Endpoint**: `/api/GetClientData?id={clientId}`

**Authentication**: OAuth Bearer token in `Authorization` header:
```
Authorization: Bearer {access_token}
```

**Request Headers**:
- `Authorization`: Bearer token (automatically added)
- Header name and format configurable in `appsettings.json`

## Request/Response Examples

### Successful Request

**Request**:
```http
GET /api/Crm/GetClientData?id=12345 HTTP/1.1
Host: localhost:5000
```

**Response**:
```http
HTTP/1.1 200 OK
Content-Type: application/json

{
  "data": "{\"clientId\":\"12345\",\"name\":\"John Doe\",\"email\":\"john@example.com\"}"
}
```

### Missing Client ID

**Request**:
```http
GET /api/Crm/GetClientData HTTP/1.1
Host: localhost:5000
```

**Response**:
```http
HTTP/1.1 400 Bad Request
Content-Type: application/json

{
  "error": "Client ID is required"
}
```

### CRM Server Error

**Request**:
```http
GET /api/Crm/GetClientData?id=12345 HTTP/1.1
Host: localhost:5000
```

**Response**:
```http
HTTP/1.1 503 Service Unavailable
Content-Type: application/json

{
  "error": "Unable to connect to CRM server",
  "message": "The remote server returned an error: (404) Not Found."
}
```

## Testing

### Using Swagger UI

1. Start the application in development mode
2. Navigate to `http://localhost:5000/swagger`
3. Expand the `Crm` controller
4. Click on `GET /api/Crm/GetClientData`
5. Click "Try it out"
6. Enter a client ID in the `id` parameter
7. Click "Execute"
8. View the response

### Using Postman

1. Create a new GET request
2. URL: `http://localhost:5000/api/Crm/GetClientData?id=12345`
3. Send the request
4. View the response

### Using Integration Tests

See `CrmClientApp.Tests/Integration/LiveApiIntegrationTests.cs` for examples of programmatic API testing.

## Notes

- All timestamps are in UTC
- All responses are in JSON format
- The API is stateless (no session management)
- OAuth tokens are automatically cached and refreshed
- Token refresh occurs 1 minute before expiration
- Thread-safe token retrieval ensures only one token request at a time
