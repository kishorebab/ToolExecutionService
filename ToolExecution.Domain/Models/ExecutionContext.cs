namespace ToolExecution.Domain.Models;

/// <summary>
/// Encapsulates the execution context for a tool invocation.
/// Provides distributed tracing, request correlation, and optional extensions.
/// </summary>
public class ExecutionContext
{
    /// <summary>
    /// Unique execution ID (GUID). Used for distributed tracing.
    /// Generated if not provided.
    /// </summary>
    public required string ExecutionId { get; init; }

    /// <summary>
    /// Plan ID (GUID, optional). Links this execution to a larger plan being orchestrated.
    /// Useful when multiple tools are executed as part of an agent plan.
    /// </summary>
    public string? PlanId { get; init; }

    /// <summary>
    /// Step ID within the plan (string or numeric). Identifies position in execution sequence.
    /// Example: "1", "step-fetch-logs", "0.1"
    /// </summary>
    public string? StepId { get; init; }

    /// <summary>
    /// Correlation ID for grouping related requests across services.
    /// Aids in debugging and distributed tracing.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Optional identifier of who/what requested the execution.
    /// Can be user ID, service name, agent name, etc.
    /// Used for auditing and authorization.
    /// </summary>
    public string? RequestedBy { get; init; }

    /// <summary>
    /// Optional namespace (for Kubernetes operations or logical scoping).
    /// Can be reused across multiple tool calls in same context.
    /// </summary>
    public string? Namespace { get; init; }

    /// <summary>
    /// Timestamp when execution context was created (UTC, ISO 8601).
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Additional metadata/custom values for future extensibility.
    /// Can store agent-specific data, feature flags, etc.
    /// Never serialized to external clients.
    /// </summary>
    public IDictionary<string, object?> Metadata { get; init; } = new Dictionary<string, object?>();

    /// <summary>
    /// Creates an execution context with minimum required fields.
    /// </summary>
    /// <param name="executionId">Unique execution ID. Auto-generated as GUID if null/empty.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <returns>New ExecutionContext instance.</returns>
    public static ExecutionContext Create(
        string? executionId = null,
        string? correlationId = null)
    {
        return new ExecutionContext
        {
            ExecutionId = executionId ?? Guid.NewGuid().ToString("N"),
            CorrelationId = correlationId,
        };
    }

    /// <summary>
    /// Creates an execution context for a tool within a larger plan.
    /// </summary>
    /// <param name="planId">Unique plan ID.</param>
    /// <param name="stepId">Step ID within the plan.</param>
    /// <param name="correlationId">Optional correlation ID.</param>
    /// <returns>New ExecutionContext instance.</returns>
    public static ExecutionContext CreateForPlanStep(
        string planId,
        string stepId,
        string? correlationId = null)
    {
        return new ExecutionContext
        {
            ExecutionId = Guid.NewGuid().ToString("N"),
            PlanId = planId,
            StepId = stepId,
            CorrelationId = correlationId,
        };
    }
}
