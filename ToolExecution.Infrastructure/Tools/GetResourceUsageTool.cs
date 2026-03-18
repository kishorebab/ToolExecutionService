using System.Diagnostics;
using ToolExecution.Domain.Models;
using ToolExecution.Infrastructure.Clients;

namespace ToolExecution.Infrastructure.Tools;

/// <summary>
/// Tool for retrieving resource usage metrics from Kubernetes.
/// Implements the ITool interface for integration with the Tool Execution Engine.
/// </summary>
public class GetResourceUsageTool : ITool
{
    private readonly IKubernetesClient _kubernetesClient;

    public ToolDefinition Definition { get; }

    public GetResourceUsageTool(IKubernetesClient kubernetesClient)
    {
        _kubernetesClient = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));

        Definition = new ToolDefinition
        {
            Name = "get-resource-usage",
            Description = "Retrieves resource usage metrics (CPU, memory) from Kubernetes pods.",
            Version = "1.0.0",
            Category = "kubernetes",
            Tags = ["kubernetes", "metrics", "resources", "cpu", "memory"],
            IsIdempotent = true,
            TimeoutSeconds = 30,
            IsEnabled = true,
            InputSchema = """
                {
                    "type": "object",
                    "properties": {
                        "namespace": {
                            "type": "string",
                            "description": "Kubernetes namespace to get resource usage from",
                            "default": "default"
                        },
                        "podName": {
                            "type": "string",
                            "description": "Optional pod name to get usage for specific pod (returns all if not provided)"
                        }
                    },
                    "required": ["namespace"]
                }
                """,
            OutputSchema = """
                {
                    "type": "object",
                    "properties": {
                        "resourceUsage": {
                            "type": "array",
                            "description": "Array of resource usage metrics",
                            "items": {
                                "type": "object",
                                "properties": {
                                    "name": { "type": "string" },
                                    "namespace": { "type": "string" },
                                    "cpuUsage": { "type": "string" },
                                    "memoryUsage": { "type": "string" }
                                }
                            }
                        }
                    }
                }
                """,
            Parameters = new Dictionary<string, ToolParameterDto>
            {
                ["namespace"] = new() { Type = "string", Required = true,  Description = "Kubernetes namespace" },
                ["podName"]   = new() { Type = "string", Required = false, Description = "Filter to a specific pod (optional, omit for all pods)" }
            }
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
            var podName = request.Input.TryGetValue("podName", out var podObj) 
                ? podObj?.ToString() 
                : null;

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
            var args = new GetResourceUsageArguments
            {
                Namespace = @namespace,
                PodName = podName
            };

            var result = await _kubernetesClient.GetResourceUsageAsync(args, cancellationToken);

            stopwatch.Stop();

            if (!result.Success)
            {
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    result.Error ?? "Failed to get resource usage",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.Failed,
                    request.CorrelationId);
            }

            // Convert output to dictionary
            var output = new Dictionary<string, object?>
            {
                { "resourceUsage", result.Output ?? new GetResourceUsageOutput() }
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
                $"Error getting resource usage: {ex.Message}",
                stopwatch.ElapsedMilliseconds,
                ExecutionStatus.Failed,
                request.CorrelationId);
        }
    }
}
