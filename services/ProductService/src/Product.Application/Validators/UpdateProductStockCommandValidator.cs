using FluentValidation;
using Product.Application.Commands;

namespace Product.Application.Validators;

/// <summary>
/// Validator cho UpdateProductStockCommand
/// Stock management quan trọng cho inventory accuracy
/// </summary>
public class UpdateProductStockCommandValidator : AbstractValidator<UpdateProductStockCommand>
{
    public UpdateProductStockCommandValidator()
    {
        // ProductId validation
        RuleFor(x => x.ProductId)
            .NotEmpty()
            .WithMessage("ProductId không được để trống");

        // New quantity validation
        RuleFor(x => x.NewQuantity)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Số lượng tồn kho mới không được âm")
            .LessThan(1_000_000)
            .WithMessage("Số lượng tồn kho không được vượt quá 1 triệu");

        // Reason validation
        RuleFor(x => x.Reason)
            .NotEmpty()
            .WithMessage("Lý do thay đổi tồn kho không được để trống")
            .Length(2, 200)
            .WithMessage("Lý do phải có từ 2-200 ký tự");

        // UpdatedBy validation
        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .WithMessage("Thông tin người cập nhật không được để trống");
    }
}