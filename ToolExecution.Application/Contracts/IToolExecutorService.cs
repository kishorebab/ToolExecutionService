using ToolExecution.Domain.Models;

namespace ToolExecution.Application.Contracts;

/// <summary>
/// Service for executing tool requests with strongly-typed arguments and outputs.
/// This is the public-facing service used by the API layer.
/// </summary>
public interface IToolExecutorService
{
    /// <summary>
    /// Executes a get-pod-logs tool request.
    /// </summary>
    Task<ToolExecutionResponse<GetPodLogsOutput>> GetPodLogsAsync(
        ToolExecutionRequest<GetPodLogsArguments> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a list-pods tool request.
    /// </summary>
    Task<ToolExecutionResponse<ListPodsOutput>> ListPodsAsync(
        ToolExecutionRequest<ListPodsArguments> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a get-deployments tool request.
    /// </summary>
    Task<ToolExecutionResponse<GetDeploymentsOutput>> GetDeploymentsAsync(
        ToolExecutionRequest<GetDeploymentsArguments> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a get-resource-usage tool request.
    /// </summary>
    Task<ToolExecutionResponse<GetResourceUsageOutput>> GetResourceUsageAsync(
        ToolExecutionRequest<GetResourceUsageArguments> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes an execute-command tool request.
    /// </summary>
    Task<ToolExecutionResponse<ExecuteCommandOutput>> ExecuteCommandAsync(
        ToolExecutionRequest<ExecuteCommandArguments> request,
        CancellationToken cancellationToken = default);
}
