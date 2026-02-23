using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Nodes;
using ToolExecution.Application.Contracts;
using ToolExecution.Domain.Models;

namespace ToolExecution.API.Controllers;

[ApiController]
[Route("api/tools")]
public class ToolExecutionController : ControllerBase
{
    private readonly IToolExecutionOrchestratorService _orchestrator;
    private readonly ActivitySource _activitySource = new("ToolExecution.API");

    public ToolExecutionController(IToolExecutionOrchestratorService orchestrator)
    {
        _orchestrator = orchestrator;
    }

    private async Task<IActionResult> HandleAsync(ToolCall call, CancellationToken cancellationToken)
    {
        var traceId = call.TraceId;
        if (string.IsNullOrWhiteSpace(traceId)) traceId = Request.Headers["traceId"].FirstOrDefault() ?? Guid.NewGuid().ToString();

        using var activity = _activitySource.StartActivity("ToolExecutionRequest");
        activity?.SetTag("traceId", traceId);

        var result = await _orchestrator.ExecuteAsync(call with { TraceId = traceId }, cancellationToken);

        var response = new
        {
            traceId = result.TraceId,
            sessionId = result.SessionId,
            toolName = result.ToolName,
            success = result.Success,
            output = result.Output,
            metrics = result.Metrics,
            error = result.Error
        };

        return Ok(response);
    }

    [HttpPost("get-pod-logs")]
    public Task<IActionResult> GetPodLogs([FromBody] JsonObject body, CancellationToken ct) => HandleAsync(Map(body, "get-pod-logs"), ct);

    [HttpPost("list-pods")]
    public Task<IActionResult> ListPods([FromBody] JsonObject body, CancellationToken ct) => HandleAsync(Map(body, "list-pods"), ct);

    [HttpPost("get-deployments")]
    public Task<IActionResult> GetDeployments([FromBody] JsonObject body, CancellationToken ct) => HandleAsync(Map(body, "get-deployments"), ct);

    [HttpPost("get-resource-usage")]
    public Task<IActionResult> GetResourceUsage([FromBody] JsonObject body, CancellationToken ct) => HandleAsync(Map(body, "get-resource-usage"), ct);

    [HttpPost("execute-command")]
    public Task<IActionResult> ExecuteCommand([FromBody] JsonObject body, CancellationToken ct) => HandleAsync(Map(body, "execute-command"), ct);

    private static ToolCall Map(JsonObject body, string toolName)
    {
        var traceId = body["traceId"]?.ToString() ?? string.Empty;
        var sessionId = body["sessionId"]?.ToString() ?? string.Empty;
        var args = body["arguments"] as JsonObject;
        return new ToolCall { TraceId = traceId, SessionId = sessionId, ToolName = toolName, Arguments = args };
    }
}
