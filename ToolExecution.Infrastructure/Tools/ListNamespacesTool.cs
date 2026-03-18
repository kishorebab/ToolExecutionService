using System.Diagnostics;
using System.Text.Json;
using ToolExecution.Domain.Models;
using ToolExecution.Infrastructure.Clients;

namespace ToolExecution.Infrastructure.Tools;

/// <summary>
/// Tool for listing all available namespaces in a Kubernetes cluster.
/// Implements the ITool interface for integration with the Tool Execution Engine.
/// </summary>
public class ListNamespacesTool : ITool
{
    private readonly IKubernetesClient _kubernetesClient;

    public ToolDefinition Definition { get; }

    public ListNamespacesTool(IKubernetesClient kubernetesClient)
    {
        _kubernetesClient = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));

        Definition = new ToolDefinition
        {
            Name = "list-namespaces",
            Description = "Lists all available Kubernetes namespaces. " +
                          "Always called by the orchestrator before planning " +
                          "to provide the AI Agent with the available namespaces.",
            Version = "1.0.0",
            Category = "kubernetes",
            Tags = ["kubernetes", "namespaces", "list"],
            IsIdempotent = true,
            TimeoutSeconds = 10,
            IsEnabled = true,
            InputSchema = """
                {
                    "type": "object",
                    "properties": {},
                    "required": []
                }
                """,
            OutputSchema = """
                {
                    "type": "object",
                    "properties": {
                        "namespaces": {
                            "type": "array",
                            "description": "List of available namespaces",
                            "items": { "type": "string" }
                        },
                        "count": {
                            "type": "integer",
                            "description": "Total number of namespaces"
                        }
                    }
                }
                """,
            Parameters = new Dictionary<string, ToolParameterDto>()
        };
    }

    public async Task<ToolResponse> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Call Kubernetes client
            var result = await _kubernetesClient.ListNamespacesAsync(cancellationToken);

            stopwatch.Stop();

            if (!result.Success)
            {
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    result.Error ?? "Failed to list namespaces",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.Failed,
                    request.CorrelationId);
            }

            // Convert anonymous output object to dictionary
            var outputDict = new Dictionary<string, object?>();
            if (result.Output != null)
            {
                var json = JsonSerializer.Serialize(result.Output);
                var jsonDoc = JsonDocument.Parse(json);
                foreach (var prop in jsonDoc.RootElement.EnumerateObject())
                {
                    outputDict[prop.Name] = prop.Value.Deserialize<object>();
                }
            }

            return ToolResponse.CreateSuccess(
                request.TraceId,
                outputDict,
                stopwatch.ElapsedMilliseconds,
                request.CorrelationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ToolResponse.CreateFailure(
                request.TraceId,
                $"Error listing namespaces: {ex.Message}",
                stopwatch.ElapsedMilliseconds,
                ExecutionStatus.Failed,
                request.CorrelationId);
        }
    }
}
