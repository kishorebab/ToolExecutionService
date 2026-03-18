namespace ToolExecution.API.Controllers;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using ToolExecution.Application.Contracts;
using ToolExecution.Domain.Models;

/// <summary>
/// REST API endpoints for the Tool Execution Engine.
/// Allows: registering tools, listing tools, executing tools, health checks.
/// </summary>
[ApiController]
[Route("api/engine/tools")]
[Produces("application/json")]
public class ToolsController : ControllerBase
{
    private readonly IToolExecutor _executor;
    private readonly ILogger<ToolsController> _logger;

    public ToolsController(
        IToolExecutor executor,
        ILogger<ToolsController> logger)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// List all registered tools.
    /// </summary>
    /// <returns>Collection of tool definitions.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ListToolsResponse), 200)]
    public IActionResult ListTools()
    {
        _logger.LogInformation("Listing all tools");

        var toolDefinitions = _executor.ListTools();

        var response = new ListToolsResponse
        {
            Tools = toolDefinitions
                .Where(td => td.IsEnabled)
                .Select(td => new ToolDefinitionDto
                {
                    Name = td.Name,
                    Description = td.Description,
                    IsIdempotent = td.IsIdempotent,
                    TimeoutSeconds = td.TimeoutSeconds,
                    Parameters = td.Parameters
                }).ToList(),
            Count = toolDefinitions.Count(td => td.IsEnabled)
        };

        return Ok(response);
    }

    /// <summary>
    /// Get a specific tool by name.
    /// </summary>
    /// <param name="toolName">Name of the tool.</param>
    /// <returns>Tool definition if found.</returns>
    [HttpGet("{toolName}")]
    [ProducesResponseType(typeof(ToolDefinitionDto), 200)]
    [ProducesResponseType(404)]
    public IActionResult GetTool([FromRoute] string toolName)
    {
        _logger.LogInformation("Getting tool: {ToolName}", toolName);

        var toolDef = _executor.GetTool(toolName);
        if (toolDef == null)
        {
            _logger.LogWarning("Tool not found: {ToolName}", toolName);
            return NotFound(new { error = $"Tool '{toolName}' not found." });
        }

        var response = new ToolDefinitionDto
        {
            Name = toolDef.Name,
            Description = toolDef.Description,
            IsIdempotent = toolDef.IsIdempotent,
            TimeoutSeconds = toolDef.TimeoutSeconds,
            Parameters = toolDef.Parameters
        };

        return Ok(response);
    }

    /// <summary>
    /// Execute a tool by name with input parameters.
    /// </summary>
    /// <param name="toolName">Name of the tool to execute.</param>
    /// <param name="request">Execution request with input parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Standardized tool execution response.</returns>
    [HttpPost("{toolName}/execute")]
    [ProducesResponseType(typeof(ExecuteToolResponse), 200)]
    [ProducesResponseType(typeof(ExecuteToolResponse), 400)]
    [ProducesResponseType(typeof(ExecuteToolResponse), 408)]
    [ProducesResponseType(typeof(ExecuteToolResponse), 500)]
    public async Task<IActionResult> ExecuteToolAsync(
        [FromRoute][Required] string toolName,
        [FromBody][Required] ExecuteToolRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Executing tool: {ToolName}, TraceId: {TraceId}",
            toolName, request.TraceId);

        try
        {
            var response = await _executor.ExecuteAsync(
                toolName,
                request.Input ?? new Dictionary<string, object?>(),
                request.TraceId,
                request.CorrelationId,
                request.RequestedBy,
                cancellationToken);

            var statusCode = response.Status switch
            {
                ExecutionStatus.Success => StatusCodes.Status200OK,
                ExecutionStatus.ValidationError => StatusCodes.Status400BadRequest,
                ExecutionStatus.NotFound => StatusCodes.Status404NotFound,
                ExecutionStatus.Timeout => StatusCodes.Status408RequestTimeout,
                ExecutionStatus.Cancelled => StatusCodes.Status499ClientClosedRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            var apiResponse = new ExecuteToolResponse
            {
                TraceId = response.TraceId,
                CorrelationId = response.CorrelationId,
                Status = response.Status.ToString(),
                Success = response.IsSuccess,
                Output = response.Output,
                ErrorMessage = response.ErrorMessage,
                ExecutionTimeMs = response.ExecutionTimeMs,
                CompletedAt = response.CompletedAt
            };

            return StatusCode(statusCode, apiResponse);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Tool execution cancelled: {ToolName}, TraceId: {TraceId}",
                toolName, request.TraceId);

            var response = new ExecuteToolResponse
            {
                TraceId = request.TraceId ?? Guid.NewGuid().ToString("N"),
                CorrelationId = request.CorrelationId,
                Status = ExecutionStatus.Cancelled.ToString(),
                Success = false,
                ErrorMessage = "Execution was cancelled.",
                ExecutionTimeMs = 0,
                CompletedAt = DateTime.UtcNow
            };

            return StatusCode(StatusCodes.Status499ClientClosedRequest, response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex, "Unexpected error executing tool: {ToolName}, TraceId: {TraceId}",
                toolName, request.TraceId);

            var response = new ExecuteToolResponse
            {
                TraceId = request.TraceId ?? Guid.NewGuid().ToString("N"),
                CorrelationId = request.CorrelationId,
                Status = ExecutionStatus.Failed.ToString(),
                Success = false,
                ErrorMessage = "An unexpected error occurred during execution.",
                ExecutionTimeMs = 0,
                CompletedAt = DateTime.UtcNow
            };

            return StatusCode(StatusCodes.Status500InternalServerError, response);
        }
    }

    /// <summary>
    /// Health check for the tool engine.
    /// </summary>
    /// <returns>Health status.</returns>
    [HttpGet("/health/tools")]
    [ProducesResponseType(typeof(HealthResponse), 200)]
    public IActionResult HealthCheck()
    {
        var tools = _executor.ListTools();

        return Ok(new HealthResponse
        {
            Status = "healthy",
            RegisteredToolCount = tools.Count,
            EnabledToolCount = tools.Count(t => t.IsEnabled),
            Timestamp = DateTime.UtcNow
        });
    }
}

/// <summary>
/// DTO for listing tools response.
/// </summary>
public class ListToolsResponse
{
    public required List<ToolDefinitionDto> Tools { get; init; }
    public required int Count { get; init; }
}

/// <summary>
/// DTO for tool definition in API responses.
/// </summary>
public class ToolDefinitionDto
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public bool IsIdempotent { get; init; }
    public int TimeoutSeconds { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, ToolParameterDto>? Parameters { get; init; }
}

/// <summary>
/// DTO for tool execution request.
/// </summary>
public class ExecuteToolRequest
{
    /// <summary>
    /// Input parameters for the tool.
    /// Keys must match tool's InputSchema.
    /// </summary>
    public IDictionary<string, object?>? Input { get; init; }

    /// <summary>
    /// Optional trace ID for distributed tracing (auto-generated if missing).
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// Optional correlation ID for grouping related requests.
    /// </summary>
    public string? CorrelationId { get; init; }

    /// <summary>
    /// Optional identifier of who requested execution.
    /// </summary>
    public string? RequestedBy { get; init; }
}

/// <summary>
/// DTO for tool execution response.
/// </summary>
public class ExecuteToolResponse
{
    public required string TraceId { get; init; }
    public string? CorrelationId { get; init; }
    public required string Status { get; init; }
    public required bool Success { get; init; }
    public IDictionary<string, object?>? Output { get; init; }
    public string? ErrorMessage { get; init; }
    public required long ExecutionTimeMs { get; init; }
    public required DateTime CompletedAt { get; init; }
}

/// <summary>
/// DTO for health check response.
/// </summary>
public class HealthResponse
{
    public required string Status { get; init; }
    public required int RegisteredToolCount { get; init; }
    public required int EnabledToolCount { get; init; }
    public required DateTime Timestamp { get; init; }
}
