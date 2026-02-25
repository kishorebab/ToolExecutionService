namespace ToolExecution.Infrastructure.Registries;

using System.Collections.Concurrent;
using ToolExecution.Domain.Models;

/// <summary>
/// In-memory, thread-safe tool registry.
/// Suitable for services with a fixed set of tools or development.
/// Can be extended later with persistence (database, plugins, etc).
/// </summary>
public sealed class InMemoryToolRegistry : IToolRegistry
{
    private readonly ConcurrentDictionary<string, ITool> _tools;

    public InMemoryToolRegistry()
    {
        _tools = new ConcurrentDictionary<string, ITool>(StringComparer.OrdinalIgnoreCase);
    }

    public void Register(ITool tool)
    {
        if (tool == null)
            throw new ArgumentNullException(nameof(tool));

        if (string.IsNullOrWhiteSpace(tool.Definition?.Name))
            throw new ArgumentException("Tool must have a non-empty Name in its Definition.", nameof(tool));

        var toolName = tool.Definition.Name;

        if (!_tools.TryAdd(toolName, tool))
        {
            throw new InvalidOperationException(
                $"Tool with name '{toolName}' is already registered. Unregister it first before re-registering.");
        }
    }

    public bool Unregister(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return false;

        return _tools.TryRemove(toolName, out _);
    }

    public ITool? Get(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return null;

        _tools.TryGetValue(toolName, out var tool);
        return tool;
    }

    public bool Exists(string toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName))
            return false;

        return _tools.ContainsKey(toolName);
    }

    public IReadOnlyCollection<string> ListToolNames()
    {
        return _tools.Keys.ToList().AsReadOnly();
    }

    public IReadOnlyCollection<ToolDefinition> ListToolDefinitions()
    {
        return _tools.Values
            .Select(t => t.Definition)
            .ToList()
            .AsReadOnly();
    }

    public int Count => _tools.Count;
}
