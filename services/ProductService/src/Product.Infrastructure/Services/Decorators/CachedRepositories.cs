using Product.Domain.Repositories;
using Product.Domain.Entities;
using Product.Infrastructure.Services.Caching;
using Microsoft.Extensions.Logging;

namespace Product.Infrastructure.Services.Decorators;

/// <summary>
/// Wrapper classes để cache value types (workaround cho ICacheService constraint)
/// </summary>
internal record CachedValue<T>(T Value) where T : struct;
internal record CachedBool(bool Value);
internal record CachedInt(int Value);
internal record CachedDecimal(decimal Value);

/// <summary>
/// Cached decorator cho ProductRepository sử dụng Cache-Aside pattern
/// Decorator pattern: Wrap original repository với caching logic
/// Cache-Aside: Application code quản lý cache (load on miss, invalidate on write)
/// </summary>
public class CachedProductRepository : IProductRepository
{
    private readonly IProductRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedProductRepository> _logger;

    // Cache expiration times (strategies khác nhau cho các loại data)
    private static readonly TimeSpan EntityCacheTime = TimeSpan.FromMinutes(30);     // Individual entities
    private static readonly TimeSpan ListCacheTime = TimeSpan.FromMinutes(10);       // Lists và collections
    private static readonly TimeSpan QueryCacheTime = TimeSpan.FromMinutes(5);       // Query results

    public CachedProductRepository(
        IProductRepository repository,
        ICacheService cacheService,
        ILogger<CachedProductRepository> logger)
    {
        _repository = repository;
        _cacheService = cacheService;
        _logger = logger;
    }

    #region Basic CRUD with Caching

    public async Task<Domain.Entities.Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"product:{id}";
        
        var cached = await _cacheService.GetAsync<Domain.Entities.Product>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for Product {ProductId}", id);
            return cached;
        }

        _logger.LogDebug("Cache miss for Product {ProductId}, fetching from repository", id);
        var product = await _repository.GetByIdAsync(id, cancellationToken);
        
        if (product != null)
        {
            await _cacheService.SetAsync(cacheKey, product, EntityCacheTime, cancellationToken);
        }

        return product;
    }

    public async Task<Domain.Entities.Product> AddAsync(Domain.Entities.Product product, CancellationToken cancellationToken = default)
    {
        var result = await _repository.AddAsync(product, cancellationToken);

        // Invalidate relevant caches sau khi add
        await InvalidateProductCaches(result.CategoryId);

        _logger.LogDebug("Added Product {ProductId} and invalidated caches", result.Id);
        return result;
    }

    public async Task UpdateAsync(Domain.Entities.Product product, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateAsync(product, cancellationToken);

        // Invalidate entity cache
        await _cacheService.RemoveAsync($"product:{product.Id}", cancellationToken);
        
        // Invalidate category-related caches
        await InvalidateProductCaches(product.CategoryId);

        _logger.LogDebug("Updated Product {ProductId} and invalidated caches", product.Id);
    }

    public async Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(productId, cancellationToken);

        // Invalidate entity cache
        await _cacheService.RemoveAsync($"product:{productId}", cancellationToken);
        
        // Invalidate all list caches since we don't know the category
        await InvalidateProductCaches();

        _logger.LogDebug("Deleted Product {ProductId} and invalidated caches", productId);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"product_exists:{id}";
        
        var cached = await _cacheService.GetAsync<CachedBool>(cacheKey, cancellationToken);
        if (cached != null) return cached.Value;

        var exists = await _repository.ExistsAsync(id, cancellationToken);
        await _cacheService.SetAsync(cacheKey, new CachedBool(exists), QueryCacheTime, cancellationToken);

        return exists;
    }

    public async Task<Domain.Entities.Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"product_sku:{sku}";
        
        var cached = await _cacheService.GetAsync<Domain.Entities.Product>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var product = await _repository.GetBySkuAsync(sku, cancellationToken);
        if (product != null)
        {
            await _cacheService.SetAsync(cacheKey, product, EntityCacheTime, cancellationToken);
        }

        return product;
    }

    public async Task<bool> IsSkuUniqueAsync(string sku, Guid? excludeProductId = null, CancellationToken cancellationToken = default)
    {
        // SKU uniqueness check không cache vì có business-critical importance
        return await _repository.IsSkuUniqueAsync(sku, excludeProductId, cancellationToken);
    }

    public async Task<(List<Domain.Entities.Product> products, int totalCount)> GetProductsPagedAsync(
        int page, int pageSize, List<Guid>? categoryIds = null, decimal? minPrice = null, 
        decimal? maxPrice = null, string? searchTerm = null, bool onlyActive = true, 
        string sortBy = "name", string sortDirection = "asc", CancellationToken cancellationToken = default)
    {
        var cacheKey = $"products_paged:{page}:{pageSize}:{string.Join(",", categoryIds ?? new List<Guid>())}:{minPrice}:{maxPrice}:{searchTerm}:{onlyActive}:{sortBy}:{sortDirection}";
        
        // Tạo wrapper class cho tuple
        var cached = await _cacheService.GetAsync<PagedProductsResult>(cacheKey, cancellationToken);
        if (cached != null) return (cached.Products, cached.TotalCount);

        var result = await _repository.GetProductsPagedAsync(page, pageSize, categoryIds, minPrice, maxPrice, 
            searchTerm, onlyActive, sortBy, sortDirection, cancellationToken);
        
        await _cacheService.SetAsync(cacheKey, new PagedProductsResult(result.products, result.totalCount), QueryCacheTime, cancellationToken);
        return result;
    }

    public async Task<List<Domain.Entities.Product>> GetLowStockProductsAsync(int threshold = 10, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"low_stock_products:{threshold}";
        
        var cached = await _cacheService.GetAsync<List<Domain.Entities.Product>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var products = await _repository.GetLowStockProductsAsync(threshold, cancellationToken);
        await _cacheService.SetAsync(cacheKey, products, QueryCacheTime, cancellationToken);

        return products;
    }

    public async Task<List<Domain.Entities.Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = "products_all";
        
        var cached = await _cacheService.GetAsync<List<Domain.Entities.Product>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var products = await _repository.GetAllAsync(cancellationToken);
        await _cacheService.SetAsync(cacheKey, products, ListCacheTime, cancellationToken);

        return products;
    }

    public async Task<List<Domain.Entities.Product>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"products_category:{categoryId}";
        
        var cached = await _cacheService.GetAsync<List<Domain.Entities.Product>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var products = await _repository.GetByCategoryAsync(categoryId, cancellationToken);
        await _cacheService.SetAsync(cacheKey, products, ListCacheTime, cancellationToken);

        return products;
    }

    public async Task<List<Domain.Entities.Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice, string currency, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"products_price_range:{minPrice}:{maxPrice}:{currency}";
        
        var cached = await _cacheService.GetAsync<List<Domain.Entities.Product>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var products = await _repository.GetByPriceRangeAsync(minPrice, maxPrice, currency, cancellationToken);
        await _cacheService.SetAsync(cacheKey, products, QueryCacheTime, cancellationToken);

        return products;
    }

    public async Task<List<Domain.Entities.Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = "products_active";
        
        var cached = await _cacheService.GetAsync<List<Domain.Entities.Product>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var products = await _repository.GetActiveProductsAsync(cancellationToken);
        await _cacheService.SetAsync(cacheKey, products, ListCacheTime, cancellationToken);

        return products;
    }

    public async Task<List<Domain.Entities.Product>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"products_search:{searchTerm}";
        
        var cached = await _cacheService.GetAsync<List<Domain.Entities.Product>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var products = await _repository.SearchByNameAsync(searchTerm, cancellationToken);
        await _cacheService.SetAsync(cacheKey, products, QueryCacheTime, cancellationToken);

        return products;
    }

    public async Task<List<Domain.Entities.Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idsList = ids.ToList();
        var cacheKey = $"products_by_ids:{string.Join(",", idsList)}";
        
        var cached = await _cacheService.GetAsync<List<Domain.Entities.Product>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var products = await _repository.GetByIdsAsync(idsList, cancellationToken);
        await _cacheService.SetAsync(cacheKey, products, QueryCacheTime, cancellationToken);

        return products;
    }

    public async Task<int> CountByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"products_count_category:{categoryId}";
        
        var cached = await _cacheService.GetAsync<CachedInt>(cacheKey, cancellationToken);
        if (cached != null) return cached.Value;

        var count = await _repository.CountByCategoryAsync(categoryId, cancellationToken);
        await _cacheService.SetAsync(cacheKey, new CachedInt(count), QueryCacheTime, cancellationToken);

        return count;
    }

    public async Task<decimal> GetTotalInventoryValueAsync(string currency, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"total_inventory_value:{currency}";
        
        var cached = await _cacheService.GetAsync<CachedDecimal>(cacheKey, cancellationToken);
        if (cached != null) return cached.Value;

        var total = await _repository.GetTotalInventoryValueAsync(currency, cancellationToken);
        await _cacheService.SetAsync(cacheKey, new CachedDecimal(total), QueryCacheTime, cancellationToken);

        return total;
    }

    #endregion

    #region Cache Invalidation Helper

    /// <summary>
    /// Invalidate các cache patterns liên quan đến product operations
    /// </summary>
    private async Task InvalidateProductCaches(Guid? categoryId = null)
    {
        // Invalidate các list cache patterns
        await _cacheService.RemoveByPatternAsync("products_*");
        await _cacheService.RemoveByPatternAsync("products_paged:*");
        await _cacheService.RemoveByPatternAsync("active_products:*");
        await _cacheService.RemoveByPatternAsync("low_stock:*");

        if (categoryId.HasValue)
        {
            await _cacheService.RemoveByPatternAsync($"products_category:{categoryId}*");
            await _cacheService.RemoveByPatternAsync($"products_count_category:{categoryId}");
        }
    }

    #endregion
}

/// <summary>
/// Wrapper cho paged products result
/// </summary>
internal record PagedProductsResult(List<Domain.Entities.Product> Products, int TotalCount);

/// <summary>
/// Wrapper cho paged categories result
/// </summary>
internal record PagedCategoriesResult(List<Category> Categories, int TotalCount);

/// <summary>
/// Cached decorator cho CategoryRepository
/// Hierarchical data có cache strategy khác: longer expiration vì ít thay đổi
/// </summary>
public class CachedCategoryRepository : ICategoryRepository
{
    private readonly ICategoryRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CachedCategoryRepository> _logger;

    // Categories ít thay đổi → cache longer
    private static readonly TimeSpan EntityCacheTime = TimeSpan.FromHours(1);       // Individual categories
    private static readonly TimeSpan HierarchyCacheTime = TimeSpan.FromMinutes(30); // Tree structures
    private static readonly TimeSpan ListCacheTime = TimeSpan.FromMinutes(15);      // Category lists
    private static readonly TimeSpan QueryCacheTime = TimeSpan.FromMinutes(5);       // Query results

    public CachedCategoryRepository(
        ICategoryRepository repository,
        ICacheService cacheService,
        ILogger<CachedCategoryRepository> logger)
    {
        _repository = repository;
        _cacheService = cacheService;
        _logger = logger;
    }

    #region Query Operations với Caching

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"category:{id}";
        
        var cached = await _cacheService.GetAsync<Category>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var category = await _repository.GetByIdAsync(id, cancellationToken);
        if (category != null)
        {
            await _cacheService.SetAsync(cacheKey, category, EntityCacheTime, cancellationToken);
        }

        return category;
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"category_slug:{slug}";
        
        var cached = await _cacheService.GetAsync<Category>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var category = await _repository.GetBySlugAsync(slug, cancellationToken);
        if (category != null)
        {
            await _cacheService.SetAsync(cacheKey, category, EntityCacheTime, cancellationToken);
        }

        return category;
    }

    public async Task<List<Category>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = "categories_all_active";
        
        var cached = await _cacheService.GetAsync<List<Category>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var categories = await _repository.GetAllActiveAsync(cancellationToken);
        await _cacheService.SetAsync(cacheKey, categories, ListCacheTime, cancellationToken);

        return categories;
    }

    public async Task<(List<Category> categories, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null, bool? isActive = null, Guid? parentId = null, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"categories_paged:{pageNumber}:{pageSize}:{searchTerm}:{isActive}:{parentId}";
        
        var cached = await _cacheService.GetAsync<PagedCategoriesResult>(cacheKey, cancellationToken);
        if (cached != null) return (cached.Categories, cached.TotalCount);

        var result = await _repository.GetPagedAsync(pageNumber, pageSize, searchTerm, isActive, parentId, cancellationToken);
        await _cacheService.SetAsync(cacheKey, new PagedCategoriesResult(result.categories, result.totalCount), QueryCacheTime, cancellationToken);

        return result;
    }

    public async Task<List<Category>> GetCategoryHierarchyAsync(Guid? rootId = null, bool includeInactive = false, int maxDepth = 5, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"category_hierarchy:{rootId}:{includeInactive}:{maxDepth}";
        
        var cached = await _cacheService.GetAsync<List<Category>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var hierarchy = await _repository.GetCategoryHierarchyAsync(rootId, includeInactive, maxDepth, cancellationToken);
        await _cacheService.SetAsync(cacheKey, hierarchy, HierarchyCacheTime, cancellationToken);

        return hierarchy;
    }

    public async Task<List<Category>> GetCategoryPathAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"category_path:{categoryId}";
        
        var cached = await _cacheService.GetAsync<List<Category>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var path = await _repository.GetCategoryPathAsync(categoryId, cancellationToken);
        await _cacheService.SetAsync(cacheKey, path, HierarchyCacheTime, cancellationToken);

        return path;
    }

    public async Task<List<Guid>> GetDescendantIdsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"category_descendant_ids:{categoryId}";
        
        var cached = await _cacheService.GetAsync<List<Guid>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var descendantIds = await _repository.GetDescendantIdsAsync(categoryId, cancellationToken);
        await _cacheService.SetAsync(cacheKey, descendantIds, HierarchyCacheTime, cancellationToken);

        return descendantIds;
    }

    public async Task<List<Category>> GetChildrenAsync(Guid parentId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"category_children:{parentId}:{includeInactive}";
        
        var cached = await _cacheService.GetAsync<List<Category>>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var children = await _repository.GetChildrenAsync(parentId, includeInactive, cancellationToken);
        await _cacheService.SetAsync(cacheKey, children, ListCacheTime, cancellationToken);

        return children;
    }

    public async Task<bool> HasProductsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"category_has_products:{categoryId}";
        
        var cached = await _cacheService.GetAsync<CachedBool>(cacheKey, cancellationToken);
        if (cached != null) return cached.Value;

        var hasProducts = await _repository.HasProductsAsync(categoryId, cancellationToken);
        await _cacheService.SetAsync(cacheKey, new CachedBool(hasProducts), QueryCacheTime, cancellationToken);

        return hasProducts;
    }

    public async Task<bool> HasSubCategoriesAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"category_has_subcategories:{categoryId}";
        
        var cached = await _cacheService.GetAsync<CachedBool>(cacheKey, cancellationToken);
        if (cached != null) return cached.Value;

        var hasSubCategories = await _repository.HasSubCategoriesAsync(categoryId, cancellationToken);
        await _cacheService.SetAsync(cacheKey, new CachedBool(hasSubCategories), QueryCacheTime, cancellationToken);

        return hasSubCategories;
    }

    #endregion

    #region Command Operations với Cache Invalidation

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _repository.AddAsync(category, cancellationToken);
        await InvalidateCategoryCaches(category.ParentId);
        _logger.LogDebug("Added Category {CategoryId} and invalidated caches", category.Id);
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateAsync(category, cancellationToken);
        
        // Invalidate entity và hierarchy caches
        await _cacheService.RemoveAsync($"category:{category.Id}", cancellationToken);
        await _cacheService.RemoveAsync($"category_slug:{category.Slug}", cancellationToken);
        await InvalidateCategoryCaches(category.ParentId);
        
        _logger.LogDebug("Updated Category {CategoryId} and invalidated caches", category.Id);
    }

    public async Task DeleteAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(category, cancellationToken);
        await InvalidateCategoryCaches(category.ParentId);
        _logger.LogDebug("Deleted Category {CategoryId} and invalidated caches", category.Id);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _repository.SaveChangesAsync(cancellationToken);
    }

    #endregion

    #region Statistics với Caching

    public async Task<int> GetProductCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"category_product_count:{categoryId}";
        
        var cached = await _cacheService.GetAsync<CachedInt>(cacheKey, cancellationToken);
        if (cached != null) return cached.Value;

        var count = await _repository.GetProductCountAsync(categoryId, cancellationToken);
        await _cacheService.SetAsync(cacheKey, new CachedInt(count), QueryCacheTime, cancellationToken);

        return count;
    }

    public async Task<int> GetActiveProductCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"category_active_product_count:{categoryId}";
        
        var cached = await _cacheService.GetAsync<CachedInt>(cacheKey, cancellationToken);
        if (cached != null) return cached.Value;

        var count = await _repository.GetActiveProductCountAsync(categoryId, cancellationToken);
        await _cacheService.SetAsync(cacheKey, new CachedInt(count), QueryCacheTime, cancellationToken);

        return count;
    }

    public async Task<int> GetSubCategoriesCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"category_subcategories_count:{categoryId}";
        
        var cached = await _cacheService.GetAsync<CachedInt>(cacheKey, cancellationToken);
        if (cached != null) return cached.Value;

        var count = await _repository.GetSubCategoriesCountAsync(categoryId, cancellationToken);
        await _cacheService.SetAsync(cacheKey, new CachedInt(count), QueryCacheTime, cancellationToken);

        return count;
    }

    public async Task<int> GetDescendantsCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"category_descendants_count:{categoryId}";
        
        var cached = await _cacheService.GetAsync<CachedInt>(cacheKey, cancellationToken);
        if (cached != null) return cached.Value;

        var count = await _repository.GetDescendantsCountAsync(categoryId, cancellationToken);
        await _cacheService.SetAsync(cacheKey, new CachedInt(count), QueryCacheTime, cancellationToken);

        return count;
    }

    #endregion

    #region Cache Invalidation Helper

    /// <summary>
    /// Invalidate category hierarchy caches
    /// Hierarchical data cần invalidate nhiều cache patterns
    /// </summary>
    private async Task InvalidateCategoryCaches(Guid? parentId = null)
    {
        // Invalidate all category list caches
        await _cacheService.RemoveByPatternAsync("categories_*");
        await _cacheService.RemoveByPatternAsync("category_hierarchy:*");
        await _cacheService.RemoveByPatternAsync("category_path:*");
        await _cacheService.RemoveByPatternAsync("category_children:*");

        if (parentId.HasValue)
        {
            await _cacheService.RemoveByPatternAsync($"category_children:{parentId}:*");
        }
    }

    #endregion
}