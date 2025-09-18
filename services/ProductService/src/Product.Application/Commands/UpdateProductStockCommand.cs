using MediatR;

namespace Product.Application.Commands;

/// <summary>
/// Command để update tồn kho Product
/// Business use case: Inventory adjustment, stock receiving, stock allocation
/// CQRS Pattern: Dedicated command cho stock management operations
/// </summary>
public record UpdateProductStockCommand : IRequest
{
    /// <summary>
    /// ID của Product cần update stock
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// Số lượng tồn kho mới
    /// Business rule: Phải >= 0, không oversell
    /// </summary>
    public int NewQuantity { get; init; }

    /// <summary>
    /// Lý do thay đổi tồn kho
    /// Business context: Manual Update, Sale, Receiving, Adjustment, Return...
    /// </summary>
    public string Reason { get; init; } = "Manual Update";

    /// <summary>
    /// Người thực hiện update
    /// Audit requirement: Track stock changes
    /// </summary>
    public string UpdatedBy { get; init; } = string.Empty;
}