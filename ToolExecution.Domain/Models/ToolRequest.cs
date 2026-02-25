namespace ToolExecution.Domain.Models;

/// <summary>
/// Request to execute a tool via the Tool Execution Engine.
/// Strongly typed request with input parameters as Dictionary.
/// </summary>
public class ToolRequest
{
    /// <summary>
    /// Name of the tool to execute.
    /// Must match a registered ITool.Definition.Name.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Unique identifier for distributed tracing and correlation.
    /// Generated if not provided.
    /// </summary>
    public required string TraceId { get; init; }

    /// <summary>
    /// Correlation ID for grouping related requests.
    /// Optional but recommended for request tracing.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Input parameters for the tool.
    /// Keys must match tool's InputSchema properties.
    /// Values must be JSON-serializable.
    /// </summary>
    public IDictionary<string, object?> Input { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Optional identifier of who requested the execution.
    /// Can be user ID, service name, etc.
    /// Used for auditing and authorization (future).
    /// </summary>
    public string? RequestedBy { get; init; }

    /// <summary>
    /// When the request was created (ISO 8601 UTC).
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Additional context/metadata for the request.
    /// Can be used for debugging, feature flags, etc.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();
}
