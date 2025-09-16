using MediatR;
using Product.Domain.Exceptions;
using Product.Domain.Repositories;

namespace Product.Application.Commands.Handlers;

/// <summary>
/// Handler xử lý Command cập nhật tồn kho sản phẩm
/// Stock management quan trọng cho:
/// - Inventory accuracy
/// - Low stock alerts
/// - Out of stock handling
/// - Supply chain optimization
/// </summary>
public class UpdateProductStockCommandHandler : IRequestHandler<UpdateProductStockCommand>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductStockCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
    {
        // Lấy Product từ repository
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            throw new ProductNotFoundException(request.ProductId);

        // Gọi Domain method để update stock với business validation
        product.UpdateStock(
            newQuantity: request.NewQuantity,
            updatedBy: request.UpdatedBy,
            reason: request.Reason);

        // Lưu thay đổi và publish Domain Events
        await _productRepository.UpdateAsync(product, cancellationToken);

        // Domain Events có thể được publish:
        // - ProductStockChangedEvent (luôn)
        // - ProductLowStockEvent (nếu <= 10)
        // - ProductOutOfStockEvent (nếu = 0)
    }
}