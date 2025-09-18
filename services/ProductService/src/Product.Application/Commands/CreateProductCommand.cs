using MediatR;

namespace Product.Application.Commands;

/// <summary>
/// Command để tạo Product mới
/// Business use case: Admin/Staff tạo product trong catalog
/// CQRS Pattern: Command chứa tất cả data cần thiết cho business operation
/// </summary>
public record CreateProductCommand : IRequest<Guid>
{
    /// <summary>
    /// Tên sản phẩm
    /// Business validation: Sẽ được validate trong ProductName Value Object
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// SKU sản phẩm (optional - sẽ auto-generate nếu null)
    /// Business identifier: Unique business code cho product
    /// </summary>
    public string? Sku { get; init; }

    /// <summary>
    /// Mô tả sản phẩm
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Giá tiền (amount)
    /// Business rule: Phải > 0, sẽ được validate trong Money Value Object
    /// </summary>
    public decimal PriceAmount { get; init; }

    /// <summary>
    /// Đơn vị tiền tệ (VND, USD, EUR...)
    /// Business rule: Phải là currency code hợp lệ
    /// </summary>
    public string Currency { get; init; } = "VND";

    /// <summary>
    /// Số lượng tồn kho ban đầu
    /// Business rule: Phải >= 0
    /// </summary>
    public int InitialStock { get; init; }

    /// <summary>
    /// ID của Category
    /// Business reference: Link đến Category aggregate
    /// </summary>
    public Guid CategoryId { get; init; }

    /// <summary>
    /// Người tạo sản phẩm (từ authentication context)
    /// Audit requirement: Track who created the product
    /// </summary>
    public string CreatedBy { get; init; } = string.Empty;
}