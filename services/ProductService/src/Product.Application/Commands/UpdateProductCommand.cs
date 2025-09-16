using MediatR;

namespace Product.Application.Commands;

/// <summary>
/// Command để cập nhật thông tin cơ bản của sản phẩm
/// Chỉ update name và description, không touch price/stock (có commands riêng)
/// </summary>
public record UpdateProductCommand(
    Guid ProductId,
    string Name,
    string Description,
    string UpdatedBy) : IRequest;