using FluentValidation;
using Product.Application.Commands;

namespace Product.Application.Validators;

/// <summary>
/// Validator cho CreateProductCommand
/// Validate input trước khi vào Domain logic
/// Fail-fast approach: Lỗi validation thì không chạy business logic
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        // Validation cho tên sản phẩm
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Tên sản phẩm không được để trống")
            .Length(3, 200)
            .WithMessage("Tên sản phẩm phải có từ 3-200 ký tự")
            .Matches(@"^[a-zA-Z0-9\s\-_.()]+$")
            .WithMessage("Tên sản phẩm chỉ được chứa chữ, số và ký tự đặc biệt cơ bản");

        // SKU validation (optional vì có thể auto-generate)
        RuleFor(x => x.Sku)
            .Matches(@"^[A-Z]{2,5}-\d{8}-\d{4}$")
            .WithMessage("SKU phải theo format: PREFIX-YYYYMMDD-XXXX (ví dụ: PRD-20241201-0001)")
            .When(x => !string.IsNullOrWhiteSpace(x.Sku));

        // Description validation
        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .WithMessage("Mô tả sản phẩm không được vượt quá 2000 ký tự");

        // Price validation
        RuleFor(x => x.Price)
            .GreaterThan(0)
            .WithMessage("Giá sản phẩm phải lớn hơn 0")
            .LessThan(1_000_000_000)
            .WithMessage("Giá sản phẩm không được vượt quá 1 tỷ");

        // Currency validation
        RuleFor(x => x.Currency)
            .NotEmpty()
            .WithMessage("Mã tiền tệ không được để trống")
            .Must(BeValidCurrency)
            .WithMessage("Mã tiền tệ không hợp lệ. Chỉ hỗ trợ: VND, USD, EUR, JPY");

        // Initial stock validation
        RuleFor(x => x.InitialStock)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Số lượng tồn kho ban đầu không được âm")
            .LessThan(1_000_000)
            .WithMessage("Số lượng tồn kho không được vượt quá 1 triệu");

        // CategoryId validation
        RuleFor(x => x.CategoryId)
            .NotEmpty()
            .WithMessage("CategoryId không được để trống");

        // CreatedBy validation
        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .WithMessage("Thông tin người tạo không được để trống")
            .Length(2, 100)
            .WithMessage("Tên người tạo phải có từ 2-100 ký tự");
    }

    /// <summary>
    /// Kiểm tra mã tiền tệ có hợp lệ không
    /// </summary>
    private static bool BeValidCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            return false;

        var validCurrencies = new[] { "VND", "USD", "EUR", "JPY" };
        return validCurrencies.Contains(currency.ToUpperInvariant());
    }
}