using MediatR;
using Product.Application.DTOs;
using Product.Domain.Repositories;

namespace Product.Application.Queries.Handlers;

/// <summary>
/// Handler xử lý Query lấy danh sách sản phẩm sắp hết hàng
/// Critical cho inventory management:
/// - Trigger reorder process
/// - Notify procurement team
/// - Update dashboard alerts
/// </summary>
public class GetLowStockProductsQueryHandler : IRequestHandler<GetLowStockProductsQuery, List<ProductDto>>
{
    private readonly IProductRepository _productRepository;

    public GetLowStockProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<List<ProductDto>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
    {
        // Lấy danh sách products có low stock từ repository
        var products = await _productRepository.GetLowStockProductsAsync(request.Threshold, cancellationToken);

        // Map sang DTOs
        return products.Select(MapToDto).ToList();
    }

    private ProductDto MapToDto(Domain.Entities.Product product)
    {
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