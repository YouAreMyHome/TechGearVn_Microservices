using AutoMapper;
using FluentAssertions;
using Moq;
using Product.Application.DTOs;
using Product.Application.Queries;
using Product.Application.Queries.Handlers;
using Product.Domain.Repositories;
using Product.Domain.ValueObjects;
using Xunit;
using ProductEntity = Product.Domain.Entities.Product;

namespace Product.UnitTests.Application.Queries;

/// <summary>
/// Unit tests cho GetProductsQueryHandler
/// </summary>
public class GetProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly Mock<ICategoryRepository> _mockCategoryRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly GetProductsQueryHandler _handler;

    public GetProductsQueryHandlerTests()
    {
        _mockProductRepository = new Mock<IProductRepository>();
        _mockCategoryRepository = new Mock<ICategoryRepository>();
        _mockMapper = new Mock<IMapper>();
        _handler = new GetProductsQueryHandler(
            _mockProductRepository.Object,
            _mockCategoryRepository.Object,
            _mockMapper.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnPagedResult_WhenValidQuery()
    {
        // Arrange
        var query = new GetProductsQuery
        {
            Page = 1,
            PageSize = 10
        };

        var products = CreateSampleProducts();
        var productDtos = new List<ProductDto>
        {
            new ProductDto { Id = products[0].Id, Name = "Product 1" }
        };

        _mockProductRepository
            .Setup(x => x.GetProductsPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<List<Guid>?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, 1));

        _mockMapper.Setup(m => m.Map<List<ProductDto>>(products))
            .Returns(productDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Products.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task Handle_ShouldApplyFilters_WhenFiltersProvided()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var query = new GetProductsQuery
        {
            Page = 1,
            PageSize = 10,
            CategoryId = categoryId,
            MinPrice = 10m,
            MaxPrice = 100m,
            SearchTerm = "test",
            OnlyActive = true,
            SortBy = "price",
            SortDirection = "desc"
        };

        var products = CreateSampleProducts();
        var productDtos = new List<ProductDto>
        {
            new ProductDto { Id = products[0].Id, Name = "Product 1" }
        };

        _mockProductRepository
            .Setup(x => x.GetProductsPagedAsync(
                1, 10, It.IsAny<List<Guid>?>(), 10m, 100m, "test", true, "price", "desc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, 1));

        _mockMapper.Setup(m => m.Map<List<ProductDto>>(products))
            .Returns(productDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AppliedFilters.Should().ContainKey("categoryId");
        result.AppliedFilters.Should().ContainKey("minPrice");
        result.AppliedFilters.Should().ContainKey("maxPrice");
        result.AppliedFilters.Should().ContainKey("searchTerm");
        result.AppliedFilters["categoryId"].Should().Be(categoryId);
        result.AppliedFilters["minPrice"].Should().Be(10m);
        result.AppliedFilters["maxPrice"].Should().Be(100m);
        result.AppliedFilters["searchTerm"].Should().Be("test");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmptyResult_WhenNoProductsFound()
    {
        // Arrange
        var query = new GetProductsQuery { Page = 1, PageSize = 10 };
        var emptyProducts = new List<ProductEntity>();
        var emptyProductDtos = new List<ProductDto>();

        _mockProductRepository
            .Setup(x => x.GetProductsPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<List<Guid>?>(),
                It.IsAny<decimal?>(),
                It.IsAny<decimal?>(),
                It.IsAny<string?>(),
                It.IsAny<bool>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((emptyProducts, 0));

        _mockMapper.Setup(m => m.Map<List<ProductDto>>(emptyProducts))
            .Returns(emptyProductDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Products.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.HasNextPage.Should().BeFalse();
        result.HasPreviousPage.Should().BeFalse();
    }

    private List<ProductEntity> CreateSampleProducts()
    {
        return new List<ProductEntity>
        {
            ProductEntity.Create(
                "Sample Product 1",
                "PRD-20241201-0001",
                "Description 1",
                25.99m,
                "VND",
                100,
                Guid.NewGuid(),
                "user1"),
            ProductEntity.Create(
                "Sample Product 2",
                "PRD-20241201-0002",
                "Description 2",
                15.50m,
                "VND",
                50,
                Guid.NewGuid(),
                "user1"),
            ProductEntity.Create(
                "Sample Product 3",
                "PRD-20241201-0003",
                "Description 3",
                75.00m,
                "VND",
                25,
                Guid.NewGuid(),
                "user1")
        };
    }
}