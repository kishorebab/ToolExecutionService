namespace ToolExecution.Domain.Models;

/// <summary>
/// Generic wrapper for strongly-typed tool execution responses.
/// </summary>
/// <typeparam name="TOutput">The type of output specific to this tool.</typeparam>
public class ToolExecutionResponse<TOutput> where TOutput : class
{
    /// <summary>
    /// Unique trace ID matching the request for distributed tracing correlation.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Session/request ID from the original request.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the tool that was executed.
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the tool execution was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Strongly-typed output from the specific tool.
    /// Null if execution failed or tool returned no output.
    /// </summary>
    public TOutput? Output { get; set; }

    /// <summary>
    /// Execution metrics such as latency and performance data.
    /// </summary>
    public ToolExecutionMetrics Metrics { get; set; } = new();

    /// <summary>
    /// Error message if execution failed.
    /// Null if execution was successful.
    /// </summary>
    public string? Error { get; set; }
}
