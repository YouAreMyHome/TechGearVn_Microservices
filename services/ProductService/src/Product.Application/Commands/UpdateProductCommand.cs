using MediatR;

namespace Product.Application.Commands;

/// <summary>
/// Command để update thông tin tổng quát của Product
/// Business use case: Update product information (name, description, category)
/// CQRS Pattern: General update command cho non-critical product updates
/// </summary>
public record UpdateProductCommand : IRequest
{
    /// <summary>
    /// ID của Product cần update
    /// </summary>
    public Guid ProductId { get; init; }

    /// <summary>
    /// Tên mới của Product
    /// Business rule: Must be unique and follow naming conventions
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Mô tả mới của Product
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Category mới của Product
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Người thực hiện update
    /// Audit requirement: Track who made the changes
    /// </summary>
    public string UpdatedBy { get; init; } = string.Empty;

    /// <summary>
    /// Lý do thay đổi (optional)
    /// Business context: Product information correction, feature update...
    /// </summary>
    public string? Reason { get; init; }
}