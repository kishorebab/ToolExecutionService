namespace ToolExecution.Domain.Models;

/// <summary>
/// Defines a tool that can be executed by the engine.
/// Immutable and serves as metadata for all registered tools.
/// </summary>
public class ToolDefinition
{
    /// <summary>
    /// Unique name of the tool (e.g., "echo", "math_add", "get-pod-logs")
    /// Must be URL-safe and lowercase.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of what the tool does.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Version of the tool implementation (e.g., "1.0.0", "1.0.1")
    /// Allows multiple versions of same tool to coexist.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// JSON Schema (as string) describing input parameters.
    /// Example: {"type":"object","properties":{"a":{"type":"number"},"b":{"type":"number"}},"required":["a","b"]}
    /// </summary>
    public required string InputSchema { get; init; }

    /// <summary>
    /// JSON Schema (as string) describing output format.
    /// Example: {"type":"object","properties":{"result":{"type":"number"}}}
    /// </summary>
    public required string OutputSchema { get; init; }

    /// <summary>
    /// Maximum allowed execution time in seconds.
    /// Prevents tools from running indefinitely.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;

    /// <summary>
    /// Whether multiple executions with identical inputs always produce identical outputs.
    /// Important for caching and retry logic.
    /// </summary>
    public bool IsIdempotent { get; init; } = false;

    /// <summary>
    /// Category/classification of the tool (e.g., "math", "infrastructure", "data")
    /// Used for organizing tools in UI/documentation.
    /// </summary>
    public string? Category { get; init; }

    /// <summary>
    /// Optional tags for searching and filtering tools.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// Whether this tool is enabled/callable. Disabled tools appear in registry but cannot execute.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// ISO 8601 timestamp when this tool was registered.
    /// </summary>
    public DateTime RegisteredAt { get; init; } = DateTime.UtcNow;
}
