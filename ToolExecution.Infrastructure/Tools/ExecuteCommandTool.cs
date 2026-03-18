using System.Diagnostics;
using ToolExecution.Domain.Models;
using ToolExecution.Infrastructure.Clients;

namespace ToolExecution.Infrastructure.Tools;

/// <summary>
/// Tool for executing commands inside a Kubernetes pod.
/// Implements the ITool interface for integration with the Tool Execution Engine.
/// </summary>
public class ExecuteCommandTool : ITool
{
    private readonly IKubernetesClient _kubernetesClient;

    public ToolDefinition Definition { get; }

    public ExecuteCommandTool(IKubernetesClient kubernetesClient)
    {
        _kubernetesClient = kubernetesClient ?? throw new ArgumentNullException(nameof(kubernetesClient));

        Definition = new ToolDefinition
        {
            Name = "execute-command",
            Description = "Executes a command inside a Kubernetes pod.",
            Version = "1.0.0",
            Category = "kubernetes",
            Tags = ["kubernetes", "execute", "command", "debug"],
            IsIdempotent = false, // Commands may have side effects
            TimeoutSeconds = 60,
            IsEnabled = true,
            InputSchema = """
                {
                    "type": "object",
                    "properties": {
                        "namespace": {
                            "type": "string",
                            "description": "Kubernetes namespace of the pod"
                        },
                        "podName": {
                            "type": "string",
                            "description": "Name of the pod to execute command in"
                        },
                        "command": {
                            "type": "array",
                            "description": "Command and arguments to execute (e.g., ['sh', '-c', 'ls -la'])",
                            "items": { "type": "string" }
                        }
                    },
                    "required": ["namespace", "podName", "command"]
                }
                """,
            OutputSchema = """
                {
                    "type": "object",
                    "properties": {
                        "stdout": {
                            "type": "array",
                            "description": "Standard output lines",
                            "items": { "type": "string" }
                        },
                        "stderr": {
                            "type": "array",
                            "description": "Standard error output lines",
                            "items": { "type": "string" }
                        },
                        "exitCode": {
                            "type": "integer",
                            "description": "Process exit code"
                        }
                    }
                }
                """,
            Parameters = new Dictionary<string, ToolParameterDto>
            {
                ["namespace"] = new() { Type = "string", Required = true, Description = "Kubernetes namespace" },
                ["podName"]   = new() { Type = "string", Required = true, Description = "Name of the pod to exec into" },
                ["command"]   = new() { Type = "array",  Required = true, Description = "Command and args as a list, e.g. [\"cat\", \"/etc/hosts\"]" }
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

            if (!request.Input.TryGetValue("command", out var commandObj) || 
                commandObj == null)
            {
                stopwatch.Stop();
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    "Missing required input 'command'.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.ValidationError,
                    request.CorrelationId);
            }

            var @namespace = namespaceObj.ToString();
            var podName = podNameObj.ToString();

            // Parse command array
            List<string> command = new();
            if (commandObj is System.Collections.IEnumerable enumerable && 
                !(commandObj is string))
            {
                foreach (var item in enumerable)
                {
                    command.Add(item?.ToString() ?? "");
                }
            }
            else
            {
                stopwatch.Stop();
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    "Command must be an array of strings.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.ValidationError,
                    request.CorrelationId);
            }

            if (string.IsNullOrWhiteSpace(@namespace) || 
                string.IsNullOrWhiteSpace(podName) || 
                command.Count == 0)
            {
                stopwatch.Stop();
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    "Namespace, pod name, and command cannot be empty.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.ValidationError,
                    request.CorrelationId);
            }

            // Call Kubernetes client
            var args = new ExecuteCommandArguments
            {
                Namespace = @namespace,
                PodName = podName,
                Command = command
            };

            var result = await _kubernetesClient.ExecuteCommandAsync(args, cancellationToken);

            stopwatch.Stop();

            if (!result.Success)
            {
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    result.Error ?? "Failed to execute command",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.Failed,
                    request.CorrelationId);
            }

            // Convert output to dictionary
            var output = new Dictionary<string, object?>
            {
                { "stdout", result.Output ?? new ExecuteCommandOutput() }
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
                $"Error executing command: {ex.Message}",
                stopwatch.ElapsedMilliseconds,
                ExecutionStatus.Failed,
                request.CorrelationId);
        }
    }
}
