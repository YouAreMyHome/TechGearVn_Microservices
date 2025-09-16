using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using ValidationException = Product.Application.Exceptions.ValidationException; // Alias để tránh conflict

namespace Product.Application.Behaviors;

/// <summary>
/// MediatR Pipeline Behavior để validate Commands/Queries trước khi execute
/// Fail-fast approach: Nếu validation fail thì throw exception ngay
/// Chạy đầu tiên trong pipeline, trước LoggingBehavior và Handler chính
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

    public ValidationBehavior(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehavior<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        // Nếu không có validator nào thì skip validation
        if (!_validators.Any())
        {
            _logger.LogDebug("No validators found for {RequestName}", requestName);
            return await next();
        }

        _logger.LogDebug("Validating {RequestName} with {ValidatorCount} validators",
            requestName, _validators.Count());

        // Chạy tất cả validators cho request này
        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        // Collect tất cả validation failures
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        // Nếu có lỗi validation thì throw custom exception
        if (failures.Count != 0)
        {
            var errorMessage = $"Validation failed for {requestName}";

            _logger.LogWarning("{ErrorMessage}. Errors: {ValidationErrors}",
                errorMessage,
                string.Join("; ", failures.Select(f => f.ErrorMessage)));

            // Sử dụng custom ValidationException thay vì FluentValidation.ValidationException
            throw new ValidationException(failures);
        }

        _logger.LogDebug("Validation passed for {RequestName}", requestName);

        // Nếu validation pass thì continue với next behavior/handler
        return await next();
    }
}