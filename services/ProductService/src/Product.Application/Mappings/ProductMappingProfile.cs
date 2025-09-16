using AutoMapper;
using Product.Application.Commands; // Thêm namespace cho Commands
using Product.Application.DTOs;
using Product.Domain.Entities;

namespace Product.Application.Mappings;

/// <summary>
/// AutoMapper Profile cho Product mapping
/// Tuân thủ Clean Architecture: Application layer orchestrate mapping
/// Tự động map giữa Domain Entities ↔ Application DTOs
/// Giảm boilerplate code và đảm bảo consistency
/// </summary>
public class ProductMappingProfile : Profile
{
    public ProductMappingProfile()
    {
        CreateMappings();
    }

    private void CreateMappings()
    {
        // ============ DOMAIN ENTITY → DTO (Read Operations) ============

        /// <summary>
        /// Map từ Product Aggregate Root sang ProductDto
        /// Value Objects được flatten thành primitive properties
        /// Computed properties được tính toán cho UI convenience
        /// </summary>
        CreateMap<Domain.Entities.Product, ProductDto>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Value))
            .ForMember(dest => dest.Sku, opt => opt.MapFrom(src => src.Sku.Value))
            .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Price.Currency))
            .ForMember(dest => dest.InStock, opt => opt.MapFrom(src => src.StockQuantity > 0))
            .ForMember(dest => dest.LowStock, opt => opt.MapFrom(src => src.StockQuantity <= 10))
            .ForMember(dest => dest.DisplayPrice, opt => opt.MapFrom(src => $"{src.Price.Amount:N0} {src.Price.Currency}"));

        // ============ REQUEST DTOs → COMMANDS (Write Operations) ============

        /// <summary>
        /// Map CreateProductRequest → CreateProductCommand
        /// API Controller sẽ dùng mapping này để chuyển HTTP request thành Command
        /// CreatedBy sẽ được inject từ User Context (JWT) trong Controller
        /// </summary>
        CreateMap<CreateProductRequest, CreateProductCommand>()
            .ConstructUsing(src => new CreateProductCommand(
                src.Name,
                src.Sku,
                src.Description,
                src.Price,
                src.Currency,
                src.InitialStock,
                src.CategoryId,
                "System" // CreatedBy - sẽ được override từ User Context
            ));

        /// <summary>
        /// Map UpdateProductRequest → UpdateProductCommand  
        /// ProductId sẽ được lấy từ route parameter trong Controller
        /// UpdatedBy sẽ được inject từ User Context (JWT)
        /// </summary>
        CreateMap<UpdateProductRequest, UpdateProductCommand>()
            .ConstructUsing((src, context) => new UpdateProductCommand(
                Guid.Empty, // ProductId sẽ được set từ route parameter
                src.Name,
                src.Description,
                "System" // UpdatedBy - sẽ được override từ User Context
            ));

        /// <summary>
        /// Map UpdateProductPriceRequest → UpdateProductPriceCommand
        /// Price change là critical operation, cần audit trail đầy đủ
        /// </summary>
        CreateMap<UpdateProductPriceRequest, UpdateProductPriceCommand>()
            .ConstructUsing((src, context) => new UpdateProductPriceCommand(
                Guid.Empty, // ProductId từ route
                src.NewPrice,
                "System" // UpdatedBy từ User Context
            ));

        /// <summary>
        /// Map UpdateProductStockRequest → UpdateProductStockCommand
        /// Stock management quan trọng cho inventory accuracy
        /// </summary>
        CreateMap<UpdateProductStockRequest, UpdateProductStockCommand>()
            .ConstructUsing((src, context) => new UpdateProductStockCommand(
                Guid.Empty, // ProductId từ route
                src.NewQuantity,
                src.Reason,
                "System" // UpdatedBy từ User Context
            ));
    }
}