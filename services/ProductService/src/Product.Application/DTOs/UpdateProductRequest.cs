namespace Product.Application.DTOs;

/// <summary>
/// Request DTO để cập nhật thông tin sản phẩm
/// Chỉ cập nhật name và description, không cập nhật price/stock (có commands riêng)
/// </summary>
public record UpdateProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}