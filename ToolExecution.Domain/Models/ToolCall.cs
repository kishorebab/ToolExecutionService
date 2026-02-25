namespace ToolExecution.Domain.Models;

/// <summary>
/// Internal domain model for tool execution calls.
/// This is used internally for processing and tracing.
/// </summary>
public sealed record ToolCall
{
    /// <summary>
    /// Unique trace ID for distributed tracing.
    /// </summary>
    public string TraceId { get; init; } = string.Empty;

    /// <summary>
    /// Session/request ID.
    /// </summary>
    public string SessionId { get; init; } = string.Empty;

    /// <summary>
    /// Name of the tool to execute.
    /// </summary>
    public string ToolName { get; init; } = string.Empty;

    /// <summary>
    /// Strongly-typed arguments object. Type varies by tool.
    /// </summary>
    public object? Arguments { get; init; }
}
