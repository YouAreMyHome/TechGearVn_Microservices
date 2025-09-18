using AutoMapper;
using MediatR;
using Product.Application.DTOs;
using Product.Application.Queries;
using Product.Domain.Repositories;

namespace Product.Application.Queries.Handlers;

/// <summary>
/// Handler cho GetProductsQuery - Advanced product filtering với business logic
/// CQRS Read Handler: Optimized cho high-performance product catalog
/// Business logic: Category hierarchy, price filtering, search optimization
/// </summary>
public class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, PagedProductResult>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public GetProductsQueryHandler(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<PagedProductResult> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        // Business logic: Resolve category IDs với hierarchy
        List<Guid>? categoryIds = null;
        
        if (request.CategoryId.HasValue)
        {
            categoryIds = new List<Guid> { request.CategoryId.Value };
            
            // Nếu include sub-categories, resolve toàn bộ hierarchy
            if (request.IncludeSubCategories)
            {
                var subCategoryIds = await _categoryRepository.GetDescendantIdsAsync(
                    request.CategoryId.Value, cancellationToken);
                categoryIds.AddRange(subCategoryIds);
            }
        }

        // Repository call với advanced filtering
        var (products, totalCount) = await _productRepository.GetProductsPagedAsync(
            page: request.Page,
            pageSize: request.PageSize,
            categoryIds: categoryIds,
            minPrice: request.MinPrice,
            maxPrice: request.MaxPrice,
            searchTerm: request.SearchTerm,
            onlyActive: request.OnlyActive,
            sortBy: request.SortBy,
            sortDirection: request.SortDirection,
            cancellationToken: cancellationToken);

        // Map Domain entities → DTOs
        var productDtos = _mapper.Map<List<ProductDto>>(products);

        // Business metadata cho client
        var appliedFilters = new Dictionary<string, object>();
        if (request.CategoryId.HasValue)
            appliedFilters["categoryId"] = request.CategoryId.Value;
        if (request.MinPrice.HasValue)
            appliedFilters["minPrice"] = request.MinPrice.Value;
        if (request.MaxPrice.HasValue)
            appliedFilters["maxPrice"] = request.MaxPrice.Value;
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            appliedFilters["searchTerm"] = request.SearchTerm;

        return new PagedProductResult
        {
            Products = productDtos,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
            HasNextPage = request.Page * request.PageSize < totalCount,
            HasPreviousPage = request.Page > 1,
            AppliedFilters = appliedFilters
        };
    }
}

/// <summary>
/// Handler cho GetProductByIdQuery
/// Simple lookup với error handling
/// </summary>
public class GetProductByIdQueryHandler : IRequestHandler<GetProductByIdQuery, ProductDto?>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetProductByIdQueryHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<ProductDto?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        return product != null ? _mapper.Map<ProductDto>(product) : null;
    }
}

/// <summary>
/// Handler cho GetProductBySkuQuery
/// Business-critical operation cho SKU lookup
/// </summary>
public class GetProductBySkuQueryHandler : IRequestHandler<GetProductBySkuQuery, ProductDto?>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetProductBySkuQueryHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<ProductDto?> Handle(GetProductBySkuQuery request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetBySkuAsync(request.Sku, cancellationToken);
        return product != null ? _mapper.Map<ProductDto>(product) : null;
    }
}

/// <summary>
/// Handler cho GetLowStockProductsQuery
/// Inventory management operation
/// </summary>
public class GetLowStockProductsQueryHandler : IRequestHandler<GetLowStockProductsQuery, List<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public GetLowStockProductsQueryHandler(IProductRepository productRepository, IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<List<ProductDto>> Handle(GetLowStockProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetLowStockProductsAsync(request.Threshold, cancellationToken);
        return _mapper.Map<List<ProductDto>>(products);
    }
}

/// <summary>
/// Handler cho SearchProductsQuery
/// Delegating search logic đến GetProductsQuery để tái sử dụng business logic
/// </summary>
public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, PagedProductResult>
{
    private readonly IMediator _mediator;

    public SearchProductsQueryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<PagedProductResult> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        // Business logic: Delegate to GetProductsQuery để reuse filtering logic
        var getProductsQuery = new GetProductsQuery
        {
            SearchTerm = request.SearchTerm,
            Page = request.Page,
            PageSize = request.PageSize,
            CategoryId = request.CategoryId,
            IncludeSubCategories = request.IncludeSubCategories,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            OnlyActive = request.OnlyActive
        };

        return await _mediator.Send(getProductsQuery, cancellationToken);
    }
}

/// <summary>
/// Handler cho GetProductsByCategoryQuery
/// Category-focused product browsing
/// </summary>
public class GetProductsByCategoryQueryHandler : IRequestHandler<GetProductsByCategoryQuery, PagedProductResult>
{
    private readonly IMediator _mediator;

    public GetProductsByCategoryQueryHandler(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<PagedProductResult> Handle(GetProductsByCategoryQuery request, CancellationToken cancellationToken)
    {
        // Business logic: Delegate to GetProductsQuery với category focus
        var getProductsQuery = new GetProductsQuery
        {
            CategoryId = request.CategoryId,
            IncludeSubCategories = request.IncludeSubCategories,
            Page = request.Page,
            PageSize = request.PageSize,
            OnlyActive = request.OnlyActive,
            SortBy = request.SortBy,
            SortDirection = request.SortDirection
        };

        return await _mediator.Send(getProductsQuery, cancellationToken);
    }
}