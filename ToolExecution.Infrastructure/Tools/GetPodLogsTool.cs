using System.Diagnostics;
using ToolExecution.Domain.Models;
using ToolExecution.Infrastructure.Clients;

namespace ToolExecution.Infrastructure.Tools;

/// <summary>
/// Tool for retrieving pod logs from a Kubernetes pod.
/// Implements the ITool interface for integration with the Tool Execution Engine.
/// </summary>
public class GetPodLogsTool : ITool
{
    private readonly IKubernetesClient _kubernetesClient;

    public ToolDefinition Definition { get; }

    public GetPodLogsTool(IKubernetesClient kubernetesClient)
    {
        _kubernetesClient = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));

        Definition = new ToolDefinition
        {
            Name = "get-pod-logs",
            Description = "Retrieves logs from a Kubernetes pod.",
            Version = "1.0.0",
            Category = "kubernetes",
            Tags = ["kubernetes", "logs", "pods"],
            IsIdempotent = true,
            TimeoutSeconds = 30,
            IsEnabled = true,
            InputSchema = """
                {
                    "type": "object",
                    "properties": {
                        "namespace": {
                            "type": "string",
                            "description": "Kubernetes namespace of the pod",
                            "default": "default"
                        },
                        "podName": {
                            "type": "string",
                            "description": "Name of the pod"
                        },
                        "containerName": {
                            "type": "string",
                            "description": "Container name (optional, uses first container if not provided)",
                            "default": ""
                        },
                        "tailLines": {
                            "type": "integer",
                            "description": "Number of log lines to return from the end",
                            "default": 100
                        }
                    },
                    "required": ["namespace", "podName"]
                }
                """,
            OutputSchema = """
                {
                    "type": "object",
                    "properties": {
                        "logs": {
                            "type": "array",
                            "description": "Array of log lines",
                            "items": { "type": "string" }
                        }
                    }
                }
                """,
            Parameters = new Dictionary<string, ToolParameterDto>
            {
                ["namespace"]     = new() { Type = "string",  Required = true,  Description = "Kubernetes namespace" },
                ["podName"]       = new() { Type = "string",  Required = true,  Description = "Name of the pod" },
                ["containerName"] = new() { Type = "string",  Required = false, Description = "Container name (optional if single container)" },
                ["tailLines"]     = new() { Type = "integer", Required = false, Description = "Number of log lines to tail", Default = 500 }
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
            // Extract inputs
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

            if (!request.Input.TryGetValue("podName", out var podNameObj) || 
                podNameObj == null)
            {
                stopwatch.Stop();
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    "Missing required input 'podName'.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.ValidationError,
                    request.CorrelationId);
            }

            var @namespace = namespaceObj.ToString();
            var podName = podNameObj.ToString();
            var containerName = request.Input.TryGetValue("containerName", out var containerObj) 
                ? containerObj?.ToString() ?? "" 
                : "";
            var tailLines = request.Input.TryGetValue("tailLines", out var tailObj) && 
                            int.TryParse(tailObj?.ToString(), out var lines)
                ? lines
                : 100;

            if (string.IsNullOrWhiteSpace(@namespace) || string.IsNullOrWhiteSpace(podName))
            {
                stopwatch.Stop();
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    "Namespace and pod name cannot be empty.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.ValidationError,
                    request.CorrelationId);
            }

            // Call Kubernetes client
            var args = new GetPodLogsArguments
            {
                Namespace = @namespace,
                PodName = podName,
                ContainerName = containerName,
                TailLines = tailLines
            };

            var result = await _kubernetesClient.GetPodLogsAsync(args, cancellationToken);

            stopwatch.Stop();

            if (!result.Success)
            {
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    result.Error ?? "Failed to get pod logs",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.Failed,
                    request.CorrelationId);
            }

            // Convert output to dictionary
            var output = new Dictionary<string, object?>
            {
                { "logs", result.Output ?? new GetPodLogsOutput() }
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
                $"Error getting pod logs: {ex.Message}",
                stopwatch.ElapsedMilliseconds,
                ExecutionStatus.Failed,
                request.CorrelationId);
        }
    }
}
