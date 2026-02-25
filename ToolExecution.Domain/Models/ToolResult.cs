namespace ToolExecution.Domain.Models;

/// <summary>
/// Internal domain model for tool execution results.
/// This is used internally for processing and tracing.
/// </summary>
public sealed class ToolResult
{
    /// <summary>
    /// Unique trace ID matching the request.
    /// </summary>
    public string TraceId { get; init; } = string.Empty;

    /// <summary>
    /// Session/request ID from the original request.
    /// </summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>
    /// Name of the tool that was executed.
    /// </summary>
    public string ToolName { get; init; } = string.Empty;

    /// <summary>
    /// Indicates whether execution was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Strongly-typed output object. Type varies by tool.
    /// </summary>
    public object? Output { get; init; }

    /// <summary>
    /// Execution metrics as an object (typically ToolExecutionMetrics).
    /// </summary>
    public object? Metrics { get; init; }

    /// <summary>
    /// Error message if execution failed.
    /// </summary>
    public string? Error { get; init; }
}
