using ToolExecution.Domain.Models;

namespace ToolExecution.Infrastructure.Clients;

/// <summary>
/// Interface for Kubernetes client operations with strongly-typed arguments and outputs.
/// All methods support cancellation tokens for cooperative cancellation.
/// </summary>
public interface IKubernetesClient
{
    /// <summary>
    /// Retrieves pod logs from a Kubernetes pod with strongly-typed arguments.
    /// </summary>
    /// <param name="args">The strongly-typed arguments containing namespace, pod name, container name, and tail lines.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A ToolResult with GetPodLogsOutput containing the list of log lines.</returns>
    Task<ToolResult> GetPodLogsAsync(GetPodLogsArguments args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists pods in a Kubernetes namespace with strongly-typed arguments.
    /// </summary>
    /// <param name="args">The strongly-typed arguments containing the namespace.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A ToolResult with ListPodsOutput containing the list of pods.</returns>
    Task<ToolResult> ListPodsAsync(ListPodsArguments args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets deployments in a Kubernetes namespace with strongly-typed arguments.
    /// </summary>
    /// <param name="args">The strongly-typed arguments containing the namespace.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A ToolResult with GetDeploymentsOutput containing the list of deployments.</returns>
    Task<ToolResult> GetDeploymentsAsync(GetDeploymentsArguments args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets resource usage information from a Kubernetes namespace with strongly-typed arguments.
    /// </summary>
    /// <param name="args">The strongly-typed arguments containing namespace and optional pod name.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A ToolResult with GetResourceUsageOutput containing resource usage metrics.</returns>
    Task<ToolResult> GetResourceUsageAsync(GetResourceUsageArguments args, CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a command in a Kubernetes pod with strongly-typed arguments.
    /// </summary>
    /// <param name="args">The strongly-typed arguments containing namespace, pod name, and command.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A ToolResult with ExecuteCommandOutput containing stdout, stderr, and exit code.</returns>
    Task<ToolResult> ExecuteCommandAsync(ExecuteCommandArguments args, CancellationToken cancellationToken = default);
}
