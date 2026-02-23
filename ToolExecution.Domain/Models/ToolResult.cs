using System.Text.Json.Nodes;

namespace ToolExecution.Domain.Models;

public sealed class ToolResult
{
    public string TraceId { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string ToolName { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? Output { get; init; }
    public JsonObject? Metrics { get; init; }
    public string? Error { get; init; }
}
