using CrmClientApp.Controllers;
using CrmClientApp.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CrmClientApp.Tests.Controllers;

public class CrmControllerTests
{
    private readonly Mock<ICrmService> _mockCrmService;
    private readonly Mock<ILogger<CrmController>> _mockLogger;
    private readonly CrmController _controller;

    public CrmControllerTests()
    {
        _mockCrmService = new Mock<ICrmService>();
        _mockLogger = new Mock<ILogger<CrmController>>();
        _controller = new CrmController(_mockCrmService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetClientData_ShouldReturnOk_WhenClientIdIsValid()
    {
        // Arrange
        var expectedData = "{\"clientId\":\"123\",\"name\":\"Test Client\"}";
        _mockCrmService.Setup(x => x.GetClientDataAsync("123"))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _controller.GetClientData("123");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().NotBeNull();
        
        var value = okResult.Value as dynamic;
        value!.data.Should().Be(expectedData);
    }

    [Fact]
    public async Task GetClientData_ShouldReturnBadRequest_WhenClientIdIsEmpty()
    {
        // Act
        var result = await _controller.GetClientData("");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetClientData_ShouldReturnBadRequest_WhenClientIdIsNull()
    {
        // Act
        var result = await _controller.GetClientData(null!);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetClientData_ShouldReturnBadRequest_WhenClientIdIsWhitespace()
    {
        // Act
        var result = await _controller.GetClientData("   ");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetClientData_ShouldReturnServiceUnavailable_WhenHttpRequestExceptionOccurs()
    {
        // Arrange
        _mockCrmService.Setup(x => x.GetClientDataAsync("123"))
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        // Act
        var result = await _controller.GetClientData("123");

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(503);
        objectResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetClientData_ShouldReturnInternalServerError_WhenUnexpectedExceptionOccurs()
    {
        // Arrange
        _mockCrmService.Setup(x => x.GetClientDataAsync("123"))
            .ThrowsAsync(new InvalidOperationException("Unexpected error"));

        // Act
        var result = await _controller.GetClientData("123");

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        objectResult.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task GetClientData_ShouldCallCrmService_WithCorrectClientId()
    {
        // Arrange
        var clientId = "test-client-123";
        _mockCrmService.Setup(x => x.GetClientDataAsync(clientId))
            .ReturnsAsync("{}");

        // Act
        await _controller.GetClientData(clientId);

        // Assert
        _mockCrmService.Verify(x => x.GetClientDataAsync(clientId), Times.Once);
    }
}
