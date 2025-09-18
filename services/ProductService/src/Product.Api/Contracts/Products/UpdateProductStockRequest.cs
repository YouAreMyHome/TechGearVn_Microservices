using System.ComponentModel.DataAnnotations;

namespace Product.Api.Contracts.Products;

/// <summary>
/// API Contract cho UpdateProductStock request
/// Presentation Layer: Stock update HTTP input với validation
/// Business context: Inventory management, stock adjustments
/// </summary>
public class UpdateProductStockRequest
{
    /// <summary>
    /// Số lượng stock mới
    /// Business validation: Phải >= 0, stock rules sẽ validate trong Domain
    /// </summary>
    [Required(ErrorMessage = "Số lượng stock không được để trống")]
    [Range(0, int.MaxValue, ErrorMessage = "Số lượng stock phải >= 0")]
    public int NewStock { get; set; }

    /// <summary>
    /// Lý do thay đổi stock (optional)
    /// Business context: Restock, damage, correction, sale...
    /// </summary>
    [StringLength(500, ErrorMessage = "Lý do không được quá 500 ký tự")]
    public string? Reason { get; set; }
}