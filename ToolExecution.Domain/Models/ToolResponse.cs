namespace ToolExecution.Domain.Models;

/// <summary>
/// Status of a tool execution.
/// </summary>
public enum ExecutionStatus
{
    /// <summary>
    /// Tool executed successfully and returned results.
    /// </summary>
    Success = 0,

    /// <summary>
    /// Tool execution failed with an exception or error.
    /// </summary>
    Failed = 1,

    /// <summary>
    /// Tool execution exceeded the configured timeout.
    /// </summary>
    Timeout = 2,

    /// <summary>
    /// Input validation failed before tool execution.
    /// </summary>
    ValidationError = 3,

    /// <summary>
    /// Tool not found in registry.
    /// </summary>
    NotFound = 4,

    /// <summary>
    /// Tool is disabled and cannot be executed.
    /// </summary>
    Disabled = 5,

    /// <summary>
    /// Execution was cancelled by caller.
    /// </summary>
    Cancelled = 6
}

/// <summary>
/// Standardized response from tool execution.
/// Always returned, never throw raw exceptions to API.
/// </summary>
public class ToolResponse
{
    /// <summary>
    /// Trace ID from the original request (for distributed tracing).
    /// </summary>
    public required string TraceId { get; init; }

    /// <summary>
    /// Correlation ID from the original request.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Status of the execution.
    /// </summary>
    public required ExecutionStatus Status { get; init; }

    /// <summary>
    /// Output parameters from successful execution.
    /// Populated only if Status == Success.
    /// Keys match tool's OutputSchema properties.
    /// </summary>
    public IDictionary<string, object?> Output { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Error message if execution failed.
    /// Populated if Status != Success.
    /// Sanitized (no stack traces or sensitive info).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Milliseconds elapsed during execution (excluding network overhead).
    /// Includes validation, tool execution, but not serialization.
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// ISO 8601 UTC timestamp when response was created.
    /// </summary>
    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Whether execution was successful (Status == Success).
    /// Convenience property for client code.
    /// </summary>
    public bool IsSuccess => Status == ExecutionStatus.Success;

    /// <summary>
    /// Create a successful response.
    /// </summary>
    public static ToolResponse CreateSuccess(
        string traceId,
        IDictionary<string, object?> output,
        long executionTimeMs,
        string? correlationId = null)
    {
        return new ToolResponse
        {
            TraceId = traceId,
            CorrelationId = correlationId,
            Status = ExecutionStatus.Success,
            Output = output,
            ExecutionTimeMs = executionTimeMs
        };
    }

    /// <summary>
    /// Create a failed response with error message.
    /// </summary>
    public static ToolResponse CreateFailure(
        string traceId,
        string errorMessage,
        long executionTimeMs,
        ExecutionStatus status = ExecutionStatus.Failed,
        string? correlationId = null)
    {
        return new ToolResponse
        {
            TraceId = traceId,
            CorrelationId = correlationId,
            Status = status,
            ErrorMessage = errorMessage,
            ExecutionTimeMs = executionTimeMs
        };
    }
}
