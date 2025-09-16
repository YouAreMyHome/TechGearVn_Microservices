using MediatR;

namespace Product.Application.Commands;

/// <summary>
/// Command để tạo sản phẩm mới
/// Command = Intent to change state
/// Implement IRequest<T> với T là return type (Guid = ProductId)
/// </summary>
public record CreateProductCommand(
    string Name,
    string? Sku,
    string Description,
    decimal Price,
    string Currency,
    int InitialStock,
    Guid CategoryId,
    string CreatedBy) : IRequest<Guid>;