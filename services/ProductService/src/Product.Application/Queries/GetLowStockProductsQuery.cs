using MediatR;
using Product.Application.DTOs;

namespace Product.Application.Queries;

/// <summary>
/// Query để lấy danh sách sản phẩm sắp hết hàng
/// Critical cho inventory management và reorder process
/// </summary>
public record GetLowStockProductsQuery(int Threshold = 10) : IRequest<List<ProductDto>>;