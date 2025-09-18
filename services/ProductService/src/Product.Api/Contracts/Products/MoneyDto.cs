namespace Product.Api.Contracts.Products;

/// <summary>
/// DTO cho Money value object trong API responses
/// Presentation Layer: Money representation cho JSON serialization
/// Business context: Hiển thị giá với currency thông tin
/// </summary>
public class MoneyDto
{
    /// <summary>
    /// Số tiền
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Đơn vị tiền tệ (VND, USD, EUR...)
    /// </summary>
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Formatted string cho display (optional)
    /// Business context: "1,000,000 VND", "$100.00"
    /// </summary>
    public string? FormattedValue { get; set; }
}