using AutoMapper;
using MediatR;
using Product.Application.DTOs;
using Product.Application.Queries;
using Product.Domain.Repositories;

namespace Product.Application.Queries.Handlers;

/// <summary>
/// Handler cho GetCategoryByIdQuery
/// </summary>
public class GetCategoryByIdHandler : IRequestHandler<GetCategoryByIdQuery, CategoryDto?>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public GetCategoryByIdHandler(ICategoryRepository categoryRepository, IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<CategoryDto?> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id);
        return category == null ? null : _mapper.Map<CategoryDto>(category);
    }
}

/// <summary>
/// Handler cho GetCategoriesQuery
/// </summary>
public class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, CategoryListDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public GetCategoriesHandler(ICategoryRepository categoryRepository, IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<CategoryListDto> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
    {
        var (categories, totalCount) = await _categoryRepository.GetPagedAsync(
            request.PageNumber,
            request.PageSize,
            request.SearchTerm,
            request.IsActive,
            request.ParentId
        );

        var categoryDtos = _mapper.Map<List<CategoryDto>>(categories);

        return new CategoryListDto
        {
            Categories = categoryDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}

/// <summary>
/// Handler cho GetCategoryHierarchyQuery
/// </summary>
public class GetCategoryHierarchyHandler : IRequestHandler<GetCategoryHierarchyQuery, List<CategoryDto>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public GetCategoryHierarchyHandler(ICategoryRepository categoryRepository, IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<List<CategoryDto>> Handle(GetCategoryHierarchyQuery request, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetCategoryHierarchyAsync(
            request.RootId,
            request.IncludeInactive,
            request.MaxDepth
        );

        return _mapper.Map<List<CategoryDto>>(categories);
    }
}

/// <summary>
/// Handler cho GetCategoryPathQuery
/// </summary>
public class GetCategoryPathHandler : IRequestHandler<GetCategoryPathQuery, List<CategorySummaryDto>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public GetCategoryPathHandler(ICategoryRepository categoryRepository, IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<List<CategorySummaryDto>> Handle(GetCategoryPathQuery request, CancellationToken cancellationToken)
    {
        var path = await _categoryRepository.GetCategoryPathAsync(request.CategoryId);
        return _mapper.Map<List<CategorySummaryDto>>(path);
    }
}

/// <summary>
/// Handler cho GetCategoriesForSelectQuery
/// </summary>
public class GetCategoriesForSelectHandler : IRequestHandler<GetCategoriesForSelectQuery, List<CategorySummaryDto>>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public GetCategoriesForSelectHandler(ICategoryRepository categoryRepository, IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<List<CategorySummaryDto>> Handle(GetCategoriesForSelectQuery request, CancellationToken cancellationToken)
    {
        var categories = await _categoryRepository.GetAllActiveAsync();
        
        // Filter out excluded category if specified
        if (request.ExcludeCategoryId.HasValue)
        {
            categories = categories.Where(c => c.Id != request.ExcludeCategoryId.Value).ToList();
        }

        return _mapper.Map<List<CategorySummaryDto>>(categories);
    }
}

/// <summary>
/// Handler cho ValidateCategoryQuery
/// </summary>
public class ValidateCategoryHandler : IRequestHandler<ValidateCategoryQuery, bool>
{
    private readonly ICategoryRepository _categoryRepository;

    public ValidateCategoryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<bool> Handle(ValidateCategoryQuery request, CancellationToken cancellationToken)
    {
        // Validate slug uniqueness
        if (!string.IsNullOrEmpty(request.Slug))
        {
            var existingCategory = await _categoryRepository.GetBySlugAsync(request.Slug);
            if (existingCategory != null && 
                (!request.CategoryId.HasValue || existingCategory.Id != request.CategoryId.Value))
            {
                return false; // Slug already exists
            }
        }

        // Validate parent exists and no circular reference
        if (request.ParentId.HasValue && request.CategoryId.HasValue)
        {
            var path = await _categoryRepository.GetCategoryPathAsync(request.ParentId.Value);
            if (path.Any(c => c.Id == request.CategoryId.Value))
            {
                return false; // Would create circular reference
            }
        }

        return true;
    }
}

/// <summary>
/// Handler cho GetCategoryStatsQuery
/// </summary>
public class GetCategoryStatsHandler : IRequestHandler<GetCategoryStatsQuery, CategoryStatsDto>
{
    private readonly ICategoryRepository _categoryRepository;

    public GetCategoryStatsHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<CategoryStatsDto> Handle(GetCategoryStatsQuery request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId);
        if (category == null)
        {
            throw new InvalidOperationException($"Category with ID '{request.CategoryId}' not found");
        }

        // Get statistics (these methods would need to be implemented in repository)
        var totalProducts = await _categoryRepository.GetProductCountAsync(request.CategoryId);
        var activeProducts = await _categoryRepository.GetActiveProductCountAsync(request.CategoryId);
        var subCategoriesCount = await _categoryRepository.GetSubCategoriesCountAsync(request.CategoryId);
        var totalDescendants = await _categoryRepository.GetDescendantsCountAsync(request.CategoryId);

        return new CategoryStatsDto
        {
            CategoryId = category.Id,
            CategoryName = category.Name,
            TotalProducts = totalProducts,
            ActiveProducts = activeProducts,
            SubCategoriesCount = subCategoriesCount,
            TotalDescendants = totalDescendants
        };
    }
}