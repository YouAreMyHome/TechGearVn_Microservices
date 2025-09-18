using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Product.Api.Contracts.Products;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Product.UnitTests.Integration;

/// <summary>
/// Integration tests cho Products API endpoints
/// Test toàn bộ HTTP pipeline: Controllers → Application → Infrastructure
/// </summary>
public class ProductsControllerIntegrationTests : IClassFixture<ProductWebApplicationFactory>
{
    private readonly ProductWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public ProductsControllerIntegrationTests(ProductWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    [Fact]
    public async Task GetProducts_ShouldReturnSuccessAndCorrectContentType()
    {
        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.Should().BeSuccessful();
        response.Content.Headers.ContentType!.ToString().Should().Contain("application/json");
    }

    [Fact]
    public async Task GetProducts_ShouldReturnValidPagedResult()
    {
        // Act
        var response = await _client.GetAsync("/api/products?page=1&pageSize=10");

        // Assert
        response.Should().BeSuccessful();
        
        var jsonContent = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(jsonContent);
        var root = jsonDocument.RootElement;

        // Kiểm tra cấu trúc paged result
        root.TryGetProperty("items", out _).Should().BeTrue();
        root.TryGetProperty("totalCount", out _).Should().BeTrue();
        root.TryGetProperty("pageNumber", out _).Should().BeTrue();
        root.TryGetProperty("pageSize", out _).Should().BeTrue();
    }

    [Fact]
    public async Task CreateProduct_ValidData_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Test Product from Integration Test",
            Price = 99.99m,
            Currency = "USD",
            CategoryId = Guid.NewGuid(),
            Description = "Test product description",
            InitialStock = 50
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var jsonContent = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(jsonContent);
        var root = jsonDocument.RootElement;

        root.TryGetProperty("id", out _).Should().BeTrue();
        root.GetProperty("name").GetString().Should().Be(request.Name);
        root.GetProperty("price").GetProperty("amount").GetDecimal().Should().Be(request.Price);
    }

    [Fact]
    public async Task CreateProduct_InvalidData_ShouldReturnBadRequest()
    {
        // Arrange - Invalid: empty name and negative price
        var request = new CreateProductRequest
        {
            Name = "", // Invalid: empty name
            Price = -10, // Invalid: negative price
            Currency = "USD",
            CategoryId = Guid.Empty, // Invalid: empty guid
            Description = "Test description",
            InitialStock = -5 // Invalid: negative stock
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProductById_ExistingProduct_ShouldReturnProduct()
    {
        // Arrange - Tạo product trước
        var createRequest = new CreateProductRequest
        {
            Name = "Get By ID Test Product",
            Price = 149.99m,
            Currency = "USD",
            CategoryId = Guid.NewGuid(),
            Description = "Product for Get By ID test",
            InitialStock = 25
        };

        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        createResponse.Should().BeSuccessful();

        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createContent);
        var productId = createJson.RootElement.GetProperty("id").GetGuid();

        // Act
        var response = await _client.GetAsync($"/api/products/{productId}");

        // Assert
        response.Should().BeSuccessful();
        
        var jsonContent = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(jsonContent);
        var root = jsonDocument.RootElement;

        root.GetProperty("id").GetGuid().Should().Be(productId);
        root.GetProperty("name").GetString().Should().Be(createRequest.Name);
    }

    [Fact]
    public async Task GetProductById_NonExistentProduct_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/products/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_ExistingProduct_ShouldReturnNoContent()
    {
        // Arrange - Tạo product trước
        var createRequest = new CreateProductRequest
        {
            Name = "Product to Delete",
            Price = 99.99m,
            Currency = "USD",
            CategoryId = Guid.NewGuid(),
            Description = "Product to be deleted",
            InitialStock = 20
        };

        var createResponse = await _client.PostAsJsonAsync("/api/products", createRequest);
        var createContent = await createResponse.Content.ReadAsStringAsync();
        var createJson = JsonDocument.Parse(createContent);
        var productId = createJson.RootElement.GetProperty("id").GetGuid();

        // Act
        var response = await _client.DeleteAsync($"/api/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }
}