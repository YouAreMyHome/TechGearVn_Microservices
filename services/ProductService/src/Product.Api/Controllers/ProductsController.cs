using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Product.Api.Contracts.Products;
using Product.Application.Commands;
using Product.Application.Queries;

namespace Product.Api.Controllers;

/// <summary>
/// Products API Controller - Product Catalog Management
/// API Layer: RESTful endpoints cho product operations
/// Business scope: Product CRUD, inventory management, catalog browsing
/// Clean Architecture: API → Application (CQRS) → Domain → Infrastructure
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IMediator mediator,
        IMapper mapper,
        ILogger<ProductsController> logger)
    {
        _mediator = mediator;
        _mapper = mapper;
        _logger = logger;
    }

    #region Query Operations (CQRS Reads)

    /// <summary>
    /// Lấy danh sách sản phẩm với advanced filtering
    /// Business use case: Product catalog browsing với category hierarchy
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedProductResponse>> GetProducts(
        [FromQuery] GetProductsRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting products with filters: {@Request}", request);

        var query = _mapper.Map<GetProductsQuery>(request);
        var result = await _mediator.Send(query, cancellationToken);
        
        var response = _mapper.Map<PagedProductResponse>(result);
        return Ok(response);
    }

    /// <summary>
    /// Lấy sản phẩm theo ID
    /// Business use case: Product detail page
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetProduct(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductByIdQuery(id);
        var product = await _mediator.Send(query, cancellationToken);

        if (product == null)
        {
            _logger.LogWarning("Product {ProductId} not found", id);
            return NotFound(new { Message = $"Không tìm thấy sản phẩm có ID: {id}" });
        }

        var response = _mapper.Map<ProductResponse>(product);
        return Ok(response);
    }

    /// <summary>
    /// Lấy sản phẩm theo SKU
    /// Business use case: Quick product lookup, barcode scanning
    /// </summary>
    [HttpGet("by-sku/{sku}")]
    [ProducesResponseType(typeof(ProductResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetProductBySku(
        string sku,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductBySkuQuery(sku);
        var product = await _mediator.Send(query, cancellationToken);

        if (product == null)
        {
            return NotFound(new { Message = $"Không tìm thấy sản phẩm có SKU: {sku}" });
        }

        var response = _mapper.Map<ProductResponse>(product);
        return Ok(response);
    }

    /// <summary>
    /// Search sản phẩm
    /// Business use case: Product discovery, customer search
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedProductResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedProductResponse>> SearchProducts(
        [FromQuery] string searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? categoryId = null,
        [FromQuery] bool includeSubCategories = true,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchProductsQuery
        {
            SearchTerm = searchTerm,
            Page = page,
            PageSize = pageSize,
            CategoryId = categoryId,
            IncludeSubCategories = includeSubCategories,
            MinPrice = minPrice,
            MaxPrice = maxPrice
        };

        var result = await _mediator.Send(query, cancellationToken);
        var response = _mapper.Map<PagedProductResponse>(result);
        return Ok(response);
    }

    /// <summary>
    /// Lấy sản phẩm theo category
    /// Business use case: Category page browsing
    /// </summary>
    [HttpGet("by-category/{categoryId:guid}")]
    [ProducesResponseType(typeof(PagedProductResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedProductResponse>> GetProductsByCategory(
        Guid categoryId,
        [FromQuery] bool includeSubCategories = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDirection = "asc",
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductsByCategoryQuery
        {
            CategoryId = categoryId,
            IncludeSubCategories = includeSubCategories,
            Page = page,
            PageSize = pageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        var result = await _mediator.Send(query, cancellationToken);
        var response = _mapper.Map<PagedProductResponse>(result);
        return Ok(response);
    }

    /// <summary>
    /// Lấy sản phẩm tồn kho thấp
    /// Business use case: Inventory alerts, restocking management
    /// </summary>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(List<ProductResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ProductResponse>>> GetLowStockProducts(
        [FromQuery] int threshold = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetLowStockProductsQuery(threshold);
        var products = await _mediator.Send(query, cancellationToken);
        
        var response = _mapper.Map<List<ProductResponse>>(products);
        return Ok(response);
    }

    #endregion

    #region Command Operations (CQRS Writes)

    /// <summary>
    /// Tạo sản phẩm mới
    /// Business use case: Product catalog management
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateProductResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateProductResponse>> CreateProduct(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating product: {ProductName}", request.Name);

        var command = _mapper.Map<CreateProductCommand>(request);
        command = command with { CreatedBy = GetCurrentUser() };

        var productId = await _mediator.Send(command, cancellationToken);

        // Return created product
        var createdProduct = await _mediator.Send(new GetProductByIdQuery(productId), cancellationToken);
        var response = new CreateProductResponse
        {
            ProductId = productId,
            Product = _mapper.Map<ProductResponse>(createdProduct)
        };

        return CreatedAtAction(
            nameof(GetProduct),
            new { id = productId },
            response);
    }

    /// <summary>
    /// Cập nhật thông tin sản phẩm
    /// Business use case: Product information maintenance
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = _mapper.Map<UpdateProductCommand>(request);
        command = command with 
        { 
            ProductId = id,
            UpdatedBy = GetCurrentUser()
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Cập nhật giá sản phẩm
    /// Business use case: Price management, promotional pricing
    /// </summary>
    [HttpPatch("{id:guid}/price")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateProductPrice(
        Guid id,
        [FromBody] UpdateProductPriceRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateProductPriceCommand
        {
            ProductId = id,
            NewPrice = request.NewPrice,
            Currency = request.Currency ?? "VND",
            UpdatedBy = GetCurrentUser(),
            Reason = request.Reason
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Cập nhật tồn kho sản phẩm
    /// Business use case: Inventory management
    /// </summary>
    [HttpPatch("{id:guid}/stock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UpdateProductStock(
        Guid id,
        [FromBody] UpdateProductStockRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateProductStockCommand
        {
            ProductId = id,
            NewQuantity = request.NewStock,
            Reason = request.Reason ?? "Manual Update",
            UpdatedBy = GetCurrentUser()
        };

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Deactivate sản phẩm (soft delete)
    /// Business use case: Product lifecycle management
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeactivateProduct(
        Guid id,
        [FromQuery] string? reason = "Discontinued",
        CancellationToken cancellationToken = default)
    {
        var command = new DeactivateProductCommand(
            id,
            reason ?? "Discontinued",
            GetCurrentUser()
        );

        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Bulk operations - Cập nhật giá nhiều sản phẩm
    /// Business use case: Promotional pricing, bulk price adjustments
    /// </summary>
    [HttpPatch("bulk/price")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<object>> BulkUpdatePrices(
        [FromBody] BulkUpdatePricesRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new BulkUpdateProductPricesCommand
        {
            ProductPriceUpdates = _mapper.Map<List<Product.Application.Commands.ProductPriceUpdate>>(request.ProductPriceUpdates),
            UpdatedBy = GetCurrentUser(),
            Reason = request.Reason ?? "Bulk Price Update"
        };

        var updatedCount = await _mediator.Send(command, cancellationToken);
        
        return Ok(new 
        { 
            Message = $"Đã cập nhật giá cho {updatedCount} sản phẩm",
            UpdatedCount = updatedCount 
        });
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Temporary method - sẽ thay bằng JWT token parsing
    /// </summary>
    private string GetCurrentUser()
    {
        // TODO: Parse từ JWT token
        return "admin@techgear.vn";
    }

    #endregion
}