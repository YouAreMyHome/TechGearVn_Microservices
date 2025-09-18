using FluentValidation;
using Product.Application.Commands;
using Product.Domain.Repositories;

namespace Product.Application.Commands.Validators;

/// <summary>
/// Validator cho CreateProductCommand
/// Application Layer: Business validation rules
/// Validation logic: SKU uniqueness, category existence, price rules
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public CreateProductCommandValidator(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;

        // Name validation
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống")
            .MaximumLength(200).WithMessage("Tên sản phẩm không được quá 200 ký tự")
            .Must(BeValidProductName).WithMessage("Tên sản phẩm chứa ký tự không hợp lệ");

        // SKU validation
        RuleFor(x => x.Sku)
            .MaximumLength(50).WithMessage("SKU không được quá 50 ký tự")
            .Must(BeValidSku).WithMessage("SKU chỉ được chứa chữ cái, số và dấu gạch ngang")
            .MustAsync(BeUniqueSkuAsync).WithMessage("SKU đã tồn tại trong hệ thống")
            .When(x => !string.IsNullOrEmpty(x.Sku));

        // Price validation
        RuleFor(x => x.PriceAmount)
            .GreaterThan(0).WithMessage("Giá sản phẩm phải lớn hơn 0")
            .LessThanOrEqualTo(1_000_000_000m).WithMessage("Giá sản phẩm không được quá 1 tỷ VND");

        // Currency validation
        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Đơn vị tiền tệ không được để trống")
            .Length(3).WithMessage("Đơn vị tiền tệ phải có 3 ký tự")
            .Must(BeValidCurrency).WithMessage("Đơn vị tiền tệ không hợp lệ (VND, USD, EUR, JPY, GBP)");

        // Stock validation
        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0).WithMessage("Số lượng tồn kho phải >= 0")
            .LessThanOrEqualTo(1_000_000).WithMessage("Số lượng tồn kho không được quá 1 triệu");

        // Category validation
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category ID không được để trống")
            .MustAsync(CategoryExistAndActiveAsync).WithMessage("Category không tồn tại hoặc đã bị vô hiệu hóa");

        // Description validation
        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Mô tả không được quá 2000 ký tự");
    }

    /// <summary>
    /// Business Rule: Product name validation
    /// </summary>
    private static bool BeValidProductName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Không cho phép chỉ có số
        if (name.All(char.IsDigit))
            return false;

        // Không cho phép ký tự đặc biệt nguy hiểm
        var invalidChars = new[] { '<', '>', '"', '\'', '&' };
        return !invalidChars.Any(name.Contains);
    }

    /// <summary>
    /// Business Rule: SKU format validation
    /// </summary>
    private static bool BeValidSku(string? sku)
    {
        if (string.IsNullOrEmpty(sku))
            return true;

        // SKU chỉ chứa chữ cái, số, dấu gạch ngang
        return sku.All(c => char.IsLetterOrDigit(c) || c == '-');
    }

    /// <summary>
    /// Business Rule: SKU phải unique trong hệ thống
    /// </summary>
    private async Task<bool> BeUniqueSkuAsync(string? sku, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(sku))
            return true;

        return await _productRepository.IsSkuUniqueAsync(sku, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Business Rule: Category phải tồn tại và active
    /// </summary>
    private async Task<bool> CategoryExistAndActiveAsync(Guid categoryId, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
        return category != null && category.IsActive;
    }

    /// <summary>
    /// Business Rule: Currency validation
    /// </summary>
    private static bool BeValidCurrency(string currency)
    {
        var validCurrencies = new[] { "VND", "USD", "EUR", "JPY", "GBP", "AUD", "CAD", "CHF" };
        return validCurrencies.Contains(currency.ToUpperInvariant());
    }
}

/// <summary>
/// Validator cho UpdateProductCommand
/// </summary>
public class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;

    public UpdateProductCommandValidator(
        IProductRepository productRepository,
        ICategoryRepository categoryRepository)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID không được để trống")
            .MustAsync(ProductExistAsync).WithMessage("Sản phẩm không tồn tại");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên sản phẩm không được để trống")
            .MaximumLength(200).WithMessage("Tên sản phẩm không được quá 200 ký tự");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Mô tả không được quá 2000 ký tự");

        RuleFor(x => x.Category)
            .MaximumLength(100).WithMessage("Category không được quá 100 ký tự")
            .When(x => !string.IsNullOrEmpty(x.Category));

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Lý do cập nhật không được để trống")
            .MaximumLength(200).WithMessage("Lý do không được quá 200 ký tự");
    }

    private async Task<bool> ProductExistAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        return product != null;
    }

    private async Task<bool> CategoryExistAndActiveAsync(Guid? categoryId, CancellationToken cancellationToken)
    {
        if (!categoryId.HasValue)
            return true;

        var category = await _categoryRepository.GetByIdAsync(categoryId.Value, cancellationToken);
        return category != null && category.IsActive;
    }
}

/// <summary>
/// Validator cho UpdateProductPriceCommand
/// </summary>
public class UpdateProductPriceCommandValidator : AbstractValidator<UpdateProductPriceCommand>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductPriceCommandValidator(IProductRepository productRepository)
    {
        _productRepository = productRepository;

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID không được để trống")
            .MustAsync(ProductExistAsync).WithMessage("Sản phẩm không tồn tại");

        RuleFor(x => x.NewPrice)
            .GreaterThan(0).WithMessage("Giá mới phải lớn hơn 0")
            .LessThanOrEqualTo(1_000_000_000m).WithMessage("Giá không được quá 1 tỷ VND");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Đơn vị tiền tệ không được để trống")
            .Length(3).WithMessage("Đơn vị tiền tệ phải có 3 ký tự");

        RuleFor(x => x.Reason)
            .MaximumLength(200).WithMessage("Lý do không được quá 200 ký tự");
    }

    private async Task<bool> ProductExistAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        return product != null && product.IsActive;
    }
}

/// <summary>
/// Validator cho UpdateProductStockCommand
/// </summary>
public class UpdateProductStockCommandValidator : AbstractValidator<UpdateProductStockCommand>
{
    private readonly IProductRepository _productRepository;

    public UpdateProductStockCommandValidator(IProductRepository productRepository)
    {
        _productRepository = productRepository;

        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product ID không được để trống")
            .MustAsync(ProductExistAsync).WithMessage("Sản phẩm không tồn tại");

        RuleFor(x => x.NewQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Số lượng phải >= 0")
            .LessThanOrEqualTo(1_000_000).WithMessage("Số lượng không được quá 1 triệu");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Lý do cập nhật không được để trống")
            .MaximumLength(200).WithMessage("Lý do không được quá 200 ký tự");
    }

    private async Task<bool> ProductExistAsync(Guid productId, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
        return product != null;
    }
}