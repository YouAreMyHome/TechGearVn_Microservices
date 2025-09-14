namespace Product.Domain.Exceptions;
/// <summary>
/// Base exception cho tất cả Domain-level exceptions
/// Domain exceptions thể hiện vi phạm business rules
/// Khác với technical exceptions (network, database...)
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }

    protected DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}