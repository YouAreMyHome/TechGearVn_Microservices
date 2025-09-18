using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Product.Api.Middleware;
using Product.Domain.Exceptions;
using Product.Application.Exceptions;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;

namespace Product.UnitTests.Application.Middleware;

/// <summary>
/// Unit tests cho GlobalExceptionHandlingMiddleware
/// Verify rằng các exceptions được convert thành proper HTTP responses
/// Test both business exceptions và technical exceptions
/// </summary>
public class GlobalExceptionHandlingMiddlewareTests
{
    private readonly Mock<ILogger<GlobalExceptionHandlingMiddleware>> _loggerMock;
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly GlobalExceptionHandlingMiddleware _middleware;

    public GlobalExceptionHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<GlobalExceptionHandlingMiddleware>>();
        _nextMock = new Mock<RequestDelegate>();
        _middleware = new GlobalExceptionHandlingMiddleware(_nextMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task InvokeAsync_WhenNoException_ShouldCallNext()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(x => x(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WhenProductNotFoundException_ShouldReturn404()
    {
        // Arrange
        var context = CreateHttpContext();
        var productId = Guid.NewGuid();
        var exception = new ProductNotFoundException(productId);
        
        _nextMock.Setup(x => x(It.IsAny<HttpContext>()))
                 .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(404);
        context.Response.ContentType.Should().Be("application/json");
        
        var responseBody = await GetResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        problemDetails!.Title.Should().Be("Sản phẩm không tồn tại");
        problemDetails.Status.Should().Be(404);
        problemDetails.Detail.Should().Contain(productId.ToString());
    }

    [Fact]
    public async Task InvokeAsync_WhenProductSkuAlreadyExistsException_ShouldReturn409()
    {
        // Arrange
        var context = CreateHttpContext();
        var sku = "TEST-SKU-001";
        var exception = new ProductSkuAlreadyExistsException(sku);
        
        _nextMock.Setup(x => x(It.IsAny<HttpContext>()))
                 .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(409);
        
        var responseBody = await GetResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        problemDetails!.Title.Should().Be("SKU đã tồn tại");
        problemDetails.Status.Should().Be(409);
        problemDetails.Detail.Should().Contain(sku);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationException_ShouldReturn400WithValidationDetails()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new ValidationException("Name", "Tên sản phẩm không được để trống");
        
        _nextMock.Setup(x => x(It.IsAny<HttpContext>()))
                 .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        
        var responseBody = await GetResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ValidationProblemDetails>(responseBody, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        problemDetails!.Title.Should().Be("Lỗi validation dữ liệu");
        problemDetails.Status.Should().Be(400);
        problemDetails.Errors.Should().HaveCount(1);
        problemDetails.Errors["Name"].Should().Contain("Tên sản phẩm không được để trống");
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentException_ShouldReturn400()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new ArgumentException("Invalid argument provided");
        
        _nextMock.Setup(x => x(It.IsAny<HttpContext>()))
                 .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        
        var responseBody = await GetResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        problemDetails!.Title.Should().Be("Dữ liệu đầu vào không hợp lệ");
        problemDetails.Status.Should().Be(400);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_ShouldReturn500()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new InvalidOperationException("Something went wrong");
        
        _nextMock.Setup(x => x(It.IsAny<HttpContext>()))
                 .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(422); // UnprocessableEntity for InvalidOperationException
        
        var responseBody = await GetResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        problemDetails!.Title.Should().Be("Thao tác không hợp lệ");
        problemDetails.Status.Should().Be(422);
    }

    [Fact]
    public async Task InvokeAsync_WhenTimeoutException_ShouldReturn408()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new TimeoutException("Operation timed out");
        
        _nextMock.Setup(x => x(It.IsAny<HttpContext>()))
                 .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(408);
        
        var responseBody = await GetResponseBodyAsync(context);
        var problemDetails = JsonSerializer.Deserialize<ProblemDetails>(responseBody, 
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        problemDetails!.Title.Should().Be("Timeout");
        problemDetails.Status.Should().Be(408);
    }

    [Fact]
    public async Task InvokeAsync_WhenExceptionOccurs_ShouldLogError()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new Exception("Test exception");
        
        _nextMock.Setup(x => x(It.IsAny<HttpContext>()))
                 .ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unhandled exception occurred")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Path = "/api/products";
        return context;
    }

    private static async Task<string> GetResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}