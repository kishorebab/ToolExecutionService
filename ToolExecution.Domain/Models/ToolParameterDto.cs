namespace ToolExecution.Domain.Models;

/// <summary>
/// Describes a single parameter a tool accepts.
/// Consumed by the AI Agent to understand what arguments to pass when planning tool execution.
/// </summary>
public class ToolParameterDto
{
    /// <summary>
    /// JSON-compatible type: "string", "integer", "boolean", "array", "object"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Whether this parameter must be provided. Defaults to true.
    /// </summary>
    public bool Required { get; set; } = true;

    /// <summary>
    /// Human-readable description for the LLM to understand the parameter's purpose.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Default value if not provided. Must be null if Required is true.
    /// </summary>
    public object? Default { get; set; }
}
