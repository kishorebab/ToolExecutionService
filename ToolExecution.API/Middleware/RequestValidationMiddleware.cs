using FluentValidation;
using System.Reflection;
using System.Text.Json;
using ToolExecution.Domain.Models;

namespace ToolExecution.API.Middleware;

/// <summary>
/// Middleware for validating request bodies using FluentValidation.
/// Automatically deserializes and validates strongly-typed ToolExecutionRequest bodies.
/// </summary>
public class RequestValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;

    public RequestValidationMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate POST requests to tool endpoints
        if (!context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) ||
            !context.Request.Path.StartsWithSegments("/api/tools", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Enable request body buffering so we can read it multiple times
        context.Request.EnableBuffering();

        try
        {
            // Read and parse the body
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body))
            {
                await RespondWithValidationError(context, "Request body cannot be empty");
                return;
            }

            // Try to validate based on the endpoint
            var endpoint = context.Request.Path.Value?.Split('/').Last();
            bool? validationResult = endpoint?.ToLowerInvariant() switch
            {
                "get-pod-logs" => await ValidateRequestAsync<GetPodLogsArguments>(body, context),
                "list-pods" => await ValidateRequestAsync<ListPodsArguments>(body, context),
                "get-deployments" => await ValidateRequestAsync<GetDeploymentsArguments>(body, context),
                "get-resource-usage" => await ValidateRequestAsync<GetResourceUsageArguments>(body, context),
                "execute-command" => await ValidateRequestAsync<ExecuteCommandArguments>(body, context),
                _ => (bool?)null
            };

            if (validationResult.HasValue && !validationResult.Value)
            {
                // Validation error already responded
                return;
            }

            // Reset body stream for next middleware
            context.Request.Body.Position = 0;
            await _next(context);
        }
        catch (JsonException ex)
        {
            await RespondWithValidationError(context, $"Invalid JSON format: {ex.Message}");
        }
        catch (Exception)
        {
            // Let the next middleware/handler deal with unexpected errors
            context.Request.Body.Position = 0;
            await _next(context);
        }
    }

    /// <summary>
    /// Validates a request body against the validator for the specified argument type.
    /// </summary>
    private async Task<bool> ValidateRequestAsync<TArguments>(string body, HttpContext context)
        where TArguments : class
    {
        try
        {
            // Deserialize the request
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var requestType = typeof(ToolExecutionRequest<TArguments>);
            var request = JsonSerializer.Deserialize(body, requestType, jsonOptions) as ToolExecutionRequest<TArguments>;

            if (request?.Arguments == null)
            {
                await RespondWithValidationError(context, $"Arguments field is required and must be a valid {typeof(TArguments).Name} object");
                return false;
            }

            // Get the validator from the service provider
            var validatorType = typeof(IValidator<>).MakeGenericType(typeof(TArguments));
            var validator = context.RequestServices.GetService(validatorType) as IValidator<TArguments>;

            if (validator == null)
            {
                // No validator registered, allow through
                return true;
            }

            // Validate
            var validationResult = await validator.ValidateAsync(request.Arguments);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors
                    .Select(e => new { property = e.PropertyName, message = e.ErrorMessage })
                    .ToList();

                await RespondWithValidationError(context, "Validation failed", errors);
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            await RespondWithValidationError(context, $"Invalid request format: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Responds with a 400 Bad Request containing validation error details.
    /// </summary>
    private static async Task RespondWithValidationError(HttpContext context, string message, object? details = null)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = message,
            details = details,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// Extension methods for registering the request validation middleware.
/// </summary>
public static class RequestValidationMiddlewareExtensions
{
    /// <summary>
    /// Adds the RequestValidationMiddleware to the application pipeline.
    /// </summary>
    public static IApplicationBuilder UseRequestValidation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestValidationMiddleware>();
    }
}
