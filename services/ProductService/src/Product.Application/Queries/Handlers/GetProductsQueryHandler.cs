using MediatR;
using Product.Application.DTOs;
using Product.Domain.Repositories;

namespace Product.Application.Queries.Handlers;

/// <summary>
/// Handler xử lý Query lấy danh sách sản phẩm với pagination và filtering
/// Support các filters: category, price range, active status, search term
/// Critical cho catalog browsing và admin management
/// </summary>
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, ProductListDto>
{
    private readonly IProductRepository _productRepository;

    public GetProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductListDto> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Tính toán pagination
        var skip = (request.Page - 1) * request.PageSize;

        // Lấy danh sách products từ repository với filters
        var products = await GetFilteredProductsAsync(request, cancellationToken);

        // Đếm tổng số records (cho pagination)
        var totalCount = await GetTotalCountAsync(request, cancellationToken);

        // Map sang DTOs (sẽ optimize với AutoMapper)
        var productDtos = products.Select(MapToDto).ToList();

        return new ProductListDto
        {
            Products = productDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    private async Task<List<Domain.Entities.Product>> GetFilteredProductsAsync(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Logic filtering phức tạp - tạm thời return all (sẽ implement trong Infrastructure)
        var products = await _productRepository.GetAllAsync(cancellationToken);

        // Apply filters in memory (sẽ optimize với EF Core queries sau)
        if (request.CategoryId.HasValue)
            products = products.Where(p => p.CategoryId == request.CategoryId.Value).ToList();

        if (request.MinPrice.HasValue)
            products = products.Where(p => p.Price.Amount >= request.MinPrice.Value).ToList();

        if (request.MaxPrice.HasValue)
            products = products.Where(p => p.Price.Amount <= request.MaxPrice.Value).ToList();

        if (request.IsActive.HasValue)
            products = products.Where(p => p.IsActive == request.IsActive.Value).ToList();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            products = products.Where(p =>
                p.Name.Value.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)).ToList();

        // Apply pagination
        var skip = (request.Page - 1) * request.PageSize;
        return products.Skip(skip).Take(request.PageSize).ToList();
    }

    private async Task<int> GetTotalCountAsync(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Tạm thời return count của filtered list (sẽ optimize sau)
        var products = await GetFilteredProductsAsync(request, cancellationToken);
        return products.Count;
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