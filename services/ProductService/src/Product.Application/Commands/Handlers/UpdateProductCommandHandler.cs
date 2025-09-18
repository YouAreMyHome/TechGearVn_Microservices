using MediatR;
using Product.Domain.Exceptions;
using Product.Domain.Repositories;

namespace Product.Application.Commands.Handlers;

/// <summary>
/// Handler xử lý Command cập nhật thông tin cơ bản của sản phẩm
/// Chỉ update name và description, không touch price/stock (có commands riêng)
/// </summary>
public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductCommandHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        // Lấy Product từ repository
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            throw new ProductNotFoundException(request.ProductId);

        // Gọi Domain method để update với business validation
        product.UpdateDetails(
            name: request.Name ?? string.Empty,
            description: request.Description ?? string.Empty,
            updatedBy: request.UpdatedBy);

        // Lưu thay đổi (EF Core sẽ track changes)
        await _productRepository.UpdateAsync(product, cancellationToken);

        // Domain Events sẽ được publish tự động khi SaveChanges
    }
}