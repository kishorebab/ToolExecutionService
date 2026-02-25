namespace ToolExecution.Infrastructure.SampleTools;

using System.Diagnostics;
using ToolExecution.Domain.Models;

/// <summary>
/// Sample tool that echoes back the input.
/// Demonstrates basic ITool implementation.
/// </summary>
public class EchoTool : ITool
{
    public ToolDefinition Definition { get; }

    public EchoTool()
    {
        Definition = new ToolDefinition
        {
            Name = "echo",
            Description = "Returns the input as output. Useful for testing and debugging.",
            Version = "1.0.0",
            Category = "utility",
            Tags = ["test", "debug", "echo"],
            IsIdempotent = true,
            TimeoutSeconds = 5,
            IsEnabled = true,
            InputSchema = """
                {
                    "type": "object",
                    "properties": {
                        "message": { "type": "string", "description": "Message to echo back" }
                    },
                    "required": ["message"]
                }
                """,
            OutputSchema = """
                {
                    "type": "object",
                    "properties": {
                        "echoed": { "type": "string", "description": "The echoed message" },
                        "timestamp": { "type": "string", "description": "ISO 8601 UTC timestamp" }
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

        // Simulate async work
        await Task.Delay(10, cancellationToken);

        try
        {
            // Extract input
            if (!request.Input.TryGetValue("message", out var messageObj) || messageObj == null)
            {
                stopwatch.Stop();
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    "Missing required input 'message'.",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.ValidationError,
                    request.CorrelationId);
            }

            var message = messageObj.ToString();

            // Create output
            var output = new Dictionary<string, object?>
            {
                { "echoed", message },
                { "timestamp", DateTime.UtcNow.ToString("O") }
            };

            stopwatch.Stop();
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
                $"Echo tool failed: {ex.Message}",
                stopwatch.ElapsedMilliseconds,
                ExecutionStatus.Failed,
                request.CorrelationId);
        }
    }
}
