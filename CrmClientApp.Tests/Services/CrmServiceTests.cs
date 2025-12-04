using System.Net;
using System.Text;
using CrmClientApp.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace CrmClientApp.Tests.Services;

public class CrmServiceTests
{
    private readonly Mock<ILogger<CrmService>> _mockLogger;
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly IConfiguration _configuration;

    public CrmServiceTests()
    {
        _mockLogger = new Mock<ILogger<CrmService>>();
        _mockTokenService = new Mock<ITokenService>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ExternalApi:Token:HeaderName", "Authorization" },
            { "ExternalApi:Token:HeaderFormat", "Bearer {0}" }
        };
        
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task GetClientDataAsync_ShouldReturnData_WhenRequestIsSuccessful()
    {
        // Arrange
        var expectedData = "{\"clientId\":\"123\",\"name\":\"Test Client\"}";
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://www.crmserver.com/")
        };

        _mockTokenService.Setup(x => x.GetTokenAsync())
            .ReturnsAsync("test-token-12345");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedData, Encoding.UTF8, "application/json")
            });

        var crmService = new CrmService(httpClient, _mockLogger.Object, _mockTokenService.Object, _configuration);

        // Act
        var result = await crmService.GetClientDataAsync("123");

        // Assert
        result.Should().Be(expectedData);
        _mockTokenService.Verify(x => x.GetTokenAsync(), Times.Once);
    }

    [Fact]
    public async Task GetClientDataAsync_ShouldIncludeTokenInHeader_WhenMakingRequest()
    {
        // Arrange
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://www.crmserver.com/")
        };

        _mockTokenService.Setup(x => x.GetTokenAsync())
            .ReturnsAsync("test-oauth-token");

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
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            });

        var crmService = new CrmService(httpClient, _mockLogger.Object, _mockTokenService.Object, _configuration);

        // Act
        await crmService.GetClientDataAsync("123");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Contains("Authorization").Should().BeTrue();
        capturedRequest.Headers.GetValues("Authorization").First().Should().Be("Bearer test-oauth-token");
    }

    [Fact]
    public async Task GetClientDataAsync_ShouldUseCorrectEndpoint_WhenMakingRequest()
    {
        // Arrange
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://www.crmserver.com/")
        };

        _mockTokenService.Setup(x => x.GetTokenAsync())
            .ReturnsAsync("test-token");

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
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            });

        var crmService = new CrmService(httpClient, _mockLogger.Object, _mockTokenService.Object, _configuration);

        // Act
        await crmService.GetClientDataAsync("12345");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Contain("/api/v1/ClientData/12345");
    }

    [Fact]
    public async Task GetClientDataAsync_ShouldUseCustomHeaderFormat_WhenConfigured()
    {
        // Arrange
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://www.crmserver.com/")
        };

        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "ExternalApi:Token:HeaderName", "X-Api-Key" },
                { "ExternalApi:Token:HeaderFormat", "Token {0}" }
            })
            .Build();

        _mockTokenService.Setup(x => x.GetTokenAsync())
            .ReturnsAsync("custom-token");

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
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            });

        var crmService = new CrmService(httpClient, _mockLogger.Object, _mockTokenService.Object, customConfig);

        // Act
        await crmService.GetClientDataAsync("123");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Headers.Contains("X-Api-Key").Should().BeTrue();
        capturedRequest.Headers.GetValues("X-Api-Key").First().Should().Be("Token custom-token");
    }

    [Fact]
    public async Task GetClientDataAsync_ShouldThrow_WhenHttpRequestFails()
    {
        // Arrange
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://www.crmserver.com/")
        };

        _mockTokenService.Setup(x => x.GetTokenAsync())
            .ReturnsAsync("test-token");

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Server Error", Encoding.UTF8, "text/plain")
            });

        var crmService = new CrmService(httpClient, _mockLogger.Object, _mockTokenService.Object, _configuration);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => crmService.GetClientDataAsync("123"));
    }

    [Fact]
    public async Task GetClientDataAsync_ShouldThrow_WhenTokenServiceFails()
    {
        // Arrange
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://www.crmserver.com/")
        };

        _mockTokenService.Setup(x => x.GetTokenAsync())
            .ThrowsAsync(new HttpRequestException("Token service error"));

        var crmService = new CrmService(httpClient, _mockLogger.Object, _mockTokenService.Object, _configuration);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => crmService.GetClientDataAsync("123"));
    }
}
