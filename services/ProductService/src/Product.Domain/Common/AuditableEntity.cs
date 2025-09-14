using Product.Domain.Common;

namespace Product.Domain.Common;

/// <summary>
/// Base class cho tất cả entities cần audit thông tin
/// Cung cấp tracking về thời gian và người thực hiện thay đổi
/// Tái sử dụng được cho Product, Category, Order, v.v.
/// </summary>
public abstract class AuditableEntity<T> : AggregateRoot<T>
{
    /// <summary>
    /// Thời điểm tạo entity (UTC)
    /// </summary>
    public DateTime CreatedAt { get; protected set; }

    /// <summary>
    /// Người tạo entity (UserID hoặc System)
    /// </summary>
    public string CreatedBy { get; protected set; } = default!;

    /// <summary>
    /// Thời điểm cập nhật lần cuối (UTC)
    /// Null nếu chưa từng được update
    /// </summary>
    public DateTime? UpdatedAt { get; protected set; }

    /// <summary>
    /// Người cập nhật lần cuối (UserID hoặc System)
    /// Null nếu chưa từng được update
    /// </summary>
    public string? UpdatedBy { get; protected set; }

    /// <summary>
    /// Constructor protected để chỉ các class con có thể khởi tạo
    /// </summary>
    protected AuditableEntity()
    {
    }

    /// <summary>
    /// Thiết lập thông tin audit khi tạo mới entity
    /// Gọi method này trong factory methods của entities
    /// </summary>
    /// <param name="createdBy">Người tạo (bắt buộc)</param>
    protected void SetCreatedAudit(string createdBy)
    {
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ArgumentException("Người tạo không được để trống", nameof(createdBy));

        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    /// <summary>
    /// Cập nhật thông tin audit khi entity thay đổi
    /// Gọi method này trong tất cả business methods có thay đổi dữ liệu
    /// </summary>
    /// <param name="updatedBy">Người cập nhật (bắt buộc)</param>
    protected void SetUpdatedAudit(string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(updatedBy))
            throw new ArgumentException("Người cập nhật không được để trống", nameof(updatedBy));

        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }

    /// <summary>
    /// Kiểm tra entity đã từng được update chưa
    /// </summary>
    public bool HasBeenUpdated => UpdatedAt.HasValue;

    /// <summary>
    /// Tính tuổi của entity (tính từ lúc tạo)
    /// </summary>
    public TimeSpan Age => DateTime.UtcNow - CreatedAt;

    /// <summary>
    /// Thời gian từ lần update cuối (nếu có)
    /// </summary>
    public TimeSpan? TimeSinceLastUpdate => UpdatedAt.HasValue
        ? DateTime.UtcNow - UpdatedAt.Value
        : null;
}