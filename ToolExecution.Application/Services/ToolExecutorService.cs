using System.Diagnostics;
using Polly;
using Polly.Retry;
using ToolExecution.Application.Contracts;
using ToolExecution.Domain.Models;
using ToolExecution.Infrastructure.Clients;
using ToolExecution.Infrastructure.Policies;

namespace ToolExecution.Application.Services;

/// <summary>
/// Strongly-typed tool execution service that handles specific tool requests with proper typing.
/// Implements Clean Architecture by orchestrating between infrastructure clients and application contracts.
/// </summary>
public class ToolExecutorService : IToolExecutorService
{
    private readonly IKubernetesClient _kubernetesClient;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly ActivitySource _activitySource = new("ToolExecution.ToolExecutor");

    public ToolExecutorService(IKubernetesClient kubernetesClient, PolicyProvider policyProvider)
    {
        _kubernetesClient = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));
        _retryPolicy = policyProvider?.DefaultRetryPolicy ?? throw new ArgumentNullException(nameof(policyProvider));
    }

    public async Task<ToolExecutionResponse<GetPodLogsOutput>> GetPodLogsAsync(
        ToolExecutionRequest<GetPodLogsArguments> request,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetPodLogs");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            SetActivityTags(activity, request.TraceId);

            var result = await _retryPolicy.ExecuteAsync(async ct =>
            {
                return await _kubernetesClient.GetPodLogsAsync(request.Arguments, ct);
            }, cancellationToken);

            stopwatch.Stop();

            return new ToolExecutionResponse<GetPodLogsOutput>
            {
                TraceId = request.TraceId,
                SessionId = request.SessionId,
                ToolName = request.ToolName,
                Success = result.Success,
                Output = result.Output as GetPodLogsOutput,
                Error = result.Error,
                Metrics = new ToolExecutionMetrics { LatencyMs = stopwatch.ElapsedMilliseconds }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);

            return new ToolExecutionResponse<GetPodLogsOutput>
            {
                TraceId = request.TraceId,
                SessionId = request.SessionId,
                ToolName = request.ToolName,
                Success = false,
                Error = ex.Message,
                Metrics = new ToolExecutionMetrics { LatencyMs = stopwatch.ElapsedMilliseconds }
            };
        }
    }

    public async Task<ToolExecutionResponse<ListPodsOutput>> ListPodsAsync(
        ToolExecutionRequest<ListPodsArguments> request,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("ListPods");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            SetActivityTags(activity, request.TraceId);

            var result = await _retryPolicy.ExecuteAsync(async ct =>
            {
                return await _kubernetesClient.ListPodsAsync(request.Arguments, ct);
            }, cancellationToken);

            stopwatch.Stop();

            return new ToolExecutionResponse<ListPodsOutput>
            {
                TraceId = request.TraceId,
                SessionId = request.SessionId,
                ToolName = request.ToolName,
                Success = result.Success,
                Output = result.Output as ListPodsOutput,
                Error = result.Error,
                Metrics = new ToolExecutionMetrics { LatencyMs = stopwatch.ElapsedMilliseconds }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);

            return new ToolExecutionResponse<ListPodsOutput>
            {
                TraceId = request.TraceId,
                SessionId = request.SessionId,
                ToolName = request.ToolName,
                Success = false,
                Error = ex.Message,
                Metrics = new ToolExecutionMetrics { LatencyMs = stopwatch.ElapsedMilliseconds }
            };
        }
    }

    public async Task<ToolExecutionResponse<GetDeploymentsOutput>> GetDeploymentsAsync(
        ToolExecutionRequest<GetDeploymentsArguments> request,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetDeployments");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            SetActivityTags(activity, request.TraceId);

            var result = await _retryPolicy.ExecuteAsync(async ct =>
            {
                return await _kubernetesClient.GetDeploymentsAsync(request.Arguments, ct);
            }, cancellationToken);

            stopwatch.Stop();

            return new ToolExecutionResponse<GetDeploymentsOutput>
            {
                TraceId = request.TraceId,
                SessionId = request.SessionId,
                ToolName = request.ToolName,
                Success = result.Success,
                Output = result.Output as GetDeploymentsOutput,
                Error = result.Error,
                Metrics = new ToolExecutionMetrics { LatencyMs = stopwatch.ElapsedMilliseconds }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);

            return new ToolExecutionResponse<GetDeploymentsOutput>
            {
                TraceId = request.TraceId,
                SessionId = request.SessionId,
                ToolName = request.ToolName,
                Success = false,
                Error = ex.Message,
                Metrics = new ToolExecutionMetrics { LatencyMs = stopwatch.ElapsedMilliseconds }
            };
        }
    }

    public async Task<ToolExecutionResponse<GetResourceUsageOutput>> GetResourceUsageAsync(
        ToolExecutionRequest<GetResourceUsageArguments> request,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("GetResourceUsage");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            SetActivityTags(activity, request.TraceId);

            var result = await _retryPolicy.ExecuteAsync(async ct =>
            {
                return await _kubernetesClient.GetResourceUsageAsync(request.Arguments, ct);
            }, cancellationToken);

            stopwatch.Stop();

            return new ToolExecutionResponse<GetResourceUsageOutput>
            {
                TraceId = request.TraceId,
                SessionId = request.SessionId,
                ToolName = request.ToolName,
                Success = result.Success,
                Output = result.Output as GetResourceUsageOutput,
                Error = result.Error,
                Metrics = new ToolExecutionMetrics { LatencyMs = stopwatch.ElapsedMilliseconds }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);

            return new ToolExecutionResponse<GetResourceUsageOutput>
            {
                TraceId = request.TraceId,
                SessionId = request.SessionId,
                ToolName = request.ToolName,
                Success = false,
                Error = ex.Message,
                Metrics = new ToolExecutionMetrics { LatencyMs = stopwatch.ElapsedMilliseconds }
            };
        }
    }

    public async Task<ToolExecutionResponse<ExecuteCommandOutput>> ExecuteCommandAsync(
        ToolExecutionRequest<ExecuteCommandArguments> request,
        CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("ExecuteCommand");
        var stopwatch = Stopwatch.StartNew();

        try
        {
            SetActivityTags(activity, request.TraceId);

            var result = await _retryPolicy.ExecuteAsync(async ct =>
            {
                return await _kubernetesClient.ExecuteCommandAsync(request.Arguments, ct);
            }, cancellationToken);

            stopwatch.Stop();

            return new ToolExecutionResponse<ExecuteCommandOutput>
            {
                TraceId = request.TraceId,
                SessionId = request.SessionId,
                ToolName = request.ToolName,
                Success = result.Success,
                Output = result.Output as ExecuteCommandOutput,
                Error = result.Error,
                Metrics = new ToolExecutionMetrics { LatencyMs = stopwatch.ElapsedMilliseconds }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);

            return new ToolExecutionResponse<ExecuteCommandOutput>
            {
                TraceId = request.TraceId,
                SessionId = request.SessionId,
                ToolName = request.ToolName,
                Success = false,
                Error = ex.Message,
                Metrics = new ToolExecutionMetrics { LatencyMs = stopwatch.ElapsedMilliseconds }
            };
        }
    }

    /// <summary>
    /// Sets common Activity tags for distributed tracing.
    /// </summary>
    private static void SetActivityTags(Activity? activity, string traceId)
    {
        if (activity == null) return;

        activity.SetTag("traceId", traceId);
        activity.SetTag("service", "ToolExecution");
    }
}
