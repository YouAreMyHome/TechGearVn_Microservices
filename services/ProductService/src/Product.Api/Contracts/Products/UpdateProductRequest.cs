using System.ComponentModel.DataAnnotations;

namespace Product.Api.Contracts.Products;

/// <summary>
/// API Contract cho UpdateProduct request (general update)
/// Presentation Layer: General product information update HTTP input
/// Business context: Update product details without affecting price/stock
/// </summary>
public class UpdateProductRequest
{
    /// <summary>
    /// Tên mới của Product
    /// </summary>
    [StringLength(200, ErrorMessage = "Tên sản phẩm không được quá 200 ký tự")]
    public string? Name { get; set; }

    /// <summary>
    /// Mô tả mới của Product
    /// </summary>
    [StringLength(1000, ErrorMessage = "Mô tả không được quá 1000 ký tự")]
    public string? Description { get; set; }

    /// <summary>
    /// Category mới của Product
    /// </summary>
    [StringLength(100, ErrorMessage = "Category không được quá 100 ký tự")]
    public string? Category { get; set; }

    /// <summary>
    /// Lý do thay đổi (optional)
    /// </summary>
    [StringLength(500, ErrorMessage = "Lý do không được quá 500 ký tự")]
    public string? Reason { get; set; }
}

/// <summary>
/// API Contract cho UpdateProductPrice request
/// Presentation Layer: Price update HTTP input với validation
/// Business context: Support promotional pricing, cost adjustments
/// </summary>
public class UpdateProductPriceRequest
{
    /// <summary>
    /// Giá mới
    /// Business validation: Phải > 0, change limits sẽ validate trong Domain
    /// </summary>
    [Required(ErrorMessage = "Giá mới không được để trống")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Giá mới phải lớn hơn 0")]
    public decimal NewPrice { get; set; }

    /// <summary>
    /// Đơn vị tiền tệ mới (optional - default giữ nguyên currency hiện tại)
    /// Business rule: Currency change policy tùy business requirement
    /// </summary>
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Đơn vị tiền tệ phải có 3 ký tự")]
    public string? Currency { get; set; }

    /// <summary>
    /// Lý do thay đổi giá (optional)
    /// Business context: Promotional, Market adjustment, Cost increase...
    /// </summary>
    [StringLength(500, ErrorMessage = "Lý do không được quá 500 ký tự")]
    public string? Reason { get; set; }
}