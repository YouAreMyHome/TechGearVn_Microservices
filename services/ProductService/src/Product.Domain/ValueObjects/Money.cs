namespace Product.Domain.ValueObjects;

/// <summary>
/// Value Object cho tiền tệ
/// Đảm bảo amount + currency luôn đi cùng nhau
/// Có business logic cho currency operations
/// </summary>
public record Money
{
    public decimal Amount { get; private init; }
    public string Currency { get; private init; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Factory method tạo Money với validation
    /// </summary>
    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Số tiền không được âm", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Mã tiền tệ không được để trống", nameof(currency));

        var normalizedCurrency = currency.ToUpperInvariant().Trim();

        // Kiểm tra currency hợp lệ (có thể mở rộng)
        var validCurrencies = new[] { "VND", "USD", "EUR", "JPY" };
        if (!validCurrencies.Contains(normalizedCurrency))
            throw new ArgumentException($"Mã tiền tệ '{normalizedCurrency}' không được hỗ trợ", nameof(currency));

        return new Money(amount, normalizedCurrency);
    }

    /// <summary>
    /// Cộng hai Money (phải cùng currency)
    /// </summary>
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Không thể cộng hai loại tiền tệ khác nhau: {Currency} và {other.Currency}");

        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// So sánh hai Money (phải cùng currency)
    /// </summary>
    public bool IsGreaterThan(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Không thể so sánh hai loại tiền tệ khác nhau: {Currency} và {other.Currency}");

        return Amount > other.Amount;
    }

    public override string ToString()
        => $"{Amount:N0} {Currency}";
}