using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Product.Api.Contracts.Categories;
using Product.Application.Commands;
using Product.Application.DTOs;
using Product.Application.Queries;

namespace Product.Api.Controllers;

/// <summary>
/// Category API Controller
/// Handles category management operations: CRUD, hierarchy navigation, business operations
/// Clean Architecture: API layer chỉ handle HTTP concerns, business logic ở Application layer
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(IMediator mediator, IMapper mapper, ILogger<CategoriesController> logger)
    {
        _mediator = mediator;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách categories với pagination
    /// </summary>
    /// <param name="pageNumber">Số trang (default: 1)</param>
    /// <param name="pageSize">Kích thước trang (default: 10)</param>
    /// <param name="searchTerm">Từ khóa tìm kiếm</param>
    /// <param name="isActive">Filter theo trạng thái active</param>
    /// <param name="parentId">Filter theo parent category</param>
    /// <returns>Danh sách categories với pagination info</returns>
    [HttpGet]
    public async Task<ActionResult<CategoryListResponse>> GetCategories(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] Guid? parentId = null)
    {
        var query = new GetCategoriesQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            SearchTerm = searchTerm,
            IsActive = isActive,
            ParentId = parentId
        };

        var result = await _mediator.Send(query);
        return Ok(_mapper.Map<CategoryListResponse>(result));
    }

    /// <summary>
    /// Lấy category theo ID
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <returns>Category details</returns>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> GetCategory(Guid id)
    {
        var query = new GetCategoryByIdQuery { Id = id };
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound($"Category with ID {id} not found");
        }

        return Ok(_mapper.Map<CategoryResponse>(result));
    }

    /// <summary>
    /// Lấy category hierarchy (tree structure)
    /// </summary>
    /// <param name="rootId">Root category ID (null = all roots)</param>
    /// <param name="includeInactive">Include inactive categories</param>
    /// <param name="maxDepth">Maximum depth level</param>
    /// <returns>Category tree</returns>
    [HttpGet("hierarchy")]
    public async Task<ActionResult<List<CategoryResponse>>> GetCategoryHierarchy(
        [FromQuery] Guid? rootId = null,
        [FromQuery] bool includeInactive = false,
        [FromQuery] int maxDepth = 5)
    {
        var query = new GetCategoryHierarchyQuery
        {
            RootId = rootId,
            IncludeInactive = includeInactive,
            MaxDepth = maxDepth
        };

        var result = await _mediator.Send(query);
        return Ok(_mapper.Map<List<CategoryResponse>>(result));
    }

    /// <summary>
    /// Lấy category path (breadcrumb navigation)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <returns>Path from root to category</returns>
    [HttpGet("{id:guid}/path")]
    public async Task<ActionResult<List<CategorySummaryResponse>>> GetCategoryPath(Guid id)
    {
        var query = new GetCategoryPathQuery { CategoryId = id };
        var result = await _mediator.Send(query);
        return Ok(_mapper.Map<List<CategorySummaryResponse>>(result));
    }

    /// <summary>
    /// Lấy categories cho dropdown/select controls
    /// </summary>
    /// <param name="includeInactive">Include inactive categories</param>
    /// <param name="excludeCategoryId">Exclude specific category</param>
    /// <returns>Simplified category list</returns>
    [HttpGet("for-select")]
    public async Task<ActionResult<List<CategorySummaryResponse>>> GetCategoriesForSelect(
        [FromQuery] bool includeInactive = false,
        [FromQuery] Guid? excludeCategoryId = null)
    {
        var query = new GetCategoriesForSelectQuery
        {
            IncludeInactive = includeInactive,
            ExcludeCategoryId = excludeCategoryId
        };

        var result = await _mediator.Send(query);
        return Ok(_mapper.Map<List<CategorySummaryResponse>>(result));
    }

    /// <summary>
    /// Tạo root category mới
    /// </summary>
    /// <param name="request">Category creation data</param>
    /// <returns>Created category ID</returns>
    [HttpPost("root")]
    public async Task<ActionResult<CreateCategoryResponse>> CreateRootCategory([FromBody] CreateRootCategoryRequest request)
    {
        var command = _mapper.Map<CreateRootCategoryCommand>(request);
        var categoryId = await _mediator.Send(command);

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = categoryId },
            new CreateCategoryResponse { Id = categoryId });
    }

    /// <summary>
    /// Tạo sub-category
    /// </summary>
    /// <param name="request">Sub-category creation data</param>
    /// <returns>Created category ID</returns>
    [HttpPost("sub")]
    public async Task<ActionResult<CreateCategoryResponse>> CreateSubCategory([FromBody] CreateSubCategoryRequest request)
    {
        var command = _mapper.Map<CreateSubCategoryCommand>(request);
        var categoryId = await _mediator.Send(command);

        return CreatedAtAction(
            nameof(GetCategory),
            new { id = categoryId },
            new CreateCategoryResponse { Id = categoryId });
    }

    /// <summary>
    /// Cập nhật category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="request">Update data</param>
    /// <returns>Updated category</returns>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CategoryResponse>> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request)
    {
        var command = _mapper.Map<UpdateCategoryCommand>(request);
        command = command with { Id = id };

        var result = await _mediator.Send(command);
        return Ok(_mapper.Map<CategoryResponse>(result));
    }

    /// <summary>
    /// Toggle category status (activate/deactivate)
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="request">Status toggle data</param>
    /// <returns>Updated category</returns>
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<CategoryResponse>> ToggleCategoryStatus(Guid id, [FromBody] ToggleCategoryStatusRequest request)
    {
        var command = new ToggleCategoryStatusCommand
        {
            Id = id,
            IsActive = request.IsActive,
            Reason = request.Reason
        };

        var result = await _mediator.Send(command);
        return Ok(_mapper.Map<CategoryResponse>(result));
    }

    /// <summary>
    /// Xóa category
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <param name="reason">Deletion reason</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteCategory(Guid id, [FromQuery] string reason = "")
    {
        var command = new DeleteCategoryCommand
        {
            Id = id,
            Reason = reason
        };

        var result = await _mediator.Send(command);
        
        if (!result)
        {
            return NotFound($"Category with ID {id} not found");
        }

        return NoContent();
    }

    /// <summary>
    /// Validate category business rules
    /// </summary>
    /// <param name="categoryId">Category ID (for updates)</param>
    /// <param name="slug">Slug to validate</param>
    /// <param name="parentId">Parent ID to validate</param>
    /// <returns>Validation result</returns>
    [HttpGet("validate")]
    public async Task<ActionResult<ValidationResponse>> ValidateCategory(
        [FromQuery] Guid? categoryId = null,
        [FromQuery] string? slug = null,
        [FromQuery] Guid? parentId = null)
    {
        var query = new ValidateCategoryQuery
        {
            CategoryId = categoryId,
            Slug = slug,
            ParentId = parentId
        };

        var isValid = await _mediator.Send(query);
        return Ok(new ValidationResponse { IsValid = isValid });
    }

    /// <summary>
    /// Lấy category statistics
    /// </summary>
    /// <param name="id">Category ID</param>
    /// <returns>Category statistics</returns>
    [HttpGet("{id:guid}/stats")]
    public async Task<ActionResult<CategoryStatsResponse>> GetCategoryStats(Guid id)
    {
        var query = new GetCategoryStatsQuery { CategoryId = id };
        var result = await _mediator.Send(query);
        return Ok(_mapper.Map<CategoryStatsResponse>(result));
    }
}