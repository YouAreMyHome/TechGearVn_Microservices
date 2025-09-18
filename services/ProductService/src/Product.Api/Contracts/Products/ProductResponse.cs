namespace Product.Api.Contracts.Products;

/// <summary>
/// Response DTO cho Product
/// API Layer DTO: Format phù hợp cho JSON serialization và client consumption
/// Bao gồm computed properties để client dễ sử dụng
/// </summary>
public class ProductResponse
{
    /// <summary>
    /// ID sản phẩm
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Tên sản phẩm
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// SKU sản phẩm (mã định danh business)
    /// </summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả sản phẩm
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Giá sản phẩm (số tiền)
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Mã tiền tệ
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Giá hiển thị (formatted cho UI)
    /// Ví dụ: "29.990.000 VND"
    /// </summary>
    public string DisplayPrice { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng tồn kho hiện tại
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// Trạng thái còn hàng
    /// </summary>
    public bool InStock { get; set; }

    /// <summary>
    /// Cảnh báo tồn kho thấp
    /// </summary>
    public bool LowStock { get; set; }

    /// <summary>
    /// ID danh mục
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Trạng thái active (có thể bán)
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Thời gian cập nhật cuối
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Người tạo
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Người cập nhật cuối
    /// </summary>
    public string? UpdatedBy { get; set; }
}