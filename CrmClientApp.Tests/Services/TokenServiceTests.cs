using System.Net;
using System.Text;
using System.Text.Json;
using CrmClientApp.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace CrmClientApp.Tests.Services;

[CollectionDefinition("TokenService Tests", DisableParallelization = true)]
public class TokenServiceTestCollection
{
}

[Collection("TokenService Tests")]
public class TokenServiceTests : IDisposable
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<ILogger<TokenService>> _mockLogger;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly IConfiguration _configuration;

    public TokenServiceTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockLogger = new Mock<ILogger<TokenService>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        
        var inMemorySettings = new Dictionary<string, string?>();
        
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenClientIdIsMissing()
    {
        // Arrange - Clear any previously set environment variables
        Environment.SetEnvironmentVariable("CRM_TOKEN_URL", "https://www.tokenserver.com/oauth/token");
        Environment.SetEnvironmentVariable("CRM_CLIENT_ID", null);
        Environment.SetEnvironmentVariable("CRM_CLIENT_SECRET", "secret");
        
        try
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new TokenService(_configuration, _mockLogger.Object, _mockHttpClientFactory.Object));
            
            exception.Message.Should().Contain("CRM_CLIENT_ID");
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("CRM_TOKEN_URL", null);
            Environment.SetEnvironmentVariable("CRM_CLIENT_ID", null);
            Environment.SetEnvironmentVariable("CRM_CLIENT_SECRET", null);
        }
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenClientSecretIsMissing()
    {
        // Arrange - Clear any previously set environment variables
        Environment.SetEnvironmentVariable("CRM_TOKEN_URL", "https://www.tokenserver.com/oauth/token");
        Environment.SetEnvironmentVariable("CRM_CLIENT_ID", "client-id");
        Environment.SetEnvironmentVariable("CRM_CLIENT_SECRET", null);

        try
        {
            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new TokenService(_configuration, _mockLogger.Object, _mockHttpClientFactory.Object));
            
            exception.Message.Should().Contain("CRM_CLIENT_SECRET");
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("CRM_TOKEN_URL", null);
            Environment.SetEnvironmentVariable("CRM_CLIENT_ID", null);
            Environment.SetEnvironmentVariable("CRM_CLIENT_SECRET", null);
        }
    }

    [Fact]
    public async Task GetTokenAsync_ShouldReturnToken_WhenRequestIsSuccessful()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CRM_TOKEN_URL", "https://www.tokenserver.com/oauth/token");
        Environment.SetEnvironmentVariable("CRM_CLIENT_ID", "test-client-id");
        Environment.SetEnvironmentVariable("CRM_CLIENT_SECRET", "test-secret");
        Environment.SetEnvironmentVariable("CRM_USERNAME", "test-user");
        Environment.SetEnvironmentVariable("CRM_PASSWORD", "test-password");

        var responseContent = @"{""access_token"":""test-access-token-12345"",""token_type"":""Bearer"",""expires_in"":3600,""scope"":""read write""}";
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var tokenService = new TokenService(_configuration, _mockLogger.Object, _mockHttpClientFactory.Object);

        // Act
        var token = await tokenService.GetTokenAsync();

        // Assert
        token.Should().Be("test-access-token-12345");
    }

    [Fact]
    public async Task GetTokenAsync_ShouldUseCachedToken_WhenTokenIsStillValid()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CRM_TOKEN_URL", "https://www.tokenserver.com/oauth/token");
        Environment.SetEnvironmentVariable("CRM_CLIENT_ID", "test-client-id");
        Environment.SetEnvironmentVariable("CRM_CLIENT_SECRET", "test-secret");
        Environment.SetEnvironmentVariable("CRM_USERNAME", "test-user");
        Environment.SetEnvironmentVariable("CRM_PASSWORD", "test-password");

        var responseContent = @"{""access_token"":""cached-token"",""token_type"":""Bearer"",""expires_in"":3600}";
        
        var callCount = 0;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                };
            });

        var tokenService = new TokenService(_configuration, _mockLogger.Object, _mockHttpClientFactory.Object);

        // Act - Call twice
        var token1 = await tokenService.GetTokenAsync();
        var token2 = await tokenService.GetTokenAsync();

        // Assert
        token1.Should().Be("cached-token");
        token2.Should().Be("cached-token");
        callCount.Should().Be(1); // Should only call once due to caching
    }

    [Fact]
    public async Task GetTokenAsync_ShouldIncludeScope_WhenScopeIsConfigured()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CRM_TOKEN_URL", "https://www.tokenserver.com/oauth/token");
        Environment.SetEnvironmentVariable("CRM_SCOPE", "read write");
        Environment.SetEnvironmentVariable("CRM_CLIENT_ID", "test-client-id");
        Environment.SetEnvironmentVariable("CRM_CLIENT_SECRET", "test-secret");
        Environment.SetEnvironmentVariable("CRM_USERNAME", "test-user");
        Environment.SetEnvironmentVariable("CRM_PASSWORD", "test-password");

        var responseContent = @"{""access_token"":""test-token"",""expires_in"":3600}";
        
        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                capturedRequest = request;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                };
            });

        var tokenService = new TokenService(_configuration, _mockLogger.Object, _mockHttpClientFactory.Object);

        // Act
        await tokenService.GetTokenAsync();

        // Assert
        capturedRequest.Should().NotBeNull();
        var content = await capturedRequest!.Content!.ReadAsStringAsync();
        content.Should().Contain("scope=read+write");
    }

    [Fact]
    public async Task GetTokenAsync_ShouldUseBasicAuth_WhenConfigured()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CRM_TOKEN_URL", "https://www.tokenserver.com/oauth/token");
        Environment.SetEnvironmentVariable("USE_BASIC_AUTH", "true");
        Environment.SetEnvironmentVariable("CRM_CLIENT_ID", "test-client-id");
        Environment.SetEnvironmentVariable("CRM_CLIENT_SECRET", "test-secret");
        Environment.SetEnvironmentVariable("CRM_USERNAME", "test-user");
        Environment.SetEnvironmentVariable("CRM_PASSWORD", "test-password");

        var responseContent = @"{""access_token"":""test-token-basic"",""expires_in"":3600}";
        
        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                capturedRequest = request;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
                };
            });

        var tokenService = new TokenService(_configuration, _mockLogger.Object, _mockHttpClientFactory.Object);

        // Act
        await tokenService.GetTokenAsync();

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Authorization.Should().NotBeNull();
        capturedRequest.Headers.Authorization!.Scheme.Should().Be("Basic");
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrow_WhenTokenServerReturnsError()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CRM_TOKEN_URL", "https://www.tokenserver.com/oauth/token");
        Environment.SetEnvironmentVariable("CRM_CLIENT_ID", "test-client-id");
        Environment.SetEnvironmentVariable("CRM_CLIENT_SECRET", "test-secret");
        Environment.SetEnvironmentVariable("CRM_USERNAME", "test-user");
        Environment.SetEnvironmentVariable("CRM_PASSWORD", "test-password");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Content = new StringContent("Unauthorized", Encoding.UTF8, "text/plain")
            });

        var tokenService = new TokenService(_configuration, _mockLogger.Object, _mockHttpClientFactory.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => tokenService.GetTokenAsync());
    }

    [Fact]
    public async Task GetTokenAsync_ShouldThrow_WhenTokenResponseIsInvalid()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CRM_TOKEN_URL", "https://www.tokenserver.com/oauth/token");
        Environment.SetEnvironmentVariable("CRM_CLIENT_ID", "test-client-id");
        Environment.SetEnvironmentVariable("CRM_CLIENT_SECRET", "test-secret");
        Environment.SetEnvironmentVariable("CRM_USERNAME", "test-user");
        Environment.SetEnvironmentVariable("CRM_PASSWORD", "test-password");

        var invalidResponse = new { invalid_field = "value" };
        var responseContent = JsonSerializer.Serialize(invalidResponse);
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            });

        var tokenService = new TokenService(_configuration, _mockLogger.Object, _mockHttpClientFactory.Object);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => tokenService.GetTokenAsync());
        exception.Message.Should().Contain("Invalid token response");
    }

    [Fact]
    public async Task GetTokenAsync_ShouldRefreshToken_WhenTokenIsExpired()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CRM_TOKEN_URL", "https://www.tokenserver.com/oauth/token");
        Environment.SetEnvironmentVariable("CRM_CLIENT_ID", "test-client-id");
        Environment.SetEnvironmentVariable("CRM_CLIENT_SECRET", "test-secret");
        Environment.SetEnvironmentVariable("CRM_USERNAME", "test-user");
        Environment.SetEnvironmentVariable("CRM_PASSWORD", "test-password");

        var tokenResponse1Content = @"{""access_token"":""first-token"",""expires_in"":1}";
        var tokenResponse2Content = @"{""access_token"":""refreshed-token"",""expires_in"":3600}";

        var callCount = 0;
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                var response = callCount == 1 
                    ? tokenResponse1Content
                    : tokenResponse2Content;
                
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(response, Encoding.UTF8, "application/json")
                };
            });

        var tokenService = new TokenService(_configuration, _mockLogger.Object, _mockHttpClientFactory.Object);

        // Act - Get first token
        var token1 = await tokenService.GetTokenAsync();
        
        // Wait for token to expire
        await Task.Delay(2100); // Wait longer than 1 second expiry + 1 minute buffer
        
        // Get token again - should refresh
        var token2 = await tokenService.GetTokenAsync();

        // Assert
        token1.Should().Be("first-token");
        token2.Should().Be("refreshed-token");
        callCount.Should().Be(2); // Should call twice
    }
    
    public void Dispose()
    {
        // Clean up environment variables after each test
        Environment.SetEnvironmentVariable("CRM_TOKEN_URL", null);
        Environment.SetEnvironmentVariable("GRANT_TYPE", null);
        Environment.SetEnvironmentVariable("CRM_SCOPE", null);
        Environment.SetEnvironmentVariable("USE_BASIC_AUTH", null);
        Environment.SetEnvironmentVariable("CRM_CLIENT_ID", null);
        Environment.SetEnvironmentVariable("CRM_CLIENT_SECRET", null);
        Environment.SetEnvironmentVariable("CRM_USERNAME", null);
        Environment.SetEnvironmentVariable("CRM_PASSWORD", null);
    }
}
