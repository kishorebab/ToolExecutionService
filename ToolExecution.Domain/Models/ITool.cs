namespace ToolExecution.Domain.Models;

/// <summary>
/// Base interface for all executable tools.
/// Implementations must be:
/// - Stateless (no instance state)
/// - Thread-safe
/// - Fully async
/// - Idempotent if marked as such in ToolDefinition
/// </summary>
public interface ITool
{
    /// <summary>
    /// Metadata describing this tool.
    /// </summary>
    ToolDefinition Definition { get; }

    /// <summary>
    /// Execute the tool with the given request.
    /// </summary>
    /// <param name="request">Tool execution request with input parameters.</param>
    /// <param name="cancellationToken">Cancellation token (honored immediately).</param>
    /// <returns>
    /// ToolResponse with:
    /// - Status indicating success/failure
    /// - Output parameters if successful
    /// - ErrorMessage if failed or validation error
    /// - ExecutionTimeMs with elapsed time
    /// - TraceId and CorrelationId propagated from request
    /// </returns>
    Task<ToolResponse> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken = default);
}
