# CrmClientApp Tests

This project contains comprehensive unit and integration tests for the CRM Client application.

## Test Structure

- **Unit Tests**: Test individual components in isolation using mocks
  - `Services/TokenServiceTests.cs` - Tests for OAuth token service
  - `Services/CrmServiceTests.cs` - Tests for CRM API service
  - `Controllers/CrmControllerTests.cs` - Tests for API controller

- **Integration Tests**: Test against live API endpoints
  - `Integration/LiveApiIntegrationTests.cs` - End-to-end tests with real servers

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Only Unit Tests (Exclude Integration Tests)

```bash
dotnet test --filter "Category!=Integration"
```

### Run Only Integration Tests

```bash
dotnet test --filter "Category=Integration"
```

## Integration Test Setup

Integration tests require environment variables to be set before running:

### Required Environment Variables

```bash
export OAUTH_CLIENT_ID="your-client-id"
export OAUTH_CLIENT_SECRET="your-client-secret"
```

### Optional Environment Variables

```bash
# Override default token endpoint
export TEST_TOKEN_ENDPOINT="https://custom-token-server.com/oauth/token"

# Override default CRM server URL
export TEST_CRM_BASE_URL="https://custom-crm-server.com/"

# Specify test client ID to use for API calls
export TEST_CRM_CLIENT_ID="123"

# Specify OAuth scope (if required)
export TEST_TOKEN_SCOPE="read write"

# Use Basic Auth instead of form parameters
export TEST_USE_BASIC_AUTH="true"
```

### Windows (PowerShell)

```powershell
$env:OAUTH_CLIENT_ID="your-client-id"
$env:OAUTH_CLIENT_SECRET="your-client-secret"
$env:TEST_CRM_CLIENT_ID="123"
```

### Windows (Command Prompt)

```cmd
set OAUTH_CLIENT_ID=your-client-id
set OAUTH_CLIENT_SECRET=your-client-secret
set TEST_CRM_CLIENT_ID=123
```

## Test Coverage

### TokenService Tests

- ✅ Constructor validation (missing environment variables)
- ✅ Successful token retrieval
- ✅ Token caching
- ✅ Scope inclusion in requests
- ✅ Basic Auth support
- ✅ Error handling (server errors, invalid responses)
- ✅ Token refresh when expired

### CrmService Tests

- ✅ Successful data retrieval
- ✅ Token inclusion in request headers
- ✅ Correct endpoint usage
- ✅ Custom header format support
- ✅ Error handling (HTTP errors, token service errors)

### CrmController Tests

- ✅ Successful requests
- ✅ Input validation (empty/null/whitespace client IDs)
- ✅ Error handling (HTTP exceptions, unexpected errors)
- ✅ Service method invocation

### Integration Tests

- ✅ Live token server communication
- ✅ Token caching behavior
- ✅ Live CRM server communication
- ✅ End-to-end workflow
- ✅ Token refresh handling

## Continuous Integration

For CI/CD pipelines, ensure environment variables are set in your CI configuration:

```yaml
# Example GitHub Actions
env:
  OAUTH_CLIENT_ID: ${{ secrets.OAUTH_CLIENT_ID }}
  OAUTH_CLIENT_SECRET: ${{ secrets.OAUTH_CLIENT_SECRET }}
  TEST_CRM_CLIENT_ID: ${{ secrets.TEST_CRM_CLIENT_ID }}
```

## Notes

- Integration tests make real HTTP requests to live servers
- Integration tests are marked with `[Trait("Category", "Integration")]` to allow filtering
- Unit tests use mocks and don't require network access
- All tests use FluentAssertions for readable assertions
- Tests are designed to be run in parallel where possible
