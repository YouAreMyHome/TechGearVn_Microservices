using MediatR;

namespace Product.Application.Commands;

/// <summary>
/// Command để xóa Product (soft delete)
/// Business rule: Không hard delete để preserve audit trail và data integrity
/// Tuân thủ CQRS: Command chỉ chứa data cần thiết cho business operation
/// </summary>
public record DeleteProductCommand(
    Guid ProductId,
    string DeletedBy) : IRequest
{
    // Record với primary constructor cho immutable command
    // Không có return value vì delete operation chỉ cần success/failure indication
}