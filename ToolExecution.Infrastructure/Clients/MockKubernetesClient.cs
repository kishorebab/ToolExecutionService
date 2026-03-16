using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ToolExecution.Domain.Models;

namespace ToolExecution.Infrastructure.Clients;

/// <summary>
/// Mock Kubernetes client that loads data from JSON snapshot files.
/// Supports relative date-based snapshot resolution and runtime log timestamp rewriting.
/// </summary>
public class MockKubernetesClient : IKubernetesClient
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MockKubernetesClient> _logger;
    private readonly Random _random = new();
    private readonly string _snapshotRoot;

    public MockKubernetesClient(IConfiguration configuration, ILogger<MockKubernetesClient> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _snapshotRoot = _configuration["MockCluster:SnapshotRoot"] 
            ?? Path.Combine(AppContext.BaseDirectory, "MockCluster", "snapshots");

        _logger.LogInformation("MockKubernetesClient initialized with snapshot root: {SnapshotRoot}", _snapshotRoot);
    }

    public async Task<ToolResult> GetPodLogsAsync(GetPodLogsArguments args, CancellationToken cancellationToken = default)
    {
        await SimulateLatencyAsync(80, 180);

        try
        {
            var snapshotFolder = ResolveSnapshotFolder();
            var logsFile = Path.Combine(_snapshotRoot, snapshotFolder, "get-pod-logs.json");

            if (!File.Exists(logsFile))
                return CreateErrorResult($"Logs file not found: {logsFile}");

            var json = await File.ReadAllTextAsync(logsFile, cancellationToken);
            var doc = JsonDocument.Parse(json);

            var logs = new List<string>();
            if (doc.RootElement.TryGetProperty("podLogs", out var podLogsObj))
            {
                if (podLogsObj.TryGetProperty(args.PodName, out var podLogs))
                {
                    foreach (var logLine in podLogs.EnumerateArray())
                    {
                        var line = logLine.GetString() ?? string.Empty;
                        line = RewriteLogTimestamps(line);
                        logs.Add(line);
                    }

                    // Apply tail limit
                    if (logs.Count > args.TailLines)
                        logs = logs.TakeLast(args.TailLines).ToList();
                }
            }

            var output = new GetPodLogsOutput { Logs = logs };
            _logger.LogInformation("GetPodLogs: pod={PodName}, namespace={Namespace}, returned {LineCount} lines",
                args.PodName, args.Namespace, logs.Count);

            return CreateSuccessResult(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetPodLogs failed for pod {PodName}", args.PodName);
            return CreateErrorResult($"Failed to retrieve pod logs: {ex.Message}");
        }
    }

    public async Task<ToolResult> ListPodsAsync(ListPodsArguments args, CancellationToken cancellationToken = default)
    {
        await SimulateLatencyAsync(60, 120);

        try
        {
            var snapshotFolder = ResolveSnapshotFolder();
            var podsFile = Path.Combine(_snapshotRoot, snapshotFolder, "list-pods.json");

            if (!File.Exists(podsFile))
                return CreateErrorResult($"Pods file not found: {podsFile}");

            var json = await File.ReadAllTextAsync(podsFile, cancellationToken);
            var doc = JsonDocument.Parse(json);

            var pods = new List<PodInfo>();
            if (doc.RootElement.TryGetProperty("pods", out var podsArray))
            {
                foreach (var podElement in podsArray.EnumerateArray())
                {
                    var podNamespace = podElement.GetProperty("namespace").GetString() ?? string.Empty;

                    // Filter by namespace
                    if (!string.IsNullOrEmpty(args.Namespace) && args.Namespace != "all" && podNamespace != args.Namespace)
                        continue;

                    var pod = new PodInfo
                    {
                        Name = podElement.GetProperty("name").GetString() ?? string.Empty,
                        Namespace = podNamespace,
                        Status = podElement.GetProperty("status").GetString() ?? string.Empty,
                        ReadyContainers = podElement.GetProperty("readyContainers").GetInt32(),
                        TotalContainers = podElement.GetProperty("totalContainers").GetInt32()
                    };

                    pods.Add(pod);
                }
            }

            var output = new ListPodsOutput { Pods = pods };
            _logger.LogInformation("ListPods: namespace={Namespace}, returned {PodCount} pods",
                args.Namespace, pods.Count);

            return CreateSuccessResult(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListPods failed for namespace {Namespace}", args.Namespace);
            return CreateErrorResult($"Failed to list pods: {ex.Message}");
        }
    }

    public async Task<ToolResult> GetDeploymentsAsync(GetDeploymentsArguments args, CancellationToken cancellationToken = default)
    {
        await SimulateLatencyAsync(80, 150);

        try
        {
            var snapshotFolder = ResolveSnapshotFolder();
            var deploymentsFile = Path.Combine(_snapshotRoot, snapshotFolder, "get-deployments.json");

            if (!File.Exists(deploymentsFile))
                return CreateErrorResult($"Deployments file not found: {deploymentsFile}");

            var json = await File.ReadAllTextAsync(deploymentsFile, cancellationToken);
            var doc = JsonDocument.Parse(json);

            var deployments = new List<DeploymentInfo>();
            if (doc.RootElement.TryGetProperty("deployments", out var deploymentsArray))
            {
                foreach (var depElement in deploymentsArray.EnumerateArray())
                {
                    var depNamespace = depElement.GetProperty("namespace").GetString() ?? string.Empty;

                    // Filter by namespace
                    if (!string.IsNullOrEmpty(args.Namespace) && args.Namespace != "all" && depNamespace != args.Namespace)
                        continue;

                    var deployment = new DeploymentInfo
                    {
                        Name = depElement.GetProperty("name").GetString() ?? string.Empty,
                        Namespace = depNamespace,
                        Replicas = depElement.GetProperty("replicas").GetInt32(),
                        ReadyReplicas = depElement.GetProperty("readyReplicas").GetInt32(),
                        ObservedGeneration = depElement.GetProperty("observedGeneration").GetInt64()
                    };

                    deployments.Add(deployment);
                }
            }

            var output = new GetDeploymentsOutput { Deployments = deployments };
            _logger.LogInformation("GetDeployments: namespace={Namespace}, returned {DeploymentCount} deployments",
                args.Namespace, deployments.Count);

            return CreateSuccessResult(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDeployments failed for namespace {Namespace}", args.Namespace);
            return CreateErrorResult($"Failed to get deployments: {ex.Message}");
        }
    }

    public async Task<ToolResult> GetResourceUsageAsync(GetResourceUsageArguments args, CancellationToken cancellationToken = default)
    {
        await SimulateLatencyAsync(100, 200);

        try
        {
            var snapshotFolder = ResolveSnapshotFolder();
            var resourceFile = Path.Combine(_snapshotRoot, snapshotFolder, "get-resource-usage.json");

            if (!File.Exists(resourceFile))
                return CreateErrorResult($"Resource usage file not found: {resourceFile}");

            var json = await File.ReadAllTextAsync(resourceFile, cancellationToken);
            var doc = JsonDocument.Parse(json);

            var resources = new List<ResourceUsageInfo>();
            if (doc.RootElement.TryGetProperty("resourceUsage", out var resourceArray))
            {
                foreach (var resElement in resourceArray.EnumerateArray())
                {
                    var resNamespace = resElement.GetProperty("namespace").GetString() ?? string.Empty;
                    var resName = resElement.GetProperty("name").GetString() ?? string.Empty;

                    // Filter by namespace
                    if (!string.IsNullOrEmpty(args.Namespace) && args.Namespace != "all" && resNamespace != args.Namespace)
                        continue;

                    // Filter by pod name prefix if specified
                    if (!string.IsNullOrEmpty(args.PodName) && !resName.StartsWith(args.PodName))
                        continue;

                    var resource = new ResourceUsageInfo
                    {
                        Name = resName,
                        Namespace = resNamespace,
                        CpuUsage = resElement.GetProperty("cpuUsage").GetString() ?? string.Empty,
                        MemoryUsage = resElement.GetProperty("memoryUsage").GetString() ?? string.Empty
                    };

                    resources.Add(resource);
                }
            }

            var output = new GetResourceUsageOutput { ResourceUsage = resources };
            _logger.LogInformation("GetResourceUsage: namespace={Namespace}, podName={PodName}, returned {ResourceCount} entries",
                args.Namespace, args.PodName ?? "all", resources.Count);

            return CreateSuccessResult(output);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetResourceUsage failed for namespace {Namespace}", args.Namespace);
            return CreateErrorResult($"Failed to get resource usage: {ex.Message}");
        }
    }

    public async Task<ToolResult> ExecuteCommandAsync(ExecuteCommandArguments args, CancellationToken cancellationToken = default)
    {
        // Mock execution - return a simple message
        await Task.Delay(50, cancellationToken);

        var output = new ExecuteCommandOutput
        {
            Stdout = new List<string> { $"Mock command execution in {args.Namespace}/{args.PodName}: {string.Join(" ", args.Command)}" },
            Stderr = new List<string>(),
            ExitCode = 0
        };

        _logger.LogInformation("ExecuteCommand: namespace={Namespace}, pod={PodName}, command={Command}",
            args.Namespace, args.PodName, string.Join(" ", args.Command));

        return CreateSuccessResult(output);
    }

    /// <summary>
    /// Resolves the snapshot folder path based on relative date logic.
    /// </summary>
    private string ResolveSnapshotFolder(DateTimeOffset? asOf = null)
    {
        var today = DateOnly.FromDateTime(DateTimeOffset.UtcNow.Date);
        var target = asOf.HasValue
            ? DateOnly.FromDateTime(asOf.Value.UtcDateTime.Date)
            : today;

        var daysAgo = today.DayNumber - target.DayNumber;

        _logger.LogDebug("Snapshot resolution: today={Today}, target={Target}, daysAgo={DaysAgo}",
            today, target, daysAgo);

        var result = daysAgo switch
        {
            <= 0 => "day-0",
            1 => "day-minus-1",
            _ => "day-minus-2"
        };

        _logger.LogDebug("Resolved snapshot folder to: {Folder}", result);
        return result;
    }

    /// <summary>
    /// Rewrites hardcoded dates in log lines to be relative to today.
    /// 2026-03-16 → today
    /// 2026-03-15 → yesterday
    /// 2026-03-14 → 2 days ago
    /// </summary>
    private string RewriteLogTimestamps(string logLine)
    {
        var today = DateTimeOffset.UtcNow.Date;
        var yesterday = today.AddDays(-1);
        var twoDaysAgo = today.AddDays(-2);

        // Replace hardcoded dates (assuming format YYYY-MM-DD)
        var result = logLine
            .Replace("2026-03-16", today.ToString("yyyy-MM-dd"))
            .Replace("2026-03-15", yesterday.ToString("yyyy-MM-dd"))
            .Replace("2026-03-14", twoDaysAgo.ToString("yyyy-MM-dd"));

        return result;
    }

    /// <summary>
    /// Simulates network latency.
    /// </summary>
    private async Task SimulateLatencyAsync(int minMs, int maxMs)
    {
        var delay = _random.Next(minMs, maxMs + 1);
        await Task.Delay(delay);
    }

    /// <summary>
    /// Creates a successful ToolResult.
    /// </summary>
    private ToolResult CreateSuccessResult(object output)
    {
        return new ToolResult
        {
            TraceId = Guid.NewGuid().ToString(),
            SessionId = Guid.NewGuid().ToString(),
            ToolName = "MockKubernetesClient",
            Success = true,
            Output = output,
            Error = null
        };
    }

    /// <summary>
    /// Creates an error ToolResult.
    /// </summary>
    private ToolResult CreateErrorResult(string error)
    {
        return new ToolResult
        {
            TraceId = Guid.NewGuid().ToString(),
            SessionId = Guid.NewGuid().ToString(),
            ToolName = "MockKubernetesClient",
            Success = false,
            Output = null,
            Error = error
        };
    }
}
