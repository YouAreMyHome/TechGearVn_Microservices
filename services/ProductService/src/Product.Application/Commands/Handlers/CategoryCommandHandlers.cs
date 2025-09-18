using AutoMapper;
using MediatR;
using Product.Application.Commands;
using Product.Application.DTOs;
using Product.Domain.Entities;
using Product.Domain.Repositories;

namespace Product.Application.Commands.Handlers;

/// <summary>
/// Handler cho CreateRootCategoryCommand
/// Business logic: Tạo root category không có parent
/// </summary>
public class CreateRootCategoryHandler : IRequestHandler<CreateRootCategoryCommand, Guid>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public CreateRootCategoryHandler(ICategoryRepository categoryRepository, IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateRootCategoryCommand request, CancellationToken cancellationToken)
    {
        // Validate slug uniqueness
        var existingCategory = await _categoryRepository.GetBySlugAsync(request.Slug);
        if (existingCategory != null)
        {
            throw new InvalidOperationException($"Category with slug '{request.Slug}' already exists");
        }

        // Create root category
        var category = Category.CreateRootCategory(
            request.Name,
            request.Slug,
            request.Description,
            request.DisplayOrder,
            "System" // TODO: Get from current user context
        );

        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();

        return category.Id;
    }
}

/// <summary>
/// Handler cho CreateSubCategoryCommand
/// Business logic: Tạo sub-category với parent validation
/// </summary>
public class CreateSubCategoryHandler : IRequestHandler<CreateSubCategoryCommand, Guid>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public CreateSubCategoryHandler(ICategoryRepository categoryRepository, IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<Guid> Handle(CreateSubCategoryCommand request, CancellationToken cancellationToken)
    {
        // Validate parent exists
        var parentCategory = await _categoryRepository.GetByIdAsync(request.ParentId);
        if (parentCategory == null)
        {
            throw new InvalidOperationException($"Parent category with ID '{request.ParentId}' not found");
        }

        // Validate slug uniqueness
        var existingCategory = await _categoryRepository.GetBySlugAsync(request.Slug);
        if (existingCategory != null)
        {
            throw new InvalidOperationException($"Category with slug '{request.Slug}' already exists");
        }

        // Create sub-category
        var category = Category.CreateSubCategory(
            request.Name,
            request.Slug,
            request.Description,
            parentCategory,
            request.DisplayOrder,
            "System" // TODO: Get from current user context
        );

        await _categoryRepository.AddAsync(category);
        await _categoryRepository.SaveChangesAsync();

        return category.Id;
    }
}

/// <summary>
/// Handler cho UpdateCategoryCommand
/// </summary>
public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public UpdateCategoryHandler(ICategoryRepository categoryRepository, IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id);
        if (category == null)
        {
            throw new InvalidOperationException($"Category with ID '{request.Id}' not found");
        }

        category.UpdateDetails(
            request.Name,
            request.Description,
            request.DisplayOrder,
            "System" // TODO: Get from current user context
        );

        await _categoryRepository.UpdateAsync(category);
        await _categoryRepository.SaveChangesAsync();

        return _mapper.Map<CategoryDto>(category);
    }
}

/// <summary>
/// Handler cho ToggleCategoryStatusCommand
/// </summary>
public class ToggleCategoryStatusHandler : IRequestHandler<ToggleCategoryStatusCommand, CategoryDto>
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IMapper _mapper;

    public ToggleCategoryStatusHandler(ICategoryRepository categoryRepository, IMapper mapper)
    {
        _categoryRepository = categoryRepository;
        _mapper = mapper;
    }

    public async Task<CategoryDto> Handle(ToggleCategoryStatusCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id);
        if (category == null)
        {
            throw new InvalidOperationException($"Category with ID '{request.Id}' not found");
        }

        if (request.IsActive)
        {
            category.Activate("System"); // TODO: Get from current user context
        }
        else
        {
            category.Deactivate("System"); // TODO: Get from current user context
        }

        await _categoryRepository.UpdateAsync(category);
        await _categoryRepository.SaveChangesAsync();

        return _mapper.Map<CategoryDto>(category);
    }
}

/// <summary>
/// Handler cho DeleteCategoryCommand
/// </summary>
public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly ICategoryRepository _categoryRepository;

    public DeleteCategoryHandler(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id);
        if (category == null)
        {
            return false;
        }

        // Check if category has products
        var hasProducts = await _categoryRepository.HasProductsAsync(request.Id);
        if (hasProducts)
        {
            throw new InvalidOperationException("Cannot delete category that contains products");
        }

        // Check if category has sub-categories
        var hasSubCategories = await _categoryRepository.HasSubCategoriesAsync(request.Id);
        if (hasSubCategories)
        {
            throw new InvalidOperationException("Cannot delete category that has sub-categories");
        }

        await _categoryRepository.DeleteAsync(category);
        await _categoryRepository.SaveChangesAsync();

        return true;
    }
}