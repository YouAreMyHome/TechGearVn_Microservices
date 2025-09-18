using FluentAssertions;
using Moq;
using Product.Application.Commands;
using Product.Application.Commands.Handlers;
using Product.Domain.Repositories;
using Xunit;
using ProductEntity = Product.Domain.Entities.Product;

namespace Product.UnitTests.Application.Commands;

/// <summary>
/// Unit tests cho CreateProductCommandHandler
/// Test business logic và validation cho việc tạo sản phẩm mới
/// </summary>
public class CreateProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _mockProductRepository;
    private readonly CreateProductCommandHandler _handler;

    public CreateProductCommandHandlerTests()
    {
        _mockProductRepository = new Mock<IProductRepository>();
        _handler = new CreateProductCommandHandler(_mockProductRepository.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldCreateProductSuccessfully()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Sku = "PRD-20241201-0001",
            Description = "Test description",
            PriceAmount = 100.00m,
            Currency = "VND",
            InitialStock = 10,
            CategoryId = categoryId,
            CreatedBy = "TestUser"
        };

        _mockProductRepository
            .Setup(x => x.AddAsync(It.IsAny<ProductEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductEntity product, CancellationToken _) => product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);

        _mockProductRepository.Verify(
            x => x.AddAsync(It.Is<ProductEntity>(p =>
                p.Name.Value == command.Name &&
                p.Description == command.Description &&
                p.Price.Amount == command.PriceAmount &&
                p.Price.Currency == command.Currency &&
                p.StockQuantity == command.InitialStock &&
                p.CategoryId == command.CategoryId
            ), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_ValidCommandWithNullSku_ShouldGenerateSkuAutomatically()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var command = new CreateProductCommand
        {
            Name = "Test Product",
            Sku = null, // Should auto-generate
            Description = "Test description",
            PriceAmount = 100.00m,
            Currency = "VND",
            InitialStock = 10,
            CategoryId = categoryId,
            CreatedBy = "TestUser"
        };

        _mockProductRepository
            .Setup(x => x.AddAsync(It.IsAny<ProductEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductEntity product, CancellationToken _) => product);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBe(Guid.Empty);

        _mockProductRepository.Verify(
            x => x.AddAsync(It.Is<ProductEntity>(p =>
                p.Name.Value == command.Name &&
                !string.IsNullOrEmpty(p.Sku.Value) // SKU should be auto-generated
            ), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}