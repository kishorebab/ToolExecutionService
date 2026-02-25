namespace ToolExecution.Application.DTOs;

/// <summary>
/// Arguments for the execute-command tool.
/// </summary>
public class ExecuteCommandArguments
{
    /// <summary>
    /// Kubernetes namespace where the pod resides. Required.
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Name of the pod to execute command in. Required.
    /// </summary>
    public string PodName { get; set; } = string.Empty;

    /// <summary>
    /// The command and arguments to execute. Required.
    /// Example: ["sh", "-c", "ls -la"]
    /// </summary>
    public List<string> Command { get; set; } = new();
}

/// <summary>
/// Output of the execute-command tool.
/// </summary>
public class ExecuteCommandOutput
{
    /// <summary>
    /// Standard output from the executed command.
    /// </summary>
    public List<string> Stdout { get; set; } = new();

    /// <summary>
    /// Standard error output from the executed command.
    /// </summary>
    public List<string> Stderr { get; set; } = new();

    /// <summary>
    /// Exit code from the command execution.
    /// </summary>
    public int ExitCode { get; set; } = 0;
}
