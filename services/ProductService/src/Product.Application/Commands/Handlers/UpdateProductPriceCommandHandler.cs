using MediatR;
using Product.Domain.Exceptions;
using Product.Domain.Repositories;

namespace Product.Application.Commands.Handlers;

/// <summary>
/// Handler xử lý Command cập nhật giá sản phẩm
/// Price change là critical business operation:
/// - Có business rules (max 50% change)
/// - Trigger Domain Events quan trọng
/// - Ảnh hưởng đến cart, pricing strategy
/// </summary>
public class UpdateProductPriceCommandHandler : IRequestHandler<UpdateProductPriceCommand>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductPriceCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task Handle(UpdateProductPriceCommand request, CancellationToken cancellationToken)
    {
        // Lấy Product từ repository
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            throw new ProductNotFoundException(request.ProductId);

        // Gọi Domain method để update price với business validation
        // Domain sẽ throw exception nếu vi phạm rules (e.g., >50% change)
        product.UpdatePrice(
            newPrice: request.NewPrice,
            updatedBy: request.UpdatedBy);

        // Lưu thay đổi và publish Domain Events
        await _productRepository.UpdateAsync(product, cancellationToken);

        // Domain Events: ProductPriceChangedEvent sẽ được publish
        // Infrastructure layer sẽ handle việc notify các bounded contexts khác
    }
}