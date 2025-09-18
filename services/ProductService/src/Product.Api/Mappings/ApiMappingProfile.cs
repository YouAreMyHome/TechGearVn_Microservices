using AutoMapper;
using Product.Api.Contracts.Categories;
using Product.Api.Contracts.Products;
using Product.Application.Commands;
using Product.Application.DTOs;
using Product.Application.Queries;
using Product.Domain.ValueObjects;

namespace Product.Api.Mappings;

/// <summary>
/// AutoMapper Profile cho API Layer
/// Mapping concern: Convert giữa API Contracts, Commands, Queries và DTOs
/// Clean Architecture: API layer mapping logic với CQRS support
/// Business focus: Complete mapping cho product catalog operations
/// </summary>
public class ApiMappingProfile : Profile
{
    /// <summary>
    /// Initialize API mapping profile với comprehensive mappings
    /// </summary>
    public ApiMappingProfile()
    {
        // ================================
        // 🔄 API REQUEST TO QUERY MAPPINGS (CQRS Reads)
        // ================================

        // GetProductsRequest → GetProductsQuery
        CreateMap<GetProductsRequest, GetProductsQuery>()
            .ForMember(dest => dest.Page, opt => opt.MapFrom(src => src.Page))
            .ForMember(dest => dest.PageSize, opt => opt.MapFrom(src => src.PageSize))
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.IncludeSubCategories, opt => opt.MapFrom(src => src.IncludeSubCategories))
            .ForMember(dest => dest.SearchTerm, opt => opt.MapFrom(src => src.SearchTerm))
            .ForMember(dest => dest.MinPrice, opt => opt.MapFrom(src => src.MinPrice))
            .ForMember(dest => dest.MaxPrice, opt => opt.MapFrom(src => src.MaxPrice))
            .ForMember(dest => dest.OnlyActive, opt => opt.MapFrom(src => src.OnlyActive))
            .ForMember(dest => dest.SortBy, opt => opt.MapFrom(src => src.SortBy))
            .ForMember(dest => dest.SortDirection, opt => opt.MapFrom(src => src.SortDirection))
            .ForMember(dest => dest.IsActive, opt => opt.Ignore()) // Not mapped from request
            .ForMember(dest => dest.HasStock, opt => opt.Ignore()); // Not mapped from request

        // ================================
        // 🔄 API REQUEST TO COMMAND MAPPINGS (CQRS Writes)
        // ================================

        // CreateProductRequest → CreateProductCommand
        CreateMap<CreateProductRequest, CreateProductCommand>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Sku, opt => opt.MapFrom(src => src.Sku))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.PriceAmount, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.InitialStock, opt => opt.MapFrom(src => src.InitialStock))
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()); // Set từ authentication context

        // UpdateProductRequest → UpdateProductCommand
        CreateMap<UpdateProductRequest, UpdateProductCommand>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason))
            .ForMember(dest => dest.ProductId, opt => opt.Ignore()) // Set từ route parameter
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore()); // Set từ authentication context

        // BulkUpdatePricesRequest → BulkUpdateProductPricesCommand
        CreateMap<BulkUpdatePricesRequest, BulkUpdateProductPricesCommand>()
            .ForMember(dest => dest.ProductPriceUpdates, opt => opt.MapFrom(src => src.ProductPriceUpdates))
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason))
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore()); // Set từ authentication context

        // ProductPriceUpdate mapping (API Contract → Application Command)
        CreateMap<Product.Api.Contracts.Products.ProductPriceUpdate, Product.Application.Commands.ProductPriceUpdate>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
            .ForMember(dest => dest.NewPrice, opt => opt.MapFrom(src => src.NewPrice))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency));

        // ================================
        // 🔄 CATEGORY REQUEST TO COMMAND MAPPINGS
        // ================================

        // CreateRootCategoryRequest → CreateRootCategoryCommand
        CreateMap<CreateRootCategoryRequest, CreateRootCategoryCommand>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.DisplayOrder, opt => opt.MapFrom(src => src.DisplayOrder));

        // CreateSubCategoryRequest → CreateSubCategoryCommand
        CreateMap<CreateSubCategoryRequest, CreateSubCategoryCommand>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId))
            .ForMember(dest => dest.DisplayOrder, opt => opt.MapFrom(src => src.DisplayOrder));

        // UpdateCategoryRequest → UpdateCategoryCommand
        CreateMap<UpdateCategoryRequest, UpdateCategoryCommand>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.DisplayOrder, opt => opt.MapFrom(src => src.DisplayOrder))
            .ForMember(dest => dest.Reason, opt => opt.MapFrom(src => src.Reason))
            .ForMember(dest => dest.Id, opt => opt.Ignore()); // Set từ route parameter

        // ================================
        // 🔄 DTO TO RESPONSE MAPPINGS (Application → API)
        // ================================

        // ProductDto → ProductResponse
        CreateMap<ProductDto, ProductResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Sku, opt => opt.MapFrom(src => src.Sku))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
            .ForMember(dest => dest.DisplayPrice, opt => opt.Ignore()) // Will be computed in custom logic
            .ForMember(dest => dest.InStock, opt => opt.MapFrom(src => src.StockQuantity > 0))
            .ForMember(dest => dest.LowStock, opt => opt.Ignore()) // Will be computed in custom logic
            .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.StockQuantity))
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));

        // PagedProductResult → PagedProductResponse
        CreateMap<PagedProductResult, PagedProductResponse>()
            .ForMember(dest => dest.Products, opt => opt.MapFrom(src => src.Products))
            .ForMember(dest => dest.TotalCount, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.Page, opt => opt.MapFrom(src => src.Page))
            .ForMember(dest => dest.PageSize, opt => opt.MapFrom(src => src.PageSize))
            .ForMember(dest => dest.TotalPages, opt => opt.MapFrom(src => src.TotalPages))
            .ForMember(dest => dest.HasNextPage, opt => opt.MapFrom(src => src.HasNextPage))
            .ForMember(dest => dest.HasPreviousPage, opt => opt.MapFrom(src => src.HasPreviousPage))
            .ForMember(dest => dest.FilterMetadata, opt => opt.MapFrom(src => src.FilterMetadata)); // Map FilterMetadata to FilterMetadata

        // ProductFilterMetadata (Application) → ProductFilterMetadata (API) 
        CreateMap<Product.Application.DTOs.ProductFilterMetadata, Product.Api.Contracts.Products.ProductFilterMetadata>();

        // CategoryDto → CategoryResponse
        CreateMap<CategoryDto, CategoryResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId))
            .ForMember(dest => dest.Level, opt => opt.MapFrom(src => src.Level))
            .ForMember(dest => dest.Path, opt => opt.MapFrom(src => src.Path))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.DisplayOrder, opt => opt.MapFrom(src => src.DisplayOrder))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy))
            .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children))
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.ProductCount));

        // CategorySummaryDto → CategorySummaryResponse
        CreateMap<CategorySummaryDto, CategorySummaryResponse>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Path, opt => opt.MapFrom(src => src.Path))
            .ForMember(dest => dest.Level, opt => opt.MapFrom(src => src.Level))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

        // CategoryListDto → CategoryListResponse
        CreateMap<CategoryListDto, CategoryListResponse>()
            .ForMember(dest => dest.Categories, opt => opt.MapFrom(src => src.Categories))
            .ForMember(dest => dest.TotalCount, opt => opt.MapFrom(src => src.TotalCount))
            .ForMember(dest => dest.PageNumber, opt => opt.MapFrom(src => src.PageNumber))
            .ForMember(dest => dest.PageSize, opt => opt.MapFrom(src => src.PageSize))
            .ForMember(dest => dest.HasNextPage, opt => opt.MapFrom(src => src.HasNextPage))
            .ForMember(dest => dest.HasPreviousPage, opt => opt.MapFrom(src => src.HasPreviousPage));

        // CategoryStatsDto → CategoryStatsResponse
        CreateMap<CategoryStatsDto, CategoryStatsResponse>()
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.CategoryName))
            .ForMember(dest => dest.TotalProducts, opt => opt.MapFrom(src => src.TotalProducts))
            .ForMember(dest => dest.ActiveProducts, opt => opt.MapFrom(src => src.ActiveProducts))
            .ForMember(dest => dest.SubCategoriesCount, opt => opt.MapFrom(src => src.SubCategoriesCount))
            .ForMember(dest => dest.TotalDescendants, opt => opt.MapFrom(src => src.TotalDescendants));

        // ================================
        // 🔄 MONEY VALUE OBJECT MAPPINGS
        // ================================

        // Money → MoneyDto
        CreateMap<Money, MoneyDto>()
            .ForMember(dest => dest.Amount, opt => opt.MapFrom(src => src.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Currency))
            .ForMember(dest => dest.FormattedValue, opt => opt.MapFrom(src => src.ToString()));

        // ================================
        // 🔄 ADDITIONAL HELPER MAPPINGS
        // ================================

        // Nullable Guid mapping
        CreateMap<Guid?, Guid>().ConvertUsing((src, dest) => src ?? dest);
    }
}