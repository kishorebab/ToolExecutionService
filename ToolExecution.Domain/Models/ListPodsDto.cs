namespace ToolExecution.Domain.Models;

/// <summary>
/// Information about a Kubernetes pod.
/// </summary>
public class PodInfo
{
    /// <summary>
    /// Name of the pod.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Namespace where the pod resides.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the pod (Running, Pending, Failed, etc.).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Number of ready containers within this pod.
    /// </summary>
    public int ReadyContainers { get; set; }

    /// <summary>
    /// Total number of containers defined for this pod.
    /// </summary>
    public int TotalContainers { get; set; }
}

/// <summary>
/// Arguments for the list-pods tool.
/// </summary>
public class ListPodsArguments
{
    /// <summary>
    /// Kubernetes namespace to list pods from. Required.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;
}

/// <summary>
/// Output of the list-pods tool.
/// </summary>
public class ListPodsOutput
{
    /// <summary>
    /// List of pods in the namespace.
    /// </summary>
    public List<PodInfo> Pods { get; set; } = new();
}
