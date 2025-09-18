using Product.Domain.Entities;

namespace Product.Domain.Repositories;

/// <summary>
/// Repository interface cho Category aggregate
/// Định nghĩa contract cho data access operations
/// </summary>
public interface ICategoryRepository
{
    #region Query Operations

    /// <summary>
    /// Lấy category theo ID
    /// </summary>
    Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy category theo slug (URL-friendly identifier)
    /// </summary>
    Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy tất cả categories đang active
    /// </summary>
    Task<List<Category>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy categories với pagination
    /// </summary>
    Task<(List<Category> categories, int totalCount)> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        string? searchTerm = null, 
        bool? isActive = null, 
        Guid? parentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy category hierarchy (cây phân cấp)
    /// Sử dụng recursive CTE để build tree structure
    /// </summary>
    Task<List<Category>> GetCategoryHierarchyAsync(
        Guid? rootId = null, 
        bool includeInactive = false, 
        int maxDepth = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy path từ root đến category (breadcrumb)
    /// </summary>
    Task<List<Category>> GetCategoryPathAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy all descendant IDs của một category (cho filtering)
    /// Business use case: Include sub-categories trong product search
    /// </summary>
    Task<List<Guid>> GetDescendantIdsAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy children categories của một parent
    /// </summary>
    Task<List<Category>> GetChildrenAsync(Guid parentId, bool includeInactive = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra category có products không
    /// </summary>
    Task<bool> HasProductsAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra category có sub-categories không
    /// </summary>
    Task<bool> HasSubCategoriesAsync(Guid categoryId, CancellationToken cancellationToken = default);

    #endregion

    #region Command Operations

    /// <summary>
    /// Thêm category mới
    /// </summary>
    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cập nhật category
    /// </summary>
    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xóa category
    /// </summary>
    Task DeleteAsync(Category category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save changes to database
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Statistics

    /// <summary>
    /// Đếm tổng số products trong category
    /// </summary>
    Task<int> GetProductCountAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm số products active trong category
    /// </summary>
    Task<int> GetActiveProductCountAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm số sub-categories trực tiếp
    /// </summary>
    Task<int> GetSubCategoriesCountAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Đếm tổng số descendants (tất cả cấp con)
    /// </summary>
    Task<int> GetDescendantsCountAsync(Guid categoryId, CancellationToken cancellationToken = default);

    #endregion
}