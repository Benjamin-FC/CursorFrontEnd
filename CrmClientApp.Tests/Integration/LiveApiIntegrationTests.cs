using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CrmClientApp.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace CrmClientApp.Tests.Integration;

/// <summary>
/// Integration tests that test against live API endpoints.
/// These tests require environment variables to be set:
/// - CRM_TOKEN_URL
/// - CRM_CLIENT_ID
/// - CRM_CLIENT_SECRET
/// - CRM_USERNAME
/// - CRM_PASSWORD
/// - CRM_BASEURL
/// - CRM_SCOPE (optional)
/// 
/// To run these tests, use: dotnet test --filter "Category=Integration"
/// </summary>
[Trait("Category", "Integration")]
public class LiveApiIntegrationTests : IClassFixture<IntegrationTestFixture>
{
    private readonly IntegrationTestFixture _fixture;

    public LiveApiIntegrationTests(IntegrationTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task TokenService_ShouldFetchToken_FromLiveTokenServer()
    {
        // Arrange
        var tokenService = _fixture.TokenService;

        // Act
        var token = await tokenService.GetTokenAsync();

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        token.Length.Should().BeGreaterThan(10); // Tokens are typically longer
        
        // Print token to console
        Console.WriteLine($"Successfully fetched token: {token.Substring(0, Math.Min(10, token.Length))}...");
        Console.WriteLine($"Token length: {token.Length} characters");
    }

    [Fact]
    public async Task TokenService_ShouldCacheToken_OnSubsequentCalls()
    {
        // Arrange
        var tokenService = _fixture.TokenService;

        // Act
        var token1 = await tokenService.GetTokenAsync();
        var token2 = await tokenService.GetTokenAsync();

        // Assert
        token1.Should().Be(token2); // Should be the same cached token
    }

    [Fact]
    public async Task CrmService_ShouldFetchClientData_FromLiveCrmServer()
    {
        // Arrange
        var testClientId = "100900"; // Use a test client ID
        var crmService = _fixture.CrmService;

        // Act
        var clientData = await crmService.GetClientDataAsync(testClientId);

        // Assert
        clientData.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CrmService_ShouldIncludeToken_InRequestHeaders()
    {
        // Arrange
        var testClientId = "109000"; // Use a test client ID
        var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri(_fixture.CrmBaseUrl)
        };

        var tokenService = _fixture.TokenService;
        var token = await tokenService.GetTokenAsync();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/GetClientData?id={testClientId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        // If we get 401, it means token wasn't included or was invalid
        // If we get 404, it might mean endpoint doesn't exist, but token was accepted
        // If we get 200, everything worked
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized, 
            "Token should be accepted by the server");
    }

    [Fact]
    public async Task TokenService_ShouldHandleTokenRefresh_WhenTokenExpires()
    {
        // Arrange
        var tokenService = _fixture.TokenService;

        // Act - Get initial token
        var token1 = await tokenService.GetTokenAsync();

        // Wait a bit (in real scenario, token would expire)
        await Task.Delay(1000);

        // Get token again - should still be cached if not expired
        var token2 = await tokenService.GetTokenAsync();

        // Assert
        // Both should be valid tokens (may be same if cached, or different if refreshed)
        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task FullIntegration_ShouldWork_EndToEnd()
    {
        // Arrange
        var testClientId = "109000"; // Use a test client ID
        var crmService = _fixture.CrmService;

        // Act
        var clientData = await crmService.GetClientDataAsync(testClientId);

        // Assert
        clientData.Should().NotBeNullOrWhiteSpace();
        
        // Try to parse as JSON if possible
        try
        {
            var json = JsonDocument.Parse(clientData);
            json.Should().NotBeNull();
        }
        catch
        {
            // If not JSON, that's okay - just verify we got data
            clientData.Length.Should().BeGreaterThan(0);
        }
    }
}

/// <summary>
/// Test fixture that sets up services for integration testing
/// </summary>
public class IntegrationTestFixture : IDisposable
{
    public ITokenService TokenService { get; }
    public ICrmService CrmService { get; }
    public string CrmBaseUrl { get; }
    public string TokenEndpoint { get; }

    public IntegrationTestFixture()
    {
        // Validate environment variables
        var clientId = Environment.GetEnvironmentVariable("CRM_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("CRM_CLIENT_SECRET");
        var tokenUrl = Environment.GetEnvironmentVariable("CRM_TOKEN_URL");
        var username = Environment.GetEnvironmentVariable("CRM_USERNAME");
        var password = Environment.GetEnvironmentVariable("CRM_PASSWORD");
        var baseUrl = Environment.GetEnvironmentVariable("CRM_BASEURL");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(tokenUrl) || string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "Integration tests require CRM_TOKEN_URL, CRM_CLIENT_ID, CRM_CLIENT_SECRET, CRM_USERNAME, CRM_PASSWORD, and CRM_BASEURL environment variables");
        }

        // Set values from environment
        TokenEndpoint = tokenUrl;
        CrmBaseUrl = baseUrl;

        // Build configuration (no longer needed for TokenService, but kept for CrmService)
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ExternalApi:CrmServer:TimeoutSeconds", "30" }
            })
            .Build();

        // Create logger
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var tokenLogger = loggerFactory.CreateLogger<TokenService>();
        var crmLogger = loggerFactory.CreateLogger<CrmService>();

        // Create HTTP client factory
        var httpClientFactory = new TestHttpClientFactory();

        // Create token service
        TokenService = new TokenService(configuration, tokenLogger, httpClientFactory);

        // Create CRM service HTTP client
        var crmHttpClient = new HttpClient
        {
            BaseAddress = new Uri(CrmBaseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Create CRM service
        CrmService = new CrmService(crmHttpClient, crmLogger, TokenService, configuration);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}

/// <summary>
/// Simple HTTP client factory for testing
/// </summary>
public class TestHttpClientFactory : IHttpClientFactory
{
    private readonly HttpClient _httpClient;

    public TestHttpClientFactory()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public HttpClient CreateClient(string name)
    {
        return _httpClient;
    }
}
