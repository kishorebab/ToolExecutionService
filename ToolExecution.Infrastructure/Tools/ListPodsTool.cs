using System.Diagnostics;
using ToolExecution.Domain.Models;
using ToolExecution.Infrastructure.Clients;

namespace ToolExecution.Infrastructure.Tools;

/// <summary>
/// Tool for listing pods in a Kubernetes namespace.
/// Implements the ITool interface for integration with the Tool Execution Engine.
/// </summary>
public class ListPodsTool : ITool
{
    private readonly IKubernetesClient _kubernetesClient;

    public ToolDefinition Definition { get; }

    public ListPodsTool(IKubernetesClient kubernetesClient)
    {
        _kubernetesClient = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));

        Definition = new ToolDefinition
        {
            Name = "list-pods",
            Description = "Lists all pods in a Kubernetes namespace.",
            Version = "1.0.0",
            Category = "kubernetes",
            Tags = ["kubernetes", "pods", "list"],
            IsIdempotent = true,
            TimeoutSeconds = 30,
            IsEnabled = true,
            InputSchema = """
                {
                    "type": "object",
                    "properties": {
                        "namespace": {
                            "type": "string",
                            "description": "Kubernetes namespace to list pods from",
                            "default": "default"
                        }
                    },
                    "required": ["namespace"]
                }
                """,
            OutputSchema = """
                {
                    "type": "object",
                    "properties": {
                        "pods": {
                            "type": "array",
                            "description": "List of pods in the namespace",
                            "items": {
                                "type": "object",
                                "properties": {
                                    "name": { "type": "string" },
                                    "namespace": { "type": "string" },
                                    "status": { "type": "string" },
                                    "readyContainers": { "type": "integer" },
                                    "totalContainers": { "type": "integer" }
                                }
                            }
                        }
                    }
                }
                """
        };
    }

    public async Task<ToolResponse> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Extract and validate namespace
            if (!request.Input.TryGetValue("namespace", out var namespaceObj) || 
                namespaceObj == null)
            {
                stopwatch.Stop();
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    "Missing required input 'namespace'.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.ValidationError,
                    request.CorrelationId);
            }

            var @namespace = namespaceObj.ToString();
            if (string.IsNullOrWhiteSpace(@namespace))
            {
                stopwatch.Stop();
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    "Namespace cannot be empty.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.ValidationError,
                    request.CorrelationId);
            }

            // Call Kubernetes client
            var args = new ListPodsArguments { Namespace = @namespace };
            var result = await _kubernetesClient.ListPodsAsync(args, cancellationToken);

            stopwatch.Stop();

            if (!result.Success)
            {
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    result.Error ?? "Failed to list pods",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.Failed,
                    request.CorrelationId);
            }

            // Convert output to dictionary
            var output = new Dictionary<string, object?>
            {
                { "pods", result.Output ?? new ListPodsOutput() }
            };

            return ToolResponse.CreateSuccess(
                request.TraceId,
                output,
                stopwatch.ElapsedMilliseconds,
                request.CorrelationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ToolResponse.CreateFailure(
                request.TraceId,
                $"Error listing pods: {ex.Message}",
                stopwatch.ElapsedMilliseconds,
                ExecutionStatus.Failed,
                request.CorrelationId);
        }
    }
}
