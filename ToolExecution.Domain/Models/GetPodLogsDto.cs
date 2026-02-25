namespace ToolExecution.Domain.Models;

/// <summary>
/// Arguments for the get-pod-logs tool.
/// </summary>
public class GetPodLogsArguments
{
    /// <summary>
    /// Kubernetes namespace where the pod resides. Required.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Name of the pod. Required.
    /// </summary>
    public string PodName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the container within the pod. Optional.
    /// If not specified, logs from the first container will be retrieved.
    /// </summary>
    public string? ContainerName { get; set; }

    /// <summary>
    /// Number of lines to tail from the end of the logs. Default: 500.
    /// Must be between 1 and 5000.
    /// </summary>
    public int TailLines { get; set; } = 500;
}

/// <summary>
/// Output of the get-pod-logs tool.
/// </summary>
public class GetPodLogsOutput
{
    /// <summary>
    /// List of log lines from the pod.
    /// </summary>
    public List<string> Logs { get; set; } = new();
}
