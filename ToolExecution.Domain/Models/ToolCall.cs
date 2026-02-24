using System.Text.Json.Nodes;

namespace ToolExecution.Domain.Models;

public sealed record ToolCall
{
    public string TraceId { get; init; } = string.Empty;
    public string SessionId { get; init; } = string.Empty;
    public string ToolName { get; init; } = string.Empty;
    public JsonObject? Arguments { get; init; }
}
