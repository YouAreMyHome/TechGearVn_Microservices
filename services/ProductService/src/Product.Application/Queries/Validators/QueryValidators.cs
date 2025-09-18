using FluentValidation;
using Product.Application.Queries;

namespace Product.Application.Queries.Validators;

/// <summary>
/// Validator cho GetProductsQuery
/// Application Layer: Input validation cho query parameters
/// Validation logic: Pagination limits, filter constraints, sort validation
/// </summary>
public class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        // Pagination validation
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page phải lớn hơn 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0")
            .LessThanOrEqualTo(100).WithMessage("PageSize không được quá 100 để tránh performance issues");

        // Price range validation
        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0).WithMessage("MinPrice phải >= 0")
            .When(x => x.MinPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0).WithMessage("MaxPrice phải >= 0")
            .When(x => x.MaxPrice.HasValue);

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage("MinPrice phải nhỏ hơn hoặc bằng MaxPrice")
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue);

        // Search term validation
        RuleFor(x => x.SearchTerm)
            .MaximumLength(100).WithMessage("SearchTerm không được quá 100 ký tự")
            .Must(BeValidSearchTerm).WithMessage("SearchTerm chứa ký tự không hợp lệ")
            .When(x => !string.IsNullOrEmpty(x.SearchTerm));

        // Sort validation
        RuleFor(x => x.SortBy)
            .Must(BeValidSortField).WithMessage("SortBy phải là: name, price, created_date, updated_date, stock")
            .When(x => !string.IsNullOrEmpty(x.SortBy));

        RuleFor(x => x.SortDirection)
            .Must(BeValidSortDirection).WithMessage("SortDirection phải là: asc hoặc desc")
            .When(x => !string.IsNullOrEmpty(x.SortDirection));
    }

    private static bool BeValidSearchTerm(string? searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return true;

        // Không cho phép ký tự đặc biệt nguy hiểm
        var dangerousChars = new[] { '<', '>', '\'', '"', '&', '%', ';', '(', ')', '+' };
        return !dangerousChars.Any(searchTerm.Contains);
    }

    private static bool BeValidSortField(string? sortBy)
    {
        if (string.IsNullOrEmpty(sortBy))
            return true;

        var validSortFields = new[] { "name", "price", "created_date", "updated_date", "stock" };
        return validSortFields.Contains(sortBy.ToLowerInvariant());
    }

    private static bool BeValidSortDirection(string? sortDirection)
    {
        if (string.IsNullOrEmpty(sortDirection))
            return true;

        var validDirections = new[] { "asc", "desc" };
        return validDirections.Contains(sortDirection.ToLowerInvariant());
    }
}

/// <summary>
/// Validator cho GetProductByIdQuery
/// </summary>
public class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("ProductId không được để trống");
    }
}

/// <summary>
/// Validator cho GetProductsByCategoryQuery
/// </summary>
public class GetProductsByCategoryQueryValidator : AbstractValidator<GetProductsByCategoryQuery>
{
    public GetProductsByCategoryQueryValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("CategoryId không được để trống");

        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page phải lớn hơn 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0")
            .LessThanOrEqualTo(50).WithMessage("PageSize không được quá 50");
    }
}