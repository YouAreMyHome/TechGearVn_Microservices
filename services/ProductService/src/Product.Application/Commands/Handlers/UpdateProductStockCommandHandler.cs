using MediatR;
using Product.Domain.Repositories;

namespace Product.Application.Commands.Handlers;

/// <summary>
/// Handler cho UpdateProductStockCommand
/// Application Layer: Orchestrate stock update business operation
/// Business logic: Low stock warnings, out of stock events
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
        // BƯỚC 1: Lấy Product từ Repository
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new ArgumentException($"Không tìm thấy Product với ID: {request.ProductId}");
        }

        // BƯỚC 2: Gọi Domain Method để update stock
        // Domain logic: Product.UpdateStock() sẽ validate business rules
        // Business rules: Non-negative stock, low stock warnings, out of stock events
        product.UpdateStock(
            newQuantity: request.NewQuantity,
            updatedBy: request.UpdatedBy,
            reason: request.Reason
        );

        // BƯỚC 3: Persist changes qua Repository
        // Infrastructure concern: Repository handle EF Core update và domain events
        await _productRepository.UpdateAsync(product, cancellationToken);

        // NOTE: Domain Events có thể include:
        // - ProductStockChangedEvent (always)
        // - ProductLowStockEvent (if quantity <= 10)
        // - ProductOutOfStockEvent (if quantity = 0)
    }
}