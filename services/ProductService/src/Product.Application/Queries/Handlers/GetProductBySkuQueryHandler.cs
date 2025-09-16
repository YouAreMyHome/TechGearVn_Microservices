using MediatR;
using Product.Application.DTOs;
using Product.Domain.Repositories;

namespace Product.Application.Queries.Handlers;

/// <summary>
/// Handler xử lý Query lấy sản phẩm theo SKU
/// SKU là business identifier quan trọng cho inventory
/// Thường dùng trong barcode scanning, inventory management
/// </summary>
public class GetProductBySkuQueryHandler : IRequestHandler<GetProductBySkuQuery, ProductDto?>
{
    private readonly IProductRepository _productRepository;

    public GetProductBySkuQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductDto?> Handle(GetProductBySkuQuery request, CancellationToken cancellationToken)
    {
        // Lấy Product theo SKU từ repository
        var product = await _productRepository.GetBySkuAsync(request.Sku, cancellationToken);
        if (product is null)
            return null;

        // Map sang DTO
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name.Value,
            Sku = product.Sku.Value,
            Description = product.Description,
            Price = product.Price.Amount,
            Currency = product.Price.Currency,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            CreatedBy = product.CreatedBy,
            UpdatedBy = product.UpdatedBy
        };
    }
}