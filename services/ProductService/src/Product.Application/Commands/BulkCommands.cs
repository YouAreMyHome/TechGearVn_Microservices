using MediatR;

namespace Product.Application.Commands;

/// <summary>
/// Command để bulk update prices
/// Business use case: Promotional pricing, bulk price adjustments
/// </summary>
public record BulkUpdateProductPricesCommand : IRequest<int>
{
    public List<ProductPriceUpdate> ProductPriceUpdates { get; init; } = new();
    public string UpdatedBy { get; init; } = string.Empty;
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Individual product price update trong bulk operation
/// </summary>
public record ProductPriceUpdate
{
    public Guid ProductId { get; init; }
    public decimal NewPrice { get; init; }
    public string Currency { get; init; } = "VND";
}