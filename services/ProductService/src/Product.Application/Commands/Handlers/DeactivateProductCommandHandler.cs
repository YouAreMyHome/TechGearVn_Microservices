using MediatR;
using Product.Domain.Exceptions;
using Product.Domain.Repositories;

namespace Product.Application.Commands.Handlers;

/// <summary>
/// Handler xử lý Command vô hiệu hóa sản phẩm (soft delete)
/// Business impacts:
/// - Hide khỏi catalog
/// - Ngừng nhận orders
/// - Notify customers có product trong cart
/// - Update search indexes
/// </summary>
public class DeactivateProductCommandHandler : IRequestHandler<DeactivateProductCommand>
{
    private readonly IProductRepository _productRepository;

    public DeactivateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task Handle(DeactivateProductCommand request, CancellationToken cancellationToken)
    {
        // Lấy Product từ repository
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            throw new ProductNotFoundException(request.ProductId);

        // Gọi Domain method để deactivate
        product.Deactivate(
            updatedBy: request.DeactivatedBy,
            reason: request.Reason);

        // Lưu thay đổi và publish Domain Events
        await _productRepository.UpdateAsync(product, cancellationToken);

        // Domain Event: ProductDeactivatedEvent sẽ được publish
        // Infrastructure sẽ handle việc:
        // - Update search indexes
        // - Notify external systems
        // - Cancel pending orders
    }
}