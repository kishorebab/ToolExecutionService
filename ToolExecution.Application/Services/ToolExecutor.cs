using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ToolExecution.Application.Contracts;
using ToolExecution.Domain.Models;

namespace ToolExecution.Application.Services;

/// <summary>
/// Generic tool executor service that handles execution pipeline for any registered tool.
/// Implements the core execution logic with validation, timeout, and error handling.
/// 
/// Responsibilities:
/// - Resolve tool from registry
/// - Validate input against JSON schema
/// - Execute tool with timeout
/// - Measure execution time
/// - Handle exceptions safely
/// - Return standardized response
/// </summary>
public class ToolExecutor : IToolExecutor
{
    private readonly IToolRegistry _registry;
    private readonly ILogger<ToolExecutor> _logger;
    private readonly ActivitySource _activitySource = new("ToolExecution.ToolExecutor");

    public ToolExecutor(
        IToolRegistry registry,
        ILogger<ToolExecutor> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Execute a tool by name with the given input parameters.
    /// </summary>
    /// <remarks>
    /// This method handles the complete execution pipeline:
    /// 1. Resolve tool from registry
    /// 2. Check if tool is enabled
    /// 3. Validate input against JSON schema
    /// 4. Execute tool with timeout
    /// 5. Measure execution time
    /// 6. Return standardized response
    /// 
    /// Never throws exceptions. All errors are caught and returned in ToolResponse.
    /// </remarks>
    public async Task<ToolResponse> ExecuteAsync(
        string toolName,
        IDictionary<string, object?> input,
        string? traceId = null,
        string? correlationId = null,
        string? requestedBy = null,
        CancellationToken cancellationToken = default)
    {
        // Generate traceId if not provided
        traceId = traceId ?? Guid.NewGuid().ToString("N");

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Resolve tool from registry
            var tool = _registry.Get(toolName);

            if (tool == null)
            {
                _logger.LogWarning(
                    "Tool '{ToolName}' not found in registry (TraceId: {TraceId})",
                    toolName, traceId);

                stopwatch.Stop();
                return ToolResponse.CreateFailure(
                    traceId,
                    $"Tool '{toolName}' not found in registry.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.NotFound,
                    correlationId);
            }

            var definition = tool.Definition;

            // 2. Check if tool is enabled
            if (!definition.IsEnabled)
            {
                _logger.LogWarning(
                    "Tool '{ToolName}' is disabled (TraceId: {TraceId})",
                    toolName, traceId);

                stopwatch.Stop();
                return ToolResponse.CreateFailure(
                    traceId,
                    $"Tool '{toolName}' is disabled and cannot be executed.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.Disabled,
                    correlationId);
            }

            // 3. Validate input against JSON schema
            var validationError = ValidateInput(input, definition.InputSchema);
            if (validationError != null)
            {
                _logger.LogWarning(
                    "Input validation failed for tool '{ToolName}': {Error} (TraceId: {TraceId})",
                    toolName, validationError, traceId);

                stopwatch.Stop();
                return ToolResponse.CreateFailure(
                    traceId,
                    $"Input validation failed: {validationError}",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.ValidationError,
                    correlationId);
            }

            _logger.LogInformation(
                "Executing tool '{ToolName}' with input parameters (TraceId: {TraceId}, CorrelationId: {CorrelationId})",
                toolName, traceId, correlationId);

            // 4. Execute tool with timeout
            using var activity = _activitySource.StartActivity($"ExecuteTool:{toolName}");
            activity?.SetTag("tool.name", toolName);
            activity?.SetTag("trace.id", traceId);
            if (correlationId != null)
                activity?.SetTag("correlation.id", correlationId);

            var request = new ToolRequest
            {
                ToolName = toolName,
                TraceId = traceId,
                CorrelationId = correlationId,
                Input = input,
                RequestedBy = requestedBy,
                Metadata = new Dictionary<string, object?>()
            };

            // Create cancellation token with timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(definition.TimeoutSeconds));

            ToolResponse? response = null;

            try
            {
                var toolResponse = await tool.ExecuteAsync(request, cts.Token);
                response = toolResponse;

                _logger.LogInformation(
                    "Tool '{ToolName}' execution completed successfully in {ExecutionTimeMs}ms (TraceId: {TraceId})",
                    toolName, toolResponse.ExecutionTimeMs, traceId);
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // Timeout occurred
                _logger.LogError(
                    "Tool '{ToolName}' execution timed out after {TimeoutSeconds}s (TraceId: {TraceId})",
                    toolName, definition.TimeoutSeconds, traceId);

                stopwatch.Stop();
                response = ToolResponse.CreateFailure(
                    traceId,
                    $"Tool '{toolName}' execution exceeded timeout of {definition.TimeoutSeconds} seconds.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.Timeout,
                    correlationId);
            }
            catch (OperationCanceledException)
            {
                // External cancellation
                _logger.LogInformation(
                    "Tool '{ToolName}' execution was cancelled (TraceId: {TraceId})",
                    toolName, traceId);

                stopwatch.Stop();
                response = ToolResponse.CreateFailure(
                    traceId,
                    $"Tool '{toolName}' execution was cancelled.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.Cancelled,
                    correlationId);
            }
            catch (Exception ex) when (response == null)
            {
                // Unexpected error during execution
                _logger.LogError(
                    ex,
                    "Unexpected error executing tool '{ToolName}': {ErrorMessage} (TraceId: {TraceId})",
                    toolName, ex.Message, traceId);

                activity?.SetTag("error.type", ex.GetType().Name);
                activity?.SetTag("error.message", ex.Message);

                stopwatch.Stop();
                response = ToolResponse.CreateFailure(
                    traceId,
                    $"Tool '{toolName}' execution failed: {ex.Message}",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.Failed,
                    correlationId);
            }

            stopwatch.Stop();

            // Contract guarantee: output must never be null when success is true.
            // The AI Agent's ToolResultItem requires output to be present to generate a diagnosis.
            if (response.IsSuccess && response.Output == null)
            {
                throw new InvalidOperationException(
                    $"Tool '{toolName}' returned success but output was null. " +
                    "All tools must return a non-null output object on success.");
            }

            return response;
        }
        catch (Exception ex)
        {
            // Unhandled exception in execution pipeline
            _logger.LogError(
                ex,
                "Critical error in tool execution pipeline for '{ToolName}': {ErrorMessage} (TraceId: {TraceId})",
                toolName, ex.Message, traceId);

            stopwatch.Stop();
            return ToolResponse.CreateFailure(
                traceId,
                $"Fatal error: {ex.Message}",
                stopwatch.ElapsedMilliseconds,
                ExecutionStatus.Failed,
                correlationId);
        }
    }

    /// <summary>
    /// List all registered tool definitions.
    /// </summary>
    public IReadOnlyCollection<ToolDefinition> ListTools()
    {
        _logger.LogInformation("Listing all registered tools (Count: {Count})", _registry.Count);
        return _registry.ListToolDefinitions();
    }

    /// <summary>
    /// Get a specific tool definition by name.
    /// </summary>
    public ToolDefinition? GetTool(string toolName)
    {
        var tool = _registry.Get(toolName);
        if (tool == null)
        {
            _logger.LogDebug("Tool '{ToolName}' not found in registry", toolName);
            return null;
        }

        _logger.LogDebug("Retrieved tool '{ToolName}' definition from registry", toolName);
        return tool.Definition;
    }

    /// <summary>
    /// Validate input parameters against the tool's JSON schema.
    /// </summary>
    /// <returns>Error message if validation fails, null if valid.</returns>
    private static string? ValidateInput(IDictionary<string, object?> input, string jsonSchema)
    {
        try
        {
            // Parse the JSON schema to check for required fields
            using var schemaDoc = JsonDocument.Parse(jsonSchema);
            var schemaRoot = schemaDoc.RootElement;

            // Check if schema has required fields
            if (schemaRoot.TryGetProperty("required", out var requiredElement) &&
                requiredElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var requiredField in requiredElement.EnumerateArray())
                {
                    if (requiredField.ValueKind == JsonValueKind.String)
                    {
                        var fieldName = requiredField.GetString();
                        if (string.IsNullOrEmpty(fieldName) || !input.ContainsKey(fieldName) || input[fieldName] == null)
                        {
                            return $"Required field '{fieldName}' is missing or null.";
                        }
                    }
                }
            }

            // Basic type validation if properties are defined
            if (schemaRoot.TryGetProperty("properties", out var propertiesElement) &&
                propertiesElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in propertiesElement.EnumerateObject())
                {
                    var fieldName = property.Name;
                    if (input.TryGetValue(fieldName, out var value) && value != null)
                    {
                        // Basic type checking
                        if (property.Value.TryGetProperty("type", out var typeElement))
                        {
                            var expectedType = typeElement.GetString();
                            var actualType = GetJsonType(value);

                            // Allow some type coercion
                            if (expectedType == "number" && actualType != "number" && actualType != "integer")
                            {
                                return $"Field '{fieldName}' must be a number, got {actualType}.";
                            }
                            else if (expectedType == "string" && actualType != "string")
                            {
                                // Strings can be coerced from other types, so be lenient
                            }
                            else if (expectedType == "array" && actualType != "array")
                            {
                                return $"Field '{fieldName}' must be an array, got {actualType}.";
                            }
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            // If validation itself fails, return generic error
            return $"Validation error: {ex.Message}";
        }
    }

    /// <summary>
    /// Determine JSON type of an object value.
    /// </summary>
    private static string GetJsonType(object? value)
    {
        return value switch
        {
            null => "null",
            bool => "boolean",
            string => "string",
            int or long or decimal or double or float => "number",
            System.Collections.IDictionary => "object",
            System.Collections.IEnumerable => "array",
            _ => "unknown"
        };
    }
}
