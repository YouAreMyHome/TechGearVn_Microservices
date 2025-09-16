using AutoMapper;
using MediatR;
using Product.Application.DTOs;
using Product.Domain.Repositories;

namespace Product.Application.Queries.Handlers;

/// <summary>
/// Handler xử lý Query lấy sản phẩm theo ID
/// Sử dụng AutoMapper để mapping Domain Entity → DTO
/// Clean và maintainable hơn manual mapping
/// </summary>
public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetProductByIdQueryHandler(
        IProductRepository productRepository,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        // Lấy Product từ Domain repository
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product is null)
            return null;

        // AutoMapper tự động map Domain Entity → DTO
        return _mapper.Map<ProductDto>(product);
    }
}