using System.Diagnostics;
using System.Text.Json.Nodes;
using Polly;
using Polly.Retry;
using ToolExecution.Application.Contracts;
using ToolExecution.Domain.Models;
using ToolExecution.Infrastructure.Clients;
using ToolExecution.Infrastructure.Policies;

namespace ToolExecution.Application.Services;

public class ToolExecutionOrchestratorService : IToolExecutionOrchestratorService
{
    private readonly IKubernetesClient _k8s;
    private readonly AsyncRetryPolicy _retry;
    private readonly ActivitySource _activitySource = new("ToolExecutionOrchestrator");

    public ToolExecutionOrchestratorService(IKubernetesClient k8s, PolicyProvider policies)
    {
        _k8s = k8s;
        _retry = policies.DefaultRetryPolicy;
    }

    public async Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity(call.ToolName) ?? null;
        activity?.SetTag("traceId", call.TraceId);

        if (string.IsNullOrWhiteSpace(call.ToolName))
        {
            return new ToolResult
            {
                TraceId = call.TraceId,
                SessionId = call.SessionId,
                ToolName = call.ToolName,
                Success = false,
                Error = "toolName is required"
            };
        }

        try
        {
            return await _retry.ExecuteAsync(async ct =>
            {
                // Dispatch to k8s client based on tool name
                return call.ToolName.ToLowerInvariant() switch
                {
                    "get-pod-logs" => await RemoteCallAsync(() => _k8s.GetPodLogsAsync(call.Arguments, ct)),
                    "list-pods" => await RemoteCallAsync(() => _k8s.ListPodsAsync(call.Arguments, ct)),
                    "get-deployments" => await RemoteCallAsync(() => _k8s.GetDeploymentsAsync(call.Arguments, ct)),
                    "get-resource-usage" => await RemoteCallAsync(() => _k8s.GetResourceUsageAsync(call.Arguments, ct)),
                    "execute-command" => await RemoteCallAsync(() => _k8s.ExecuteCommandAsync(call.Arguments, ct)),
                    _ => new ToolResult
                    {
                        TraceId = call.TraceId,
                        SessionId = call.SessionId,
                        ToolName = call.ToolName,
                        Success = false,
                        Error = $"Unknown tool: {call.ToolName}"
                    }
                };
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);
            return new ToolResult
            {
                TraceId = call.TraceId,
                SessionId = call.SessionId,
                ToolName = call.ToolName,
                Success = false,
                Error = ex.Message
            };
        }
    }

    private async Task<ToolResult> RemoteCallAsync(Func<Task<ToolResult>> call)
    {
        using var activity = _activitySource.StartActivity("k8s-call");
        try
        {
            var result = await call();
            activity?.SetTag("success", result.Success);
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetTag("error", true);
            activity?.SetTag("error.message", ex.Message);
            throw;
        }
    }
}
