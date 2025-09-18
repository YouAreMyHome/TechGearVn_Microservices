using System.ComponentModel.DataAnnotations;

namespace Product.Api.Contracts.Products;

/// <summary>
/// API Contract cho CreateProduct request
/// Presentation Layer: HTTP/JSON input validation và business data
/// Clean Architecture: API layer DTOs tách biệt khỏi Application DTOs
/// </summary>
public class CreateProductRequest
{
    /// <summary>
    /// Tên sản phẩm
    /// Business validation: Required, max length sẽ được validate trong Domain
    /// </summary>
    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    [StringLength(200, ErrorMessage = "Tên sản phẩm không được quá 200 ký tự")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// SKU sản phẩm (optional)
    /// Business rule: Nếu không có sẽ auto-generate
    /// </summary>
    [StringLength(50, ErrorMessage = "SKU không được quá 50 ký tự")]
    public string? Sku { get; set; }

    /// <summary>
    /// Mô tả sản phẩm
    /// </summary>
    [StringLength(1000, ErrorMessage = "Mô tả không được quá 1000 ký tự")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Giá sản phẩm (VND, USD, EUR...)
    /// Business validation: Phải > 0
    /// </summary>
    [Required(ErrorMessage = "Giá sản phẩm không được để trống")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
    public decimal Price { get; set; }

    /// <summary>
    /// Đơn vị tiền tệ
    /// Business rule: VND, USD, EUR... (3 ký tự)
    /// </summary>
    [Required(ErrorMessage = "Đơn vị tiền tệ không được để trống")]
    [StringLength(3, MinimumLength = 3, ErrorMessage = "Đơn vị tiền tệ phải có 3 ký tự")]
    public string Currency { get; set; } = "VND";

    /// <summary>
    /// Số lượng tồn kho ban đầu
    /// Business rule: Phải >= 0
    /// </summary>
    [Required(ErrorMessage = "Số lượng tồn kho không được để trống")]
    [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho phải >= 0")]
    public int InitialStock { get; set; }

    /// <summary>
    /// ID của Category
    /// Business reference: Must be valid Category ID
    /// </summary>
    [Required(ErrorMessage = "Category ID không được để trống")]
    public Guid CategoryId { get; set; }
}