using FluentValidation;
using Product.Application.Commands;

namespace Product.Application.Validators;

/// <summary>
/// Validator cho UpdateProductPriceCommand
/// Price update là critical business operation nên validate kỹ
/// </summary>
public class UpdateProductPriceCommandValidator : AbstractValidator<UpdateProductPriceCommand>
{
    public UpdateProductPriceCommandValidator()
    {
        // ProductId validation
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("ProductId không được để trống");

        // New price validation
        RuleFor(x => x.NewPrice)
            .GreaterThan(0)
            .WithMessage("Giá mới phải lớn hơn 0")
            .LessThan(1_000_000_000)
            .WithMessage("Giá mới không được vượt quá 1 tỷ");

        // UpdatedBy validation
        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .WithMessage("Thông tin người cập nhật không được để trống")
            .Length(2, 100)
            .WithMessage("Tên người cập nhật phải có từ 2-100 ký tự");
    }
}