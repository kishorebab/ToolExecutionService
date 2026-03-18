using System.Diagnostics;
using ToolExecution.Domain.Models;
using ToolExecution.Infrastructure.Clients;

namespace ToolExecution.Infrastructure.Tools;

/// <summary>
/// Tool for listing deployments in a Kubernetes namespace.
/// Implements the ITool interface for integration with the Tool Execution Engine.
/// </summary>
public class GetDeploymentsTool : ITool
{
    private readonly IKubernetesClient _kubernetesClient;

    public ToolDefinition Definition { get; }

    public GetDeploymentsTool(IKubernetesClient kubernetesClient)
    {
        _kubernetesClient = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));

        Definition = new ToolDefinition
        {
            Name = "get-deployments",
            Description = "Lists all deployments in a Kubernetes namespace.",
            Version = "1.0.0",
            Category = "kubernetes",
            Tags = ["kubernetes", "deployments", "list"],
            IsIdempotent = true,
            TimeoutSeconds = 30,
            IsEnabled = true,
            InputSchema = """
                {
                    "type": "object",
                    "properties": {
                        "namespace": {
                            "type": "string",
                            "description": "Kubernetes namespace to list deployments from",
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
                        "deployments": {
                            "type": "array",
                            "description": "List of deployments in the namespace",
                            "items": {
                                "type": "object",
                                "properties": {
                                    "name": { "type": "string" },
                                    "namespace": { "type": "string" },
                                    "replicas": { "type": "integer" },
                                    "readyReplicas": { "type": "integer" }
                                }
                            }
                        }
                    }
                }
                """,
            Parameters = new Dictionary<string, ToolParameterDto>
            {
                ["namespace"] = new() { Type = "string", Required = true, Description = "Kubernetes namespace to list deployments in" }
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
            var args = new GetDeploymentsArguments { Namespace = @namespace };
            var result = await _kubernetesClient.GetDeploymentsAsync(args, cancellationToken);

            stopwatch.Stop();

            if (!result.Success)
            {
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    result.Error ?? "Failed to list deployments",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.Failed,
                    request.CorrelationId);
            }

            // Convert output to dictionary
            var output = new Dictionary<string, object?>
            {
                { "deployments", result.Output ?? new GetDeploymentsOutput() }
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
                $"Error listing deployments: {ex.Message}",
                stopwatch.ElapsedMilliseconds,
                ExecutionStatus.Failed,
                request.CorrelationId);
        }
    }
}
