using System.ComponentModel.DataAnnotations;

namespace Product.Api.Contracts.Categories;

/// <summary>
/// Request để tạo root category
/// </summary>
public record CreateRootCategoryRequest
{
    /// <summary>
    /// Tên category
    /// </summary>
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// URL-friendly slug
    /// </summary>
    [Required(ErrorMessage = "Category slug is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Category slug must be between 2 and 100 characters")]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug must contain only lowercase letters, numbers, and hyphens")]
    public string Slug { get; init; } = string.Empty;

    /// <summary>
    /// Mô tả category (optional)
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; init; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Display order must be a positive number")]
    public int DisplayOrder { get; init; } = 0;
}

/// <summary>
/// Request để tạo sub-category
/// </summary>
public record CreateSubCategoryRequest
{
    /// <summary>
    /// Tên category
    /// </summary>
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// URL-friendly slug
    /// </summary>
    [Required(ErrorMessage = "Category slug is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Category slug must be between 2 and 100 characters")]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "Slug must contain only lowercase letters, numbers, and hyphens")]
    public string Slug { get; init; } = string.Empty;

    /// <summary>
    /// Mô tả category (optional)
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; init; }

    /// <summary>
    /// Parent category ID
    /// </summary>
    [Required(ErrorMessage = "Parent category ID is required")]
    public Guid ParentId { get; init; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Display order must be a positive number")]
    public int DisplayOrder { get; init; } = 0;
}

/// <summary>
/// Request để cập nhật category
/// </summary>
public record UpdateCategoryRequest
{
    /// <summary>
    /// Tên category
    /// </summary>
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Mô tả category (optional)
    /// </summary>
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; init; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "Display order must be a positive number")]
    public int DisplayOrder { get; init; } = 0;

    /// <summary>
    /// Lý do cập nhật
    /// </summary>
    [Required(ErrorMessage = "Update reason is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Reason must be between 5 and 200 characters")]
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Request để toggle category status
/// </summary>
public record ToggleCategoryStatusRequest
{
    /// <summary>
    /// New active status
    /// </summary>
    public bool IsActive { get; init; }

    /// <summary>
    /// Lý do thay đổi status
    /// </summary>
    [Required(ErrorMessage = "Status change reason is required")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Reason must be between 5 and 200 characters")]
    public string Reason { get; init; } = string.Empty;
}