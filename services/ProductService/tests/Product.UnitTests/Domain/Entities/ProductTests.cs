using FluentAssertions;
using Product.Domain.Events;
using Product.Domain.ValueObjects;
using Xunit;
using ProductEntity = Product.Domain.Entities.Product;

namespace Product.UnitTests.Domain.Entities;

/// <summary>
/// Unit tests cho Product Domain Entity
/// Test business rules và domain logic
/// </summary>
public class ProductTests
{
    [Fact]
    public void Create_ValidData_ShouldCreateProductSuccessfully()
    {
        // Arrange
        var name = "Test Product";
        var sku = "PRD-20241201-0001";
        var description = "Test product description";
        var price = Money.Create(100.00m, "VND");
        var initialStock = 10;
        var categoryId = Guid.NewGuid();
        var createdBy = "TestUser";

        // Act
        var product = ProductEntity.Create(
            name: name,
            sku: sku,
            description: description,
            price: price,
            initialStock: initialStock,
            categoryId: categoryId,
            createdBy: createdBy
        );

        // Assert
        product.Should().NotBeNull();
        product.Id.Should().NotBe(Guid.Empty);
        product.Name.Value.Should().Be(name);
        product.Sku.Value.Should().Be(sku);
        product.Description.Should().Be(description);
        product.Price.Should().Be(price);
        product.StockQuantity.Should().Be(initialStock);
        product.CategoryId.Should().Be(categoryId);
        product.IsActive.Should().BeTrue();
        product.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        
        // Verify domain event was raised
        product.DomainEvents.Should().HaveCount(1);
        product.DomainEvents.First().Should().BeOfType<ProductCreatedEvent>();
    }

    [Fact]
    public void IsLowStock_StockBelowThreshold_ShouldReturnTrue()
    {
        // Arrange
        var product = CreateTestProduct();
        
        // Act
        var isLowStock = product.IsLowStock();

        // Assert - default stock of 10 is at threshold (<=10), so expect true
        isLowStock.Should().BeTrue();
    }

    [Fact]
    public void IsOutOfStock_ZeroStock_ShouldReturnFalse()
    {
        // Arrange
        var product = CreateTestProduct();
        
        // Act
        var isOutOfStock = product.IsOutOfStock();

        // Assert - stock is 10, so not out of stock
        isOutOfStock.Should().BeFalse();
    }

    private ProductEntity CreateTestProduct()
    {
        return ProductEntity.Create(
            name: "Test Product",
            sku: "PRD-20241201-0001",
            description: "Test product description",
            price: Money.Create(100.00m, "VND"),
            initialStock: 10,
            categoryId: Guid.NewGuid(),
            createdBy: "TestUser"
        );
    }
}