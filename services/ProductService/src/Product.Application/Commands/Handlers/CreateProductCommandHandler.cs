using MediatR;
using Product.Domain.Entities;
using Product.Domain.Repositories;
using Product.Domain.ValueObjects;

namespace Product.Application.Commands.Handlers;

/// <summary>
/// Handler cho CreateProductCommand
/// Application Layer: Orchestrate business operation, không chứa business logic
/// Clean Architecture: Gọi Domain factories và Repository interfaces
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
        // BƯỚC 1: Tạo Money Value Object từ primitive parameters
        // Domain concern: Business validation sẽ được handle trong Money.Create()
        var price = Money.Create(request.PriceAmount, request.Currency);

        // BƯỚC 2: Gọi Domain Factory Method để tạo Product
        // Domain logic: Product.Create() sẽ validate business rules và tạo domain events
        var product = Domain.Entities.Product.Create(
            name: request.Name,
            sku: request.Sku,
            description: request.Description,
            price: price,                    // Money Value Object, không phải primitives
            initialStock: request.InitialStock,
            categoryId: request.CategoryId,
            createdBy: request.CreatedBy
        );

        // BƯỚC 3: Persist Product qua Repository
        // Infrastructure concern: Repository sẽ handle EF Core mapping
        await _productRepository.AddAsync(product, cancellationToken);

        // BƯỚC 4: Return Product ID cho API layer
        // Application result: API layer sẽ dùng ID này trong response
        return product.Id;
    }
}