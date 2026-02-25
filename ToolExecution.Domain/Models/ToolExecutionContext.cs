namespace ToolExecution.Domain.Models;

/// <summary>
/// Context for a tool execution.
/// Passed through the execution pipeline to provide tracing and metadata.
/// </summary>
public class ToolExecutionContext
{
    /// <summary>
    /// The tool request being executed.
    /// </summary>
    public required ToolRequest Request { get; init; }

    /// <summary>
    /// The tool definition for the requested tool.
    /// </summary>
    public required ToolDefinition ToolDefinition { get; init; }

    /// <summary>
    /// UTC timestamp when execution started.
    /// </summary>
    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Cancellation token for the execution.
    /// Honored throughout the pipeline.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;

    /// <summary>
    /// Optional custom state/metadata for extension points.
    /// Can be used by middleware or plugins.
    /// </summary>
    public IDictionary<string, object?> State { get; } = new Dictionary<string, object?>();

    /// <summary>
    /// Get elapsed time since execution started.
    /// </summary>
    public TimeSpan Elapsed => DateTime.UtcNow - StartedAt;

    /// <summary>
    /// Get remaining time before timeout.
    /// Negative if already timed out.
    /// </summary>
    public TimeSpan TimeRemaining => TimeSpan.FromSeconds(ToolDefinition.TimeoutSeconds) - Elapsed;

    /// <summary>
    /// Check if execution has exceeded timeout.
    /// </summary>
    public bool IsTimedOut => TimeRemaining <= TimeSpan.Zero;
}
