using MediatR;
using Product.Application.DTOs;

namespace Product.Application.Queries;

/// <summary>
/// Query để lấy sản phẩm theo ID
/// Query = Request for data, không thay đổi state
/// Return ProductDto hoặc null nếu không tìm thấy
/// </summary>
public record GetProductByIdQuery(Guid ProductId) : IRequest<ProductDto?>;