namespace ToolExecution.Domain.Models;

/// <summary>
/// Metrics for a tool execution request.
/// </summary>
public class ToolExecutionMetrics
{
    /// <summary>
    /// Latency in milliseconds for the tool execution.
    /// </summary>
    public long LatencyMs { get; set; }
}
