namespace ToolExecution.Infrastructure.SampleTools;

using System.Diagnostics;
using ToolExecution.Domain.Models;

/// <summary>
/// Sample tool that adds two numbers.
/// Demonstrates numeric input validation and calculation.
/// </summary>
public class MathAddTool : ITool
{
    public ToolDefinition Definition { get; }

    public MathAddTool()
    {
        Definition = new ToolDefinition
        {
            Name = "math_add",
            Description = "Adds two numbers and returns the sum.",
            Version = "1.0.0",
            Category = "math",
            Tags = ["math", "arithmetic", "add"],
            IsIdempotent = true,
            TimeoutSeconds = 5,
            IsEnabled = true,
            InputSchema = """
                {
                    "type": "object",
                    "properties": {
                        "a": { "type": "number", "description": "First number" },
                        "b": { "type": "number", "description": "Second number" }
                    },
                    "required": ["a", "b"]
                }
                """,
            OutputSchema = """
                {
                    "type": "object",
                    "properties": {
                        "result": { "type": "number", "description": "Sum of a and b" },
                        "a": { "type": "number" },
                        "b": { "type": "number" }
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
        await Task.Delay(5, cancellationToken);

        try
        {
            // Extract and validate inputs
            if (!TryGetDecimal(request.Input, "a", out var a))
            {
                stopwatch.Stop();
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    "Missing or invalid required input 'a' (must be a number).",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.ValidationError,
                    request.CorrelationId);
            }

            if (!TryGetDecimal(request.Input, "b", out var b))
            {
                stopwatch.Stop();
                return ToolResponse.CreateFailure(
                    request.TraceId,
                    "Missing or invalid required input 'b' (must be a number).",
                    stopwatch.ElapsedMilliseconds,
                    ExecutionStatus.ValidationError,
                    request.CorrelationId);
            }

            // Calculate
            var result = a + b;

            // Create output
            var output = new Dictionary<string, object?>
            {
                { "result", result },
                { "a", a },
                { "b", b }
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
                $"Math add tool failed: {ex.Message}",
                stopwatch.ElapsedMilliseconds,
                ExecutionStatus.Failed,
                request.CorrelationId);
        }
    }

    private static bool TryGetDecimal(
        IDictionary<string, object?> input,
        string key,
        out decimal value)
    {
        value = 0m;

        if (!input.TryGetValue(key, out var obj) || obj == null)
            return false;

        return obj switch
        {
            decimal d => (value = d) > 0 || value <= 0,
            int i => (value = i) > 0 || value <= 0,
            long l => (value = l) > 0 || value <= 0,
            float f => (value = (decimal)f) > 0 || value <= 0,
            double d => (value = (decimal)d) > 0 || value <= 0,
            string s => decimal.TryParse(s, out value),
            _ => false
        };
    }
}
