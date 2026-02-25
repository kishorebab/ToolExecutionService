namespace ToolExecution.Application.DTOs;

/// <summary>
/// Resource usage statistics for a pod or node.
/// </summary>
public class ResourceUsageInfo
{
    /// <summary>
    /// Name of the pod or node.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Namespace (for pods). May be empty for node-level metrics.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// CPU usage as a string (e.g., "100m", "0.5").
    /// </summary>
    public string CpuUsage { get; set; } = string.Empty;

    /// <summary>
    /// Memory usage as a string (e.g., "128Mi", "512Mi").
    /// </summary>
    public string MemoryUsage { get; set; } = string.Empty;
}

/// <summary>
/// Arguments for the get-resource-usage tool.
/// </summary>
public class GetResourceUsageArguments
{
    /// <summary>
    /// Kubernetes namespace to get resource usage from. Required.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Optional pod name to get resource usage for a specific pod.
    /// If not specified, returns resource usage for all pods in the namespace.
    /// </summary>
    public string? PodName { get; set; }
}

/// <summary>
/// Output of the get-resource-usage tool.
/// </summary>
public class GetResourceUsageOutput
{
    /// <summary>
    /// List of resource usage statistics.
    /// </summary>
    public List<ResourceUsageInfo> ResourceUsage { get; set; } = new();
}
