namespace ToolExecution.Application.Services;

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Domain = ToolExecution.Domain.Models;
using ToolExecution.Application.Contracts;
using ToolExecution.Domain.Models;

/// <summary>
/// Core tool execution service implementing the execution pipeline.
/// Handles: validation, tracing, timeouts, error handling, structured logging.
/// </summary>
public class ToolExecutionService : IToolExecutor
{
    private readonly Domain.IToolRegistry _registry;
    private readonly ILogger<ToolExecutionService> _logger;

    public ToolExecutionService(
        Domain.IToolRegistry registry,
        ILogger<ToolExecutionService> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ToolResponse> ExecuteAsync(
        string toolName,
        IDictionary<string, object?> input,
        string? traceId = null,
        string? correlationId = null,
        string? requestedBy = null,
        CancellationToken cancellationToken = default)
    {
        // Generate TraceId if not provided
        var finalTraceId = string.IsNullOrWhiteSpace(traceId)
            ? Guid.NewGuid().ToString("N")
            : traceId;

        using var activity = new Activity("ExecuteTool")
            .SetTag("tool.name", toolName)
            .SetTag("trace.id", finalTraceId)
            .SetTag("correlation.id", correlationId)
            .Start();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Log execution start
            _logger.LogInformation(
                "Tool execution started: {ToolName}, TraceId: {TraceId}",
                toolName, finalTraceId);

            // Step 1: Validate tool exists
            var tool = _registry.Get(toolName);
            if (tool == null)
            {
                _logger.LogWarning(
                    "Tool not found: {ToolName}, TraceId: {TraceId}",
                    toolName, finalTraceId);

                stopwatch.Stop();
                return ToolResponse.CreateFailure(
                    finalTraceId,
                    $"Tool '{toolName}' not found in registry.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.NotFound,
                    correlationId);
            }

            var toolDefinition = tool.Definition;

            // Step 2: Validate tool is enabled
            if (!toolDefinition.IsEnabled)
            {
                _logger.LogWarning(
                    "Tool execution attempted on disabled tool: {ToolName}, TraceId: {TraceId}",
                    toolName, finalTraceId);

                stopwatch.Stop();
                return ToolResponse.CreateFailure(
                    finalTraceId,
                    $"Tool '{toolName}' is disabled.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.Disabled,
                    correlationId);
            }

            // Step 3: Create execution request
            var request = new ToolRequest
            {
                ToolName = toolName,
                TraceId = finalTraceId,
                CorrelationId = correlationId,
                Input = input ?? new Dictionary<string, object?>(),
                RequestedBy = requestedBy,
                Metadata = new Dictionary<string, object?>
                {
                    { "version", toolDefinition.Version },
                    { "timestamp", DateTime.UtcNow }
                }
            };

            // Step 4: Create execution context
            var context = new ToolExecutionContext
            {
                Request = request,
                ToolDefinition = toolDefinition,
                CancellationToken = cancellationToken
            };

            // Step 5: Setup timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(toolDefinition.TimeoutSeconds));

            _logger.LogDebug(
                "Executing tool: {ToolName}, Timeout: {TimeoutSeconds}s, TraceId: {TraceId}",
                toolName, toolDefinition.TimeoutSeconds, finalTraceId);

            // Step 6: Execute tool
            ToolResponse response;
            try
            {
                response = await tool.ExecuteAsync(request, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Timeout occurred
                _logger.LogError(
                    "Tool execution timed out: {ToolName}, Timeout: {TimeoutSeconds}s, TraceId: {TraceId}",
                    toolName, toolDefinition.TimeoutSeconds, finalTraceId);

                stopwatch.Stop();
                response = ToolResponse.CreateFailure(
                    finalTraceId,
                    $"Tool execution exceeded timeout of {toolDefinition.TimeoutSeconds} seconds.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.Timeout,
                    correlationId);
            }
            catch (OperationCanceledException)
            {
                // Execution was cancelled by caller
                _logger.LogWarning(
                    "Tool execution was cancelled: {ToolName}, TraceId: {TraceId}",
                    toolName, finalTraceId);

                stopwatch.Stop();
                response = ToolResponse.CreateFailure(
                    finalTraceId,
                    "Execution was cancelled by caller.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.Cancelled,
                    correlationId);
            }
            catch (Exception ex)
            {
                // Unexpected error
                _logger.LogError(
                    ex,
                    "Tool execution failed with exception: {ToolName}, TraceId: {TraceId}, Message: {Message}",
                    toolName, finalTraceId, ex.Message);

                stopwatch.Stop();
                response = ToolResponse.CreateFailure(
                    finalTraceId,
                    $"Tool execution failed: {ex.Message}",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.Failed,
                    correlationId);
            }

            stopwatch.Stop();

            // Step 7: Log result
            if (response.IsSuccess)
            {
                _logger.LogInformation(
                    "Tool execution completed successfully: {ToolName}, Duration: {DurationMs}ms, TraceId: {TraceId}",
                    toolName, response.ExecutionTimeMs, finalTraceId);
            }
            else
            {
                _logger.LogWarning(
                    "Tool execution completed with non-success status: {ToolName}, Status: {Status}, " +
                    "Duration: {DurationMs}ms, TraceId: {TraceId}",
                    toolName, response.Status, response.ExecutionTimeMs, finalTraceId);
            }

            activity?.SetTag("execution.status", response.Status.ToString());
            activity?.SetTag("execution.time_ms", response.ExecutionTimeMs);

            return response;
        }
        finally
        {
            stopwatch.Stop();
            activity?.Stop();
        }
    }

    public IReadOnlyCollection<ToolDefinition> ListTools()
    {
        return _registry.ListToolDefinitions();
    }

    public ToolDefinition? GetTool(string toolName)
    {
        var tool = _registry.Get(toolName);
        return tool?.Definition;
    }
}
