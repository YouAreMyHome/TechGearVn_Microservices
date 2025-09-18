using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Product.Domain.Exceptions;
using Product.Application.Exceptions;
using System.Text.Json;

namespace Product.Api.Filters;

/// <summary>
/// Action Filter để handle exceptions cụ thể tại controller level
/// Complement với GlobalExceptionHandlingMiddleware
/// Cho phép custom handling cho specific controllers nếu cần
/// </summary>
public class ApiExceptionFilter : ExceptionFilterAttribute
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public override void OnException(ExceptionContext context)
    {
        if (context.Exception is ValidationException validationEx)
        {
            HandleValidationException(context, validationEx);
            return;
        }

        if (context.Exception is DomainException domainEx)
        {
            HandleDomainException(context, domainEx);
            return;
        }

        // Let other exceptions bubble up to GlobalExceptionHandlingMiddleware
        base.OnException(context);
    }

    private void HandleValidationException(ExceptionContext context, ValidationException validationEx)
    {
        var problemDetails = new ValidationProblemDetails(validationEx.Errors)
        {
            Title = "Validation Error",
            Detail = "One or more validation errors occurred",
            Status = StatusCodes.Status400BadRequest,
            Instance = context.HttpContext.Request.Path
        };

        _logger.LogWarning("Validation error occurred: {@ValidationErrors}", validationEx.Errors);

        context.Result = new BadRequestObjectResult(problemDetails);
        context.ExceptionHandled = true;
    }

    private void HandleDomainException(ExceptionContext context, DomainException domainEx)
    {
        var statusCode = domainEx switch
        {
            ProductNotFoundException => StatusCodes.Status404NotFound,
            ProductSkuAlreadyExistsException => StatusCodes.Status409Conflict,
            InvalidStockOperationException => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        var problemDetails = new ProblemDetails
        {
            Title = "Business Rule Violation",
            Detail = domainEx.Message,
            Status = statusCode,
            Instance = context.HttpContext.Request.Path
        };

        _logger.LogWarning("Domain exception occurred: {DomainException}", domainEx.Message);

        context.Result = new ObjectResult(problemDetails) { StatusCode = statusCode };
        context.ExceptionHandled = true;
    }
}