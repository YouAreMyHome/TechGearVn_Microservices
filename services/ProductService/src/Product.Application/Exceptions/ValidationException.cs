using FluentValidation.Results;

namespace Product.Application.Exceptions;

/// <summary>
/// Application Exception cho validation failures
/// Wrap FluentValidation errors thành custom exception với structured data
/// API layer sẽ catch exception này và return 400 BadRequest với error details
/// Tuân thủ Clean Architecture: Application định nghĩa exception contract riêng
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Dictionary chứa field name → error messages
    /// Structured format dễ serialize thành JSON cho API response
    /// </summary>
    public IDictionary<string, string[]> Errors { get; }

    /// <summary>
    /// Constructor mặc định
    /// </summary>
    public ValidationException() : base("Có một hoặc nhiều lỗi validation")
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Constructor với FluentValidation failures
    /// Convert FluentValidation.Results.ValidationFailure thành structured format
    /// </summary>
    public ValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        // Group errors theo property name để dễ display trong UI
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    /// <summary>
    /// Constructor với custom message
    /// </summary>
    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Constructor với message và inner exception
    /// </summary>
    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
        Errors = new Dictionary<string, string[]>();
    }

    /// <summary>
    /// Constructor với single field error (convenience method)
    /// </summary>
    public ValidationException(string fieldName, string errorMessage) : this()
    {
        Errors[fieldName] = new[] { errorMessage };
    }

    /// <summary>
    /// Kiểm tra có errors không
    /// </summary>
    public bool HasErrors => Errors.Any();

    /// <summary>
    /// Get tất cả error messages thành single string
    /// Useful cho logging
    /// </summary>
    public string GetErrorSummary()
    {
        return string.Join("; ", Errors.SelectMany(kvp =>
            kvp.Value.Select(error => $"{kvp.Key}: {error}")));
    }
}