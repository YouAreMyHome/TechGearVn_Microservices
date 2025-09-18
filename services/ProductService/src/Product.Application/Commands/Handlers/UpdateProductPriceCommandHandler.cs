using MediatR;
using Product.Domain.Repositories;
using Product.Domain.ValueObjects;

namespace Product.Application.Commands.Handlers;

/// <summary>
/// Handler cho UpdateProductPriceCommand
/// Application Layer: Orchestrate price update business operation
/// Business logic: Delegate validation về Domain layer
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
        // BƯỚC 1: Lấy Product từ Repository
        // Domain query: Find product by business ID
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new ArgumentException($"Không tìm thấy Product với ID: {request.ProductId}");
        }

        // BƯỚC 2: Tạo Money Value Object từ primitive parameters
        // Domain concern: Money.Create() sẽ validate currency format và amount
        var newPrice = Money.Create(request.NewPrice, "VND"); // Default currency

        // BƯỚC 3: Gọi Domain Method để update price
        // Domain logic: Product.UpdatePrice() sẽ validate business rules và raise domain events
        // Business rules: Price change limits, currency consistency, active product check
        product.UpdatePrice(newPrice, request.UpdatedBy);

        // BƯỚC 4: Persist changes qua Repository
        // Infrastructure concern: Repository handle EF Core update và domain events
        await _productRepository.UpdateAsync(product, cancellationToken);

        // NOTE: Domain Events sẽ được publish bởi Infrastructure layer
        // Integration: Other services sẽ receive ProductPriceChangedEvent
    }
}