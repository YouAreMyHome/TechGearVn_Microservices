namespace Product.Application.DTOs;

/// <summary>
/// DTO cho Product - dùng để trả về cho client
/// Chứa tất cả thông tin cần thiết nhưng ở dạng đơn giản (không phải Domain objects)
/// </summary>
public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Sku { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
    public Guid CategoryId { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public string? UpdatedBy { get; init; }
}