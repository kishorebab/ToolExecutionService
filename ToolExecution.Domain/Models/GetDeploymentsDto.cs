namespace ToolExecution.Domain.Models;

/// <summary>
/// Information about a Kubernetes deployment.
/// </summary>
public class DeploymentInfo
{
    /// <summary>
    /// Name of the deployment.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Namespace where the deployment resides.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Desired number of replicas.
    /// </summary>
    public int Replicas { get; set; }

    /// <summary>
    /// Number of replicas currently ready.
    /// </summary>
    public int ReadyReplicas { get; set; }

    /// <summary>
    /// Current revision/generation of the deployment.
    /// </summary>
    public long ObservedGeneration { get; set; }
}

/// <summary>
/// Arguments for the get-deployments tool.
/// </summary>
public class GetDeploymentsArguments
{
    /// <summary>
    /// Kubernetes namespace to list deployments from. Required.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;
}

/// <summary>
/// Output of the get-deployments tool.
/// </summary>
public class GetDeploymentsOutput
{
    /// <summary>
    /// List of deployments in the namespace.
    /// </summary>
    public List<DeploymentInfo> Deployments { get; set; } = new();
}
