namespace ToolExecution.Domain.Models;

/// <summary>
/// Registry for dynamic tool discovery and management.
/// Allows runtime registration and lookup of ITool implementations.
/// Can be extended later with database, plugin loading, etc.
/// </summary>
public interface IToolRegistry
{
    /// <summary>
    /// Register a tool in the registry.
    /// </summary>
    /// <param name="tool">The tool to register. Must have a unique name in Definition.</param>
    /// <exception cref="InvalidOperationException">Thrown if tool with same name already registered.</exception>
    void Register(ITool tool);

    /// <summary>
    /// Unregister a tool from the registry.
    /// </summary>
    /// <param name="toolName">The name of the tool to unregister.</param>
    /// <returns>True if tool was found and removed, false otherwise.</returns>
    bool Unregister(string toolName);

    /// <summary>
    /// Get a registered tool by name.
    /// </summary>
    /// <param name="toolName">The name of the tool.</param>
    /// <returns>The tool if found, null otherwise.</returns>
    ITool? Get(string toolName);

    /// <summary>
    /// Check if a tool is registered.
    /// </summary>
    /// <param name="toolName">The name of the tool.</param>
    /// <returns>True if tool is registered, false otherwise.</returns>
    bool Exists(string toolName);

    /// <summary>
    /// Get all registered tool names.
    /// </summary>
    /// <returns>Collection of tool names (immutable).</returns>
    IReadOnlyCollection<string> ListToolNames();

    /// <summary>
    /// Get all registered tool definitions.
    /// </summary>
    /// <returns>Collection of tool definitions (immutable).</returns>
    IReadOnlyCollection<ToolDefinition> ListToolDefinitions();

    /// <summary>
    /// Get tool count.
    /// </summary>
    int Count { get; }
}
