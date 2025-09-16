using MediatR;
using Product.Domain.Entities;
using Product.Domain.Exceptions;
using Product.Domain.Repositories;
using ProductEntity = Product.Domain.Entities.Product; // Type alias để tránh conflict

namespace Product.Application.Commands.Handlers;

/// <summary>
/// Handler xử lý Command tạo sản phẩm mới
/// Orchestrate business logic theo Clean Architecture:
/// 1. Validate business rules
/// 2. Gọi Domain Factory method 
/// 3. Persist thông qua Repository interface
/// 4. Return ProductId cho client
/// </summary>
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;

    public CreateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Business rule: Kiểm tra SKU unique trước khi tạo
        if (!string.IsNullOrWhiteSpace(request.Sku))
        {
            var isSkuUnique = await _productRepository.IsSkuUniqueAsync(
                request.Sku,
                null, // excludeProductId = null vì đang tạo mới
                cancellationToken);

            if (!isSkuUnique)
                throw new InvalidOperationException($"SKU '{request.Sku}' đã tồn tại trong hệ thống");
        }

        // Gọi Domain Factory method để tạo Product với validation đầy đủ
        // Sử dụng type alias để tránh namespace conflict
        var product = ProductEntity.Create(
            name: request.Name,
            sku: request.Sku,
            description: request.Description,
            price: request.Price,
            currency: request.Currency,
            initialStock: request.InitialStock,
            categoryId: request.CategoryId,
            createdBy: request.CreatedBy);

        // Persist thông qua Repository (tuân thủ Dependency Rule)
        // Infrastructure sẽ handle EF Core SaveChanges và Domain Events
        await _productRepository.AddAsync(product, cancellationToken);

        // Return ProductId cho client
        return product.Id;
    }
}