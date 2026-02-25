namespace ToolExecution.Domain.Models;

/// <summary>
/// Generic wrapper for strongly-typed tool execution requests.
/// </summary>
/// <typeparam name="TArguments">The type of arguments specific to this tool.</typeparam>
public class ToolExecutionRequest<TArguments> where TArguments : class
{
    /// <summary>
    /// Unique trace ID for distributed tracing and logging correlation.
    /// If not provided, a new one will be generated.
    /// </summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Session/request ID for grouping related tool executions.
    /// </summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the tool to execute.
    /// Examples: "get-pod-logs", "list-pods", "get-deployments", "get-resource-usage", "execute-command"
    /// </summary>
    public string ToolName { get; set; } = string.Empty;

    /// <summary>
    /// Strongly-typed arguments for the specific tool.
    /// </summary>
    public TArguments Arguments { get; set; } = default!;
}
