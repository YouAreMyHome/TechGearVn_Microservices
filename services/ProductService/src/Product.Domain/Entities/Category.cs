using Product.Domain.Common;

namespace Product.Domain.Entities;

/// <summary>
/// Category Entity - Product categorization aggregate
/// Business concept: Phân loại sản phẩm theo business logic
/// DDD Pattern: Aggregate Root cho category management
/// </summary>
public class Category : AuditableEntity<Guid>
{
    #region Properties

    /// <summary>
    /// Tên category
    /// Business rule: Phải unique trong system
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// URL-friendly slug for SEO and routing
    /// Business rule: Phải unique trong system
    /// </summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>
    /// Mô tả category
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Parent category ID (cho hierarchical structure)
    /// Business rule: Có thể null nếu là root category
    /// </summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// Category level in hierarchy (0 = root, 1 = first level, etc.)
    /// </summary>
    public int Level { get; private set; }

    /// <summary>
    /// Full path from root to this category for breadcrumbs
    /// </summary>
    public string Path { get; private set; } = string.Empty;

    /// <summary>
    /// Category có active không
    /// Business rule: Inactive categories không cho phép assign products
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Display order cho UI sorting
    /// </summary>
    public int DisplayOrder { get; private set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Private constructor cho EF Core
    /// </summary>
    private Category()
    {
    }

    /// <summary>
    /// Private constructor với validation
    /// </summary>
    private Category(
        string name,
        string slug,
        string? description,
        Guid? parentId,
        int level,
        string path,
        int displayOrder,
        string createdBy)
    {
        Id = Guid.NewGuid();
        Name = name;
        Slug = slug;
        Description = description;
        ParentId = parentId;
        Level = level;
        Path = path;
        DisplayOrder = displayOrder;
        IsActive = true;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        UpdatedBy = createdBy;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Tạo root category (không có parent)
    /// Business use case: Tạo top-level categories
    /// </summary>
    public static Category CreateRootCategory(
        string name,
        string slug,
        string? description,
        int displayOrder,
        string createdBy)
    {
        ValidateCategoryName(name);
        ValidateSlug(slug);

        return new Category(name, slug, description, null, 0, slug, displayOrder, createdBy);
    }

    /// <summary>
    /// Tạo sub-category với parent
    /// Business use case: Tạo hierarchical category structure
    /// </summary>
    public static Category CreateSubCategory(
        string name,
        string slug,
        string? description,
        Category parentCategory,
        int displayOrder,
        string createdBy)
    {
        ValidateCategoryName(name);
        ValidateSlug(slug);

        if (parentCategory == null)
            throw new ArgumentNullException(nameof(parentCategory), "Parent category cannot be null");

        var level = parentCategory.Level + 1;
        var path = $"{parentCategory.Path}/{slug}";

        return new Category(name, slug, description, parentCategory.Id, level, path, displayOrder, createdBy);
    }

    #endregion

    #region Business Methods

    /// <summary>
    /// Update category information
    /// Business rules: Name phải unique, active category rules
    /// </summary>
    public void UpdateDetails(string name, string? description, int displayOrder, string updatedBy)
    {
        ValidateCategoryName(name);

        Name = name;
        Description = description;
        DisplayOrder = displayOrder;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;

        // TODO: Raise domain event CategoryUpdatedEvent
    }

    /// <summary>
    /// Activate category
    /// Business impact: Cho phép assign products vào category này
    /// </summary>
    public void Activate(string updatedBy)
    {
        if (IsActive) return;

        IsActive = true;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;

        // TODO: Raise domain event CategoryActivatedEvent
    }

    /// <summary>
    /// Deactivate category
    /// Business rule: Không được deactivate nếu có products đang sử dụng
    /// </summary>
    public void Deactivate(string updatedBy)
    {
        if (!IsActive) return;

        IsActive = false;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;

        // TODO: Raise domain event CategoryDeactivatedEvent
    }

    /// <summary>
    /// Update category path khi hierarchy thay đổi
    /// Business logic: Maintain full path cho easy navigation
    /// </summary>
    public void UpdatePath(string newPath, string updatedBy)
    {
        if (string.IsNullOrWhiteSpace(newPath))
            throw new ArgumentException("Category path không được empty", nameof(newPath));

        Path = newPath;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTime.UtcNow;
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validate category name business rules
    /// </summary>
    private static void ValidateCategoryName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name không được null hoặc empty", nameof(name));

        if (name.Length > 100)
            throw new ArgumentException("Category name không được vượt quá 100 ký tự", nameof(name));

        if (name.Trim() != name)
            throw new ArgumentException("Category name không được có khoảng trắng đầu/cuối", nameof(name));
    }

    /// <summary>
    /// Validate category slug business rules
    /// </summary>
    private static void ValidateSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Category slug không được null hoặc empty", nameof(slug));

        if (slug.Length > 100)
            throw new ArgumentException("Category slug không được vượt quá 100 ký tự", nameof(slug));

        if (!System.Text.RegularExpressions.Regex.IsMatch(slug, @"^[a-z0-9-]+$"))
            throw new ArgumentException("Category slug chỉ được chứa chữ thường, số và dấu gạch ngang", nameof(slug));
    }

    #endregion
}