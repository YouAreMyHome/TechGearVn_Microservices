namespace Product.Application.DTOs;

/// <summary>
/// Request DTO để cập nhật giá sản phẩm
/// Riêng biệt vì thay đổi giá là business operation quan trọng
/// </summary>
public record UpdateProductPriceRequest
{
    public decimal NewPrice { get; init; }
}