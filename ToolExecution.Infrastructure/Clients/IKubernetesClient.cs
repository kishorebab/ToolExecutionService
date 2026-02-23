using System.Text.Json.Nodes;
using ToolExecution.Domain.Models;

namespace ToolExecution.Infrastructure.Clients;

public interface IKubernetesClient
{
    Task<ToolResult> GetPodLogsAsync(JsonObject? args, CancellationToken cancellationToken = default);
    Task<ToolResult> ListPodsAsync(JsonObject? args, CancellationToken cancellationToken = default);
    Task<ToolResult> GetDeploymentsAsync(JsonObject? args, CancellationToken cancellationToken = default);
    Task<ToolResult> GetResourceUsageAsync(JsonObject? args, CancellationToken cancellationToken = default);
    Task<ToolResult> ExecuteCommandAsync(JsonObject? args, CancellationToken cancellationToken = default);
}
