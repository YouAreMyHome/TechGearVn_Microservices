using FluentValidation;
using Product.Application.Queries;

namespace Product.Application.Queries.Validators;

/// <summary>
/// Validator cho GetCategoriesQuery
/// Application Layer: Input validation cho category queries
/// </summary>
public class GetCategoriesQueryValidator : AbstractValidator<GetCategoriesQuery>
{
    public GetCategoriesQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("PageNumber phải lớn hơn 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("PageSize phải lớn hơn 0")
            .LessThanOrEqualTo(100).WithMessage("PageSize không được quá 100");

        RuleFor(x => x.SearchTerm)
            .MaximumLength(100).WithMessage("SearchTerm không được quá 100 ký tự")
            .When(x => !string.IsNullOrEmpty(x.SearchTerm));
    }
}

/// <summary>
/// Validator cho GetCategoryByIdQuery
/// </summary>
public class GetCategoryByIdQueryValidator : AbstractValidator<GetCategoryByIdQuery>
{
    public GetCategoryByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("CategoryId không được để trống");
    }
}

/// <summary>
/// Validator cho GetCategoryHierarchyQuery
/// </summary>
public class GetCategoryHierarchyQueryValidator : AbstractValidator<GetCategoryHierarchyQuery>
{
    public GetCategoryHierarchyQueryValidator()
    {
        RuleFor(x => x.MaxDepth)
            .GreaterThan(0).WithMessage("MaxDepth phải lớn hơn 0")
            .LessThanOrEqualTo(10).WithMessage("MaxDepth không được quá 10 level");
    }
}