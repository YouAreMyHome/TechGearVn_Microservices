using MediatR;
using Product.Application.DTOs;

namespace Product.Application.Queries;

/// <summary>
/// Query để lấy sản phẩm theo SKU
/// SKU là business identifier quan trọng cho inventory management
/// </summary>
public record GetProductBySkuQuery(string Sku) : IRequest<ProductDto?>;