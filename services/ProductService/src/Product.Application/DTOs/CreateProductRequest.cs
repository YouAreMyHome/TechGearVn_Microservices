namespace Product.Application.DTOs;

/// <summary>
/// Request DTO để tạo sản phẩm mới
/// Chứa tất cả thông tin cần thiết từ client
/// </summary>
public record CreateProductRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Sku { get; init; } // Nullable - sẽ auto-generate nếu không có
    public string Description { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Currency { get; init; } = "VND";
    public int InitialStock { get; init; }
    public Guid CategoryId { get; init; }
}