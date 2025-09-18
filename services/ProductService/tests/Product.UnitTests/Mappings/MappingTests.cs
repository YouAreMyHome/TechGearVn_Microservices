using AutoMapper;
using FluentAssertions;
using Product.Api.Contracts.Products;
using Product.Api.Mappings;
using Product.Application.Commands;
using Product.Application.DTOs;
using Product.Application.Mappings;
using Product.Application.Queries;
using Product.Domain.Entities;
using Product.Domain.ValueObjects;
using Xunit;

namespace Product.UnitTests.Mappings;

/// <summary>
/// Tests for AutoMapper mappings between layers
/// Verifies that mapping between Domain → Application → API works correctly
/// Critical for ensuring data integrity across layer boundaries
/// </summary>
public class MappingTests
{
    private readonly IMapper _mapper;

    public MappingTests()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<ApplicationMappingProfile>();
            cfg.AddProfile<ApiMappingProfile>();
        });

        _mapper = configuration.CreateMapper();

        // Verify configuration is valid
        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void AutoMapper_Configuration_ShouldBeValid()
    {
        // The configuration validation in constructor should pass
        // If we reach here, mapping configuration is valid
        Assert.True(true);
    }

    [Fact]
    public void ProductEntity_To_ProductDto_ShouldMapCorrectly()
    {
        // Arrange
        var productEntity = CreateTestProduct();

        // Act
        var productDto = _mapper.Map<ProductDto>(productEntity);

        // Assert
        productDto.Should().NotBeNull();
        productDto.Id.Should().Be(productEntity.Id);
        productDto.Name.Should().Be(productEntity.Name.Value);
        productDto.Sku.Should().Be(productEntity.Sku.Value);
        productDto.Description.Should().Be(productEntity.Description);
        productDto.Price.Should().Be(productEntity.Price.Amount);
        productDto.Currency.Should().Be(productEntity.Price.Currency);
        productDto.StockQuantity.Should().Be(productEntity.StockQuantity);
        productDto.CategoryId.Should().Be(productEntity.CategoryId);
        productDto.IsActive.Should().Be(productEntity.IsActive);
        productDto.CreatedAt.Should().Be(productEntity.CreatedAt);
        productDto.CreatedBy.Should().Be(productEntity.CreatedBy);
        productDto.UpdatedAt.Should().Be(productEntity.UpdatedAt);
        productDto.UpdatedBy.Should().Be(productEntity.UpdatedBy);
    }

    [Fact]
    public void ProductDto_To_ProductResponse_ShouldMapCorrectly()
    {
        // Arrange
        var productDto = CreateTestProductDto();

        // Act
        var productResponse = _mapper.Map<ProductResponse>(productDto);

        // Assert
        productResponse.Should().NotBeNull();
        productResponse.Id.Should().Be(productDto.Id);
        productResponse.Name.Should().Be(productDto.Name);
        productResponse.Sku.Should().Be(productDto.Sku);
        productResponse.Description.Should().Be(productDto.Description);
        productResponse.Price.Should().Be(productDto.Price);
        productResponse.Currency.Should().Be(productDto.Currency);
        productResponse.InStock.Should().Be(productDto.StockQuantity > 0);
        productResponse.CreatedBy.Should().Be(productDto.CreatedBy);
        productResponse.UpdatedBy.Should().Be(productDto.UpdatedBy);
        productResponse.StockQuantity.Should().Be(productDto.StockQuantity);
        productResponse.CategoryId.Should().Be(productDto.CategoryId);
        productResponse.IsActive.Should().Be(productDto.IsActive);
        productResponse.CreatedAt.Should().Be(productDto.CreatedAt);
        productResponse.UpdatedAt.Should().Be(productDto.UpdatedAt);
    }

    [Fact]
    public void CreateProductRequest_To_CreateProductCommand_ShouldMapCorrectly()
    {
        // Arrange
        var request = new CreateProductRequest
        {
            Name = "Test Product",
            Sku = "TEST-001",
            Description = "Test Description",
            Price = 99.99m,
            Currency = "USD",
            CategoryId = Guid.NewGuid(),
            InitialStock = 10
        };

        // Act
        var command = _mapper.Map<CreateProductCommand>(request);

        // Assert
        command.Should().NotBeNull();
        command.Name.Should().Be(request.Name);
        command.Sku.Should().Be(request.Sku);
        command.Description.Should().Be(request.Description);
        command.PriceAmount.Should().Be(request.Price);
        command.Currency.Should().Be(request.Currency);
        command.CategoryId.Should().Be(request.CategoryId);
        command.InitialStock.Should().Be(request.InitialStock);
    }

    [Fact]
    public void GetProductsRequest_To_GetProductsQuery_ShouldMapCorrectly()
    {
        // Arrange
        var request = new GetProductsRequest
        {
            Page = 2,
            PageSize = 20,
            CategoryId = Guid.NewGuid(),
            IncludeSubCategories = true,
            SearchTerm = "test",
            MinPrice = 10.0m,
            MaxPrice = 100.0m,
            OnlyActive = true,
            SortBy = "Name",
            SortDirection = "ASC"
        };

        // Act
        var query = _mapper.Map<GetProductsQuery>(request);

        // Assert
        query.Should().NotBeNull();
        query.Page.Should().Be(request.Page);
        query.PageSize.Should().Be(request.PageSize);
        query.CategoryId.Should().Be(request.CategoryId);
        query.IncludeSubCategories.Should().Be(request.IncludeSubCategories);
        query.SearchTerm.Should().Be(request.SearchTerm);
        query.MinPrice.Should().Be(request.MinPrice);
        query.MaxPrice.Should().Be(request.MaxPrice);
        query.OnlyActive.Should().Be(request.OnlyActive);
        query.SortBy.Should().Be(request.SortBy);
        query.SortDirection.Should().Be(request.SortDirection);
    }

    [Fact]
    public void PagedProductResult_To_PagedProductResponse_ShouldMapCorrectly()
    {
        // Arrange
        var productDtos = new List<ProductDto> { CreateTestProductDto() };
        var appliedFilters = new Dictionary<string, object>
        {
            ["CategoryId"] = Guid.NewGuid(),
            ["SearchTerm"] = "test"
        };

        var filterMetadata = new Product.Application.DTOs.ProductFilterMetadata
        {
            CategoryId = Guid.NewGuid(),
            SearchTerm = "test",
            OnlyActive = true,
            SortBy = "name",
            SortDirection = "asc"
        };

        var pagedResult = new PagedProductResult
        {
            Products = productDtos,
            TotalCount = 1,
            Page = 1,
            PageSize = 10,
            TotalPages = 1,
            HasNextPage = false,
            HasPreviousPage = false,
            FilterMetadata = filterMetadata,
            AppliedFilters = appliedFilters
        };

        // Act
        var response = _mapper.Map<PagedProductResponse>(pagedResult);

        // Assert
        response.Should().NotBeNull();
        response.Products.Should().HaveCount(1);
        response.TotalCount.Should().Be(pagedResult.TotalCount);
        response.Page.Should().Be(pagedResult.Page);
        response.PageSize.Should().Be(pagedResult.PageSize);
        response.TotalPages.Should().Be(pagedResult.TotalPages);
        response.HasNextPage.Should().Be(pagedResult.HasNextPage);
        response.HasPreviousPage.Should().Be(pagedResult.HasPreviousPage);
        response.FilterMetadata.Should().NotBeNull();
        response.FilterMetadata.Should().BeEquivalentTo(pagedResult.FilterMetadata);
    }

    [Fact]
    public void Full_Pipeline_Mapping_Domain_To_API_ShouldWork()
    {
        // Arrange - Domain Entity
        var productEntity = CreateTestProduct();

        // Act - Map through all layers: Domain → Application → API
        var productDto = _mapper.Map<ProductDto>(productEntity);
        var productResponse = _mapper.Map<ProductResponse>(productDto);

        // Assert - End-to-end mapping verification
        productResponse.Should().NotBeNull();
        productResponse.Id.Should().Be(productEntity.Id);
        productResponse.Name.Should().Be(productEntity.Name.Value);
        productResponse.Sku.Should().Be(productEntity.Sku.Value);
        productResponse.Description.Should().Be(productEntity.Description);
        productResponse.Price.Should().Be(productEntity.Price.Amount);
        productResponse.Currency.Should().Be(productEntity.Price.Currency);
        productResponse.InStock.Should().Be(productEntity.StockQuantity > 0);
        productResponse.CreatedBy.Should().Be(productEntity.CreatedBy);
        productResponse.UpdatedBy.Should().Be(productEntity.UpdatedBy);
        productResponse.StockQuantity.Should().Be(productEntity.StockQuantity);
        productResponse.CategoryId.Should().Be(productEntity.CategoryId);
        productResponse.IsActive.Should().Be(productEntity.IsActive);
        productResponse.CreatedAt.Should().Be(productEntity.CreatedAt);
        productResponse.UpdatedAt.Should().Be(productEntity.UpdatedAt);
    }

    // Helper methods to create test data
    private static global::Product.Domain.Entities.Product CreateTestProduct()
    {
        return global::Product.Domain.Entities.Product.Create(
            name: "Test Product",
            sku: "PRD-20240101-0001", // Correct SKU format
            description: "Test Description",
            priceAmount: 99.99m,
            currency: "USD",
            initialStock: 10,
            categoryId: Guid.NewGuid(),
            createdBy: "test-user"
        );
    }

    private static ProductDto CreateTestProductDto()
    {
        return new ProductDto
        {
            Id = Guid.NewGuid(),
            Name = "Test Product",
            Sku = "TEST-001",
            Description = "Test Description",
            Price = 99.99m,
            Currency = "USD",
            StockQuantity = 10,
            CategoryId = Guid.NewGuid(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };
    }
}