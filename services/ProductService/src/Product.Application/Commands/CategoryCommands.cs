using MediatR;
using Product.Application.DTOs;

namespace Product.Application.Commands;

/// <summary>
/// Command để tạo root category mới
/// Business rule: Root category không có parent
/// </summary>
public record CreateRootCategoryCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DisplayOrder { get; init; } = 0;
}

/// <summary>
/// Command để tạo sub-category
/// Business rule: Phải có parent category hợp lệ
/// </summary>
public record CreateSubCategoryCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public Guid ParentId { get; init; }
    public int DisplayOrder { get; init; } = 0;
}

/// <summary>
/// Command để cập nhật thông tin category
/// </summary>
public record UpdateCategoryCommand : IRequest<CategoryDto>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DisplayOrder { get; init; }
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Command để di chuyển category sang parent khác
/// Business rule: Không được tạo circular reference
/// </summary>
public record MoveCategoryCommand : IRequest<CategoryDto>
{
    public Guid Id { get; init; }
    public Guid? NewParentId { get; init; }
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Command để activate/deactivate category
/// </summary>
public record ToggleCategoryStatusCommand : IRequest<CategoryDto>
{
    public Guid Id { get; init; }
    public bool IsActive { get; init; }
    public string Reason { get; init; } = string.Empty;
}

/// <summary>
/// Command để xóa category
/// Business rule: Chỉ xóa được khi không có products và sub-categories
/// </summary>
public record DeleteCategoryCommand : IRequest<bool>
{
    public Guid Id { get; init; }
    public string Reason { get; init; } = string.Empty;
}