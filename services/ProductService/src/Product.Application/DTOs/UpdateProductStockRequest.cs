namespace Product.Application.DTOs;

/// <summary>
/// Request DTO để cập nhật tồn kho
/// Có thể là nhập hàng, xuất hàng, hoặc điều chỉnh inventory
/// </summary>
public record UpdateProductStockRequest
{
    public int NewQuantity { get; init; }
    public string Reason { get; init; } = "Manual Update";
}