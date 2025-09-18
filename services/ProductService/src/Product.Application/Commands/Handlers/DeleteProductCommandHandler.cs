using MediatR;
using Product.Domain.Repositories;

namespace Product.Application.Commands.Handlers;

/// <summary>
/// Handler cho DeleteProductCommand
/// Business orchestration: Coordinate delete operation với business rules
/// Integration: Domain Events sẽ được process qua Infrastructure layer
/// </summary>
public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IProductRepository _productRepository;

    public DeleteProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Business validation: Input parameters
        if (request.ProductId == Guid.Empty)
            throw new ArgumentException("ProductId không được để trống", nameof(request.ProductId));

        if (string.IsNullOrWhiteSpace(request.DeletedBy))
            throw new ArgumentException("DeletedBy không được để trống", nameof(request.DeletedBy));

        // 2. Load Aggregate từ Repository
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product == null)
            throw new KeyNotFoundException($"Không tìm thấy sản phẩm có ID: {request.ProductId}");

        // 3. Business logic: Có thể thêm complex business rules ở đây
        // Ví dụ: Check pending orders, inventory dependencies, etc.
        // await _orderRepository.HasPendingOrdersAsync(product.Id);

        // 4. Domain operation: Gọi business method trên Aggregate
        product.MarkAsDeleted(
            deletedBy: request.DeletedBy,
            reason: "Deleted via API request"); // Có thể extend Command để nhận reason

        // 5. Persist changes: Repository sẽ handle Domain Events qua Outbox Pattern
        await _productRepository.UpdateAsync(product, cancellationToken);

        // Note: Domain Events (ProductDeletedEvent) sẽ được Infrastructure convert thành
        // Outbox Messages và publish qua Message Bus để notify other microservices
    }
}