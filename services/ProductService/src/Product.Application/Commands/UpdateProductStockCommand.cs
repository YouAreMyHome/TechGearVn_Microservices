using MediatR;

namespace Product.Application.Commands;

/// <summary>
/// Command để cập nhật tồn kho sản phẩm
/// Stock management quan trọng cho:
/// - Inventory accuracy
/// - Low stock alerts
/// - Out of stock handling
/// - Supply chain optimization
/// </summary>
public record UpdateProductStockCommand(
    Guid ProductId,
    int NewQuantity,
    string Reason,
    string UpdatedBy) : IRequest;