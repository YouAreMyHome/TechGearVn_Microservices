namespace Product.Domain.ValueObjects;

/// <summary>
/// Value Object cho mã SKU (Stock Keeping Unit)
/// SKU phải unique và follow format nhất định
/// </summary>
public record ProductSku
{
    public string Value { get; private init; }

    private ProductSku(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Factory method tạo ProductSku với validation
    /// SKU format: PREFIX-YYYYMMDD-XXXX (ví dụ: PRD-20241201-0001)
    /// </summary>
    public static ProductSku Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("SKU không được để trống", nameof(value));

        var cleanValue = value.ToUpperInvariant().Trim();

        // Validate format: PREFIX-YYYYMMDD-XXXX
        if (!IsValidSkuFormat(cleanValue))
            throw new ArgumentException($"SKU '{value}' không đúng format. Phải theo dạng: PRD-YYYYMMDD-XXXX", nameof(value));

        return new ProductSku(cleanValue);
    }

    /// <summary>
    /// Generate SKU tự động theo ngày hiện tại
    /// </summary>
    public static ProductSku Generate(string prefix = "PRD")
    {
        var dateCode = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomCode = Random.Shared.Next(1, 9999).ToString("D4");

        return new ProductSku($"{prefix}-{dateCode}-{randomCode}");
    }

    private static bool IsValidSkuFormat(string sku)
    {
        // Pattern: PREFIX-YYYYMMDD-XXXX
        var parts = sku.Split('-');

        if (parts.Length != 3)
            return false;

        // Part 1: Prefix (3-5 characters)
        if (parts[0].Length < 2 || parts[0].Length > 5)
            return false;

        // Part 2: Date (8 digits)
        if (parts[1].Length != 8 || !parts[1].All(char.IsDigit))
            return false;

        // Part 3: Sequential number (4 digits)
        if (parts[2].Length != 4 || !parts[2].All(char.IsDigit))
            return false;

        return true;
    }

    /// <summary>
    /// Implicit conversion để dễ sử dụng như string
    /// </summary>
    public static implicit operator string(ProductSku sku) => sku.Value;

    public override string ToString() => Value;
}