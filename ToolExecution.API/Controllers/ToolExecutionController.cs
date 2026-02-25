using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ToolExecution.Application.Contracts;
using ToolExecution.Domain.Models;

namespace ToolExecution.API.Controllers;

/// <summary>
/// REST API controller for tool execution endpoints.
/// All endpoints accept strongly-typed requests and return strongly-typed responses.
/// Each endpoint supports distributed tracing via the traceId field.
/// </summary>
[ApiController]
[Route("api/tools")]
public class ToolExecutionController : ControllerBase
{
    private readonly IToolExecutorService _executor;
    private readonly ActivitySource _activitySource = new("ToolExecution.API");

    public ToolExecutionController(IToolExecutorService executor)
    {
        _executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    /// <summary>
    /// Retrieves pod logs from a Kubernetes pod.
    /// </summary>
    /// <param name="request">The strongly-typed request containing namespace, pod name, and log parameters.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A strongly-typed response containing the pod logs.</returns>
    [HttpPost("get-pod-logs")]
    [ProducesResponseType(typeof(ToolExecutionResponse<GetPodLogsOutput>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ToolExecutionResponse<GetPodLogsOutput>>> GetPodLogs(
        [FromBody] ToolExecutionRequest<GetPodLogsArguments> request,
        CancellationToken cancellationToken)
    {
        var response = await ExecuteToolWithTracingAsync(
            () => _executor.GetPodLogsAsync(request, cancellationToken),
            request.TraceId);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Lists all pods in a Kubernetes namespace.
    /// </summary>
    /// <param name="request">The strongly-typed request containing the namespace.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A strongly-typed response containing the list of pods.</returns>
    [HttpPost("list-pods")]
    [ProducesResponseType(typeof(ToolExecutionResponse<ListPodsOutput>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ToolExecutionResponse<ListPodsOutput>>> ListPods(
        [FromBody] ToolExecutionRequest<ListPodsArguments> request,
        CancellationToken cancellationToken)
    {
        var response = await ExecuteToolWithTracingAsync(
            () => _executor.ListPodsAsync(request, cancellationToken),
            request.TraceId);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Gets deployments in a Kubernetes namespace.
    /// </summary>
    /// <param name="request">The strongly-typed request containing the namespace.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A strongly-typed response containing the list of deployments.</returns>
    [HttpPost("get-deployments")]
    [ProducesResponseType(typeof(ToolExecutionResponse<GetDeploymentsOutput>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ToolExecutionResponse<GetDeploymentsOutput>>> GetDeployments(
        [FromBody] ToolExecutionRequest<GetDeploymentsArguments> request,
        CancellationToken cancellationToken)
    {
        var response = await ExecuteToolWithTracingAsync(
            () => _executor.GetDeploymentsAsync(request, cancellationToken),
            request.TraceId);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Gets resource usage metrics from a Kubernetes namespace.
    /// </summary>
    /// <param name="request">The strongly-typed request containing the namespace and optional pod name.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A strongly-typed response containing resource usage information.</returns>
    [HttpPost("get-resource-usage")]
    [ProducesResponseType(typeof(ToolExecutionResponse<GetResourceUsageOutput>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ToolExecutionResponse<GetResourceUsageOutput>>> GetResourceUsage(
        [FromBody] ToolExecutionRequest<GetResourceUsageArguments> request,
        CancellationToken cancellationToken)
    {
        var response = await ExecuteToolWithTracingAsync(
            () => _executor.GetResourceUsageAsync(request, cancellationToken),
            request.TraceId);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Executes a command in a Kubernetes pod.
    /// </summary>
    /// <param name="request">The strongly-typed request containing namespace, pod name, and command.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A strongly-typed response containing stdout, stderr, and exit code.</returns>
    [HttpPost("execute-command")]
    [ProducesResponseType(typeof(ToolExecutionResponse<ExecuteCommandOutput>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ToolExecutionResponse<ExecuteCommandOutput>>> ExecuteCommand(
        [FromBody] ToolExecutionRequest<ExecuteCommandArguments> request,
        CancellationToken cancellationToken)
    {
        var response = await ExecuteToolWithTracingAsync(
            () => _executor.ExecuteCommandAsync(request, cancellationToken),
            request.TraceId);

        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Helper method to execute tool operations with OpenTelemetry tracing support.
    /// Ensures traceId is propagated and activities are created for distributed tracing.
    /// </summary>
    private async Task<T> ExecuteToolWithTracingAsync<T>(Func<Task<T>> operation, string? providedTraceId) where T : class
    {
        var traceId = providedTraceId ?? Request.Headers["traceId"].FirstOrDefault() ?? Guid.NewGuid().ToString();

        using var activity = _activitySource.StartActivity("ToolExecution");
        activity?.SetTag("http.method", Request.Method);
        activity?.SetTag("http.url", Request.Path);
        activity?.SetTag("traceId", traceId);

        try
        {
            return await operation.Invoke();
        }
        catch (Exception ex)
        {
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
    }
}
