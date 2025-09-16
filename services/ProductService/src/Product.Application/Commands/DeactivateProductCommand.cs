using MediatR;

namespace Product.Application.Commands;

/// <summary>
/// Command để vô hiệu hóa sản phẩm (soft delete)
/// Business impacts:
/// - Hide khỏi catalog
/// - Ngừng nhận orders
/// - Notify customers có product trong cart
/// - Update search indexes
/// </summary>
public record DeactivateProductCommand(
    Guid ProductId,
    string Reason,
    string DeactivatedBy) : IRequest;