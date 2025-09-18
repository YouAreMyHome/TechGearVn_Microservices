using AutoMapper;
using Product.Application.DTOs;

namespace Product.Application.Mappings;

/// <summary>
/// AutoMapper Profile cho Application Layer
/// Mapping giữa Domain Entities và Application DTOs
/// Tuân thủ Clean Architecture: Application chỉ biết về Domain, không biết về API layer
/// </summary>
public class ApplicationMappingProfile : Profile
{
    /// <summary>
    /// Initialize Application mapping profile
    /// </summary>
    public ApplicationMappingProfile()
    {
        CreateDomainToApplicationDtoMappings();
    }

    /// <summary>
    /// Mapping từ Domain Entities sang Application DTOs
    /// Flow: Domain → Application DTO → API Response (handled ở API layer)
    /// </summary>
    private void CreateDomainToApplicationDtoMappings()
    {
        // Domain.Entities.Product → Application.DTOs.ProductDto
        CreateMap<Domain.Entities.Product, ProductDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Value))
            .ForMember(dest => dest.Sku, opt => opt.MapFrom(src => src.Sku.Value))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Price.Currency))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description ?? string.Empty))
            .ForMember(dest => dest.StockQuantity, opt => opt.MapFrom(src => src.StockQuantity))
            .ForMember(dest => dest.CategoryId, opt => opt.MapFrom(src => src.CategoryId))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.CreatedBy))
            .ForMember(dest => dest.UpdatedBy, opt => opt.MapFrom(src => src.UpdatedBy));

        // Domain.Entities.Category → Application.DTOs.CategoryDto
        CreateMap<Domain.Entities.Category, CategoryDto>()
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
            .ForMember(dest => dest.Children, opt => opt.Ignore()) // Will be populated manually in handlers
            .ForMember(dest => dest.ProductCount, opt => opt.Ignore()); // Will be populated manually in handlers

        // Domain.Entities.Category → Application.DTOs.CategorySummaryDto
        CreateMap<Domain.Entities.Category, CategorySummaryDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
            .ForMember(dest => dest.Path, opt => opt.MapFrom(src => src.Path))
            .ForMember(dest => dest.Level, opt => opt.MapFrom(src => src.Level))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));

        // AutoMapper tự động xử lý collection mapping cho List<T> khi có mapping cho T
        // Không cần mapping collection tường minh

        // Mapping cho pagination result tuple → ProductListDto sẽ được handle manually trong Handler
        // Vì tuple mapping phức tạp, ta sẽ build ProductListDto directly trong Handler
    }
}