using Microsoft.EntityFrameworkCore;
using Product.Domain.Entities;
using Product.Domain.Repositories;
using Product.Infrastructure.Persistence;

namespace Product.Infrastructure.Persistence.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ProductDbContext _context;

    public CategoryRepository(ProductDbContext context)
    {
        _context = context;
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _context.Categories.FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);
    }

    public async Task<List<Category>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Category> categories, int totalCount)> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        string? searchTerm = null, 
        bool? isActive = null, 
        Guid? parentId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Categories.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(c => c.Name.Contains(searchTerm));
        }

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        if (parentId.HasValue)
        {
            query = query.Where(c => c.ParentId == parentId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var categories = await query
            .OrderBy(c => c.DisplayOrder)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (categories, totalCount);
    }

    public async Task<List<Category>> GetHierarchyAsync(
        Guid? rootId = null, 
        int maxDepth = 0, 
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Categories.AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        if (maxDepth > 0)
        {
            query = query.Where(c => c.Level <= maxDepth);
        }

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Category>> GetCategoryHierarchyAsync(
        Guid? rootId = null,
        bool includeInactive = false,
        int maxDepth = 5,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Categories.AsQueryable();

        if (rootId.HasValue)
        {
            query = query.Where(c => c.ParentId == rootId || c.Id == rootId);
        }

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        if (maxDepth > 0)
        {
            query = query.Where(c => c.Level <= maxDepth);
        }

        return await query
            .OrderBy(c => c.Level)
            .ThenBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Category>> GetCategoryPathAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var result = new List<Category>();
        var currentId = categoryId;

        while (currentId != Guid.Empty)
        {
            var category = await GetByIdAsync(currentId, cancellationToken);
            if (category == null) break;

            result.Insert(0, category);
            currentId = category.ParentId ?? Guid.Empty;
        }

        return result;
    }

    public async Task<List<Guid>> GetDescendantIdsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var result = new List<Guid>();
        var directChildren = await _context.Categories
            .Where(c => c.ParentId == categoryId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        result.AddRange(directChildren);

        foreach (var childId in directChildren)
        {
            var childDescendants = await GetDescendantIdsAsync(childId, cancellationToken);
            result.AddRange(childDescendants);
        }

        return result;
    }

    public async Task<List<Category>> GetChildrenAsync(Guid parentId, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories.Where(c => c.ParentId == parentId);

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasProductsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(false);
    }

    public async Task<bool> HasSubCategoriesAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .AnyAsync(c => c.ParentId == categoryId, cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _context.Categories.AddAsync(category, cancellationToken);
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        _context.Categories.Update(category);
    }

    public async Task DeleteAsync(Category category, CancellationToken cancellationToken = default)
    {
        _context.Categories.Remove(category);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetProductCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(0);
    }

    public async Task<int> GetActiveProductCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(0);
    }

    public async Task<int> GetSubCategoriesCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Categories
            .CountAsync(c => c.ParentId == categoryId, cancellationToken);
    }

    public async Task<int> GetDescendantsCountAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var subCategories = await _context.Categories
            .Where(c => c.ParentId == categoryId)
            .ToListAsync(cancellationToken);

        int count = subCategories.Count;
        foreach (var subCategory in subCategories)
        {
            count += await GetDescendantsCountAsync(subCategory.Id, cancellationToken);
        }

        return count;
    }
}
