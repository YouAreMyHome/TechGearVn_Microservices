namespace Product.Domain.ValueObjects;

/// <summary>
/// Value Object cho tên sản phẩm
/// Đảm bảo tên sản phẩm luôn valid và clean
/// </summary>
public record ProductName
{
    public string Value { get; private init; }

    private ProductName(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Factory method để tạo ProductName với validation
    /// Throw exception nếu invalid để fail-fast
    /// </summary>
    public static ProductName Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Tên sản phẩm không được để trống", nameof(value));

        if (value.Length > 200)
            throw new ArgumentException("Tên sản phẩm không được vượt quá 200 ký tự", nameof(value));

        if (value.Length < 3)
            throw new ArgumentException("Tên sản phẩm phải có ít nhất 3 ký tự", nameof(value));

        // Clean và normalize tên sản phẩm
        var cleanValue = value.Trim();

        return new ProductName(cleanValue);
    }

    /// <summary>
    /// Implicit conversion để dễ dàng sử dụng như string
    /// </summary>
    public static implicit operator string(ProductName productName)
        => productName.Value;

    public override string ToString() => Value;
}