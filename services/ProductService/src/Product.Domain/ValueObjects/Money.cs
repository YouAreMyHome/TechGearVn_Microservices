namespace Product.Domain.ValueObjects;

/// <summary>
/// Money Value Object theo DDD pattern
/// Business concept: Đại diện cho giá trị tiền tệ với amount và currency
/// Immutable: Thread-safe, predictable behavior
/// Value equality: So sánh theo value, không phải reference
/// </summary>
public record Money
{
    #region Properties

    /// <summary>
    /// Số tiền (amount)
    /// Business rule: Phải >= 0 cho product pricing
    /// </summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// Đơn vị tiền tệ (VND, USD, EUR, etc.)
    /// Business rule: Phải là currency code hợp lệ
    /// </summary>
    public string Currency { get; init; }

    #endregion

    #region Constructors

    /// <summary>
    /// Private constructor cho EF Core
    /// Infrastructure concern: EF Core cần parameterless constructor
    /// </summary>
    private Money()
    {
        Currency = string.Empty;
    }

    /// <summary>
    /// Private constructor với validation
    /// Domain logic: Chỉ factory methods mới có thể tạo Money
    /// </summary>
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Factory method chính để tạo Money với business validation
    /// Domain logic: Validate tất cả business rules cho Money creation
    /// </summary>
    public static Money Create(decimal amount, string currency)
    {
        // Business validation: Amount phải >= 0
        if (amount < 0)
            throw new ArgumentException("Amount không được âm", nameof(amount));

        // Business validation: Currency không được null/empty
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency không được để trống", nameof(currency));

        // Business validation: Currency format
        var normalizedCurrency = currency.ToUpper().Trim();
        if (normalizedCurrency.Length != 3)
            throw new ArgumentException("Currency code phải có 3 ký tự (VND, USD, EUR, ...)", nameof(currency));

        return new Money(amount, normalizedCurrency);
    }

    /// <summary>
    /// Factory methods cho các currency phổ biến
    /// Business convenience: Tạo Money object dễ dàng hơn
    /// </summary>
    public static Money VND(decimal amount) => Create(amount, "VND");
    public static Money USD(decimal amount) => Create(amount, "USD");
    public static Money EUR(decimal amount) => Create(amount, "EUR");

    /// <summary>
    /// Zero money cho default values
    /// Business concept: Represent "no cost" hoặc initial state
    /// </summary>
    public static Money Zero(string currency) => Create(0, currency);

    #endregion

    #region Business Methods

    /// <summary>
    /// Cộng hai Money cùng currency
    /// Business operation: Total calculation, price aggregation
    /// </summary>
    public Money Add(Money other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        if (Currency != other.Currency)
            throw new InvalidOperationException($"Không thể cộng {Currency} với {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// Trừ hai Money cùng currency
    /// Business operation: Discount calculation, price difference
    /// </summary>
    public Money Subtract(Money other)
    {
        if (other == null)
            throw new ArgumentNullException(nameof(other));

        if (Currency != other.Currency)
            throw new InvalidOperationException($"Không thể trừ {Currency} với {other.Currency}");

        var result = Amount - other.Amount;
        if (result < 0)
            throw new InvalidOperationException("Kết quả không được âm");

        return new Money(result, Currency);
    }

    /// <summary>
    /// Nhân với hệ số (cho discount, tax, quantity, etc.)
    /// Business operation: Price calculation với multiplier
    /// </summary>
    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new ArgumentException("Factor không được âm", nameof(factor));

        return new Money(Amount * factor, Currency);
    }

    /// <summary>
    /// Chia cho hệ số (cho split bills, unit price calculation)
    /// Business operation: Price division
    /// </summary>
    public Money Divide(decimal divisor)
    {
        if (divisor <= 0)
            throw new ArgumentException("Divisor phải lớn hơn 0", nameof(divisor));

        return new Money(Amount / divisor, Currency);
    }

    /// <summary>
    /// Chuyển đổi currency (placeholder for future implementation)
    /// Business operation: Multi-currency support
    /// </summary>
    public Money ConvertTo(string targetCurrency, decimal exchangeRate)
    {
        if (string.IsNullOrWhiteSpace(targetCurrency))
            throw new ArgumentException("Target currency không được rỗng", nameof(targetCurrency));

        if (exchangeRate <= 0)
            throw new ArgumentException("Exchange rate phải lớn hơn 0", nameof(exchangeRate));

        if (Currency.Equals(targetCurrency, StringComparison.OrdinalIgnoreCase))
            return this;

        return Create(Amount * exchangeRate, targetCurrency);
    }

    /// <summary>
    /// So sánh giá trị (cùng currency)
    /// Business operation: Price comparison
    /// </summary>
    public int CompareTo(Money other)
    {
        if (other == null) return 1;

        if (Currency != other.Currency)
            throw new InvalidOperationException($"Không thể so sánh {Currency} với {other.Currency}");

        return Amount.CompareTo(other.Amount);
    }

    /// <summary>
    /// Kiểm tra có lớn hơn không
    /// </summary>
    public bool IsGreaterThan(Money other) => CompareTo(other) > 0;

    /// <summary>
    /// Kiểm tra có nhỏ hơn không
    /// </summary>
    public bool IsLessThan(Money other) => CompareTo(other) < 0;

    /// <summary>
    /// Kiểm tra có bằng 0 không
    /// </summary>
    public bool IsZero() => Amount == 0;

    #endregion

    #region Display Methods

    /// <summary>
    /// Display formatting cho UI
    /// Presentation logic: Format theo business requirement
    /// </summary>
    public override string ToString()
    {
        return Currency switch
        {
            "VND" => $"{Amount:N0} VND",          // 29.990.000 VND
            "USD" => $"${Amount:N2}",            // $299.99
            "EUR" => $"€{Amount:N2}",            // €299.99
            "GBP" => $"£{Amount:N2}",            // £299.99
            _ => $"{Amount:N2} {Currency}"       // 299.99 CAD
        };
    }

    /// <summary>
    /// Format cho API responses
    /// Technical concern: Structured format cho JSON serialization
    /// </summary>
    public string ToApiFormat()
    {
        return $"{Amount:F2} {Currency}";
    }

    #endregion
}