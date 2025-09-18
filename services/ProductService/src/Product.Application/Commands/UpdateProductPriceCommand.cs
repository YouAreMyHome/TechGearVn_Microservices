using MediatR;

namespace Product.Application.Commands;

/// <summary>
/// Command để cập nhật giá sản phẩm
/// Price change là critical business operation:
/// - Có business rules (max 50% change)
/// - Trigger Domain Events quan trọng
/// - Ảnh hưởng đến cart, pricing strategy
/// </summary>
public record UpdateProductPriceCommand : IRequest
{
    public Guid ProductId { get; init; }
    public decimal NewPrice { get; init; }
    public string Currency { get; init; } = "VND";
    public string UpdatedBy { get; init; } = string.Empty;
    public string? Reason { get; init; }
}