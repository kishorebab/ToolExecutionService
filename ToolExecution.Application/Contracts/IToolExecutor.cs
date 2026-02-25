namespace ToolExecution.Application.Contracts;

using ToolExecution.Domain.Models;

/// <summary>
/// Core interface for executing tools from the registry.
/// Implements the execution pipeline with validation, timeouts, and error handling.
/// </summary>
public interface IToolExecutor
{
    /// <summary>
    /// Execute a tool by name with the given input parameters.
    /// </summary>
    /// <param name="toolName">Name of the tool to execute.</param>
    /// <param name="input">Input parameters for the tool.</param>
    /// <param name="traceId">Optional trace ID for distributed tracing (generated if not provided).</param>
    /// <param name="correlationId">Optional correlation ID for grouping related requests.</param>
    /// <param name="requestedBy">Optional identifier of who requested the execution.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Standardized ToolResponse (never throws, always returns response).</returns>
    Task<ToolResponse> ExecuteAsync(
        string toolName,
        IDictionary<string, object?> input,
        string? traceId = null,
        string? correlationId = null,
        string? requestedBy = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all registered tool definitions.
    /// </summary>
    /// <returns>Collection of tool definitions.</returns>
    IReadOnlyCollection<ToolDefinition> ListTools();

    /// <summary>
    /// Get a specific tool definition by name.
    /// </summary>
    /// <param name="toolName">Name of the tool.</param>
    /// <returns>Tool definition if found, null otherwise.</returns>
    ToolDefinition? GetTool(string toolName);
}
