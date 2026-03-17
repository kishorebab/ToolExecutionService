# ToolExecutionService Refactoring - Complete Documentation

## 🎯 Overview

The ToolExecutionService has been refactored from a simple tool caller into a **structured, validated, observable execution engine** while preserving all existing functionality and mock Kubernetes logic.

### Key Achievement
✅ **Generic tool execution pipeline** - Any ITool can be registered and executed through a unified interface
✅ **Input validation** - Full JSON schema validation before execution
✅ **Execution context** - Support for plan tracing, step IDs, correlation IDs
✅ **Comprehensive logging** - Execution start/end, parameters, duration, errors
✅ **Safe error handling** - All exceptions caught, never throw raw errors to API
✅ **Mock Kubernetes preserved** - All original K8s logic intact

---

## 📁 Updated Folder Structure

```
ToolExecution.Domain/
  Models/
    ExecutionContext.cs          [NEW] - Execution context with plan/step tracking
    ITool.cs                     [EXISTING]
    ToolDefinition.cs            [EXISTING]
    ToolRequest.cs               [EXISTING]
    ToolResponse.cs              [EXISTING, unchanged]
    ...

ToolExecution.Application/
  Services/
    ToolExecutor.cs              [NEW] - Generic execution service implementing IToolExecutor
    ToolExecutorService.cs        [EXISTING] - Legacy typed service (kept for backward compat)
  Contracts/
    IToolExecutor.cs             [EXISTING]
    IToolExecutorService.cs       [EXISTING]
    ...

ToolExecution.Infrastructure/
  Tools/                          [NEW DIRECTORY]
    ListPodsTool.cs              [NEW] - Kubernetes tool implementation
    GetPodLogsTool.cs            [NEW] - Kubernetes tool implementation
    GetDeploymentsTool.cs        [NEW] - Kubernetes tool implementation
    GetResourceUsageTool.cs      [NEW] - Kubernetes tool implementation
    ExecuteCommandTool.cs        [NEW] - Kubernetes tool implementation
  
  SampleTools/
    EchoTool.cs                  [EXISTING]
    MathAddTool.cs               [EXISTING]
  
  Clients/
    KubernetesClient.cs          [EXISTING - mock logic preserved]
    IKubernetesClient.cs         [EXISTING]
  
  Registries/
    InMemoryToolRegistry.cs      [EXISTING]
  ...
```

---

## 🔧 Core Components

### 1. ExecutionContext Model
**File:** `ToolExecution.Domain.Models.ExecutionContext`

Provides execution tracing and context:
```csharp
public class ExecutionContext
{
    public required string ExecutionId { get; init; }      // Unique execution ID
    public string? PlanId { get; init; }                   // Links to orchestration plan
    public string? StepId { get; init; }                   // Position in execution sequence
    public string? CorrelationId { get; init; }            // Request correlation
    public string? RequestedBy { get; init; }              // Who requested execution
    public string? Namespace { get; init; }                // Kubernetes namespace (reusable)
    public DateTime CreatedAt { get; init; }               // Timestamp
    public IDictionary<string, object?> Metadata { get; init; }  // Extensible
}
```

Factory methods:
- `ExecutionContext.Create()` - Simple execution
- `ExecutionContext.CreateForPlanStep()` - Plan-based execution

### 2. ToolExecutor Service
**File:** `ToolExecution.Application.Services.ToolExecutor`

Implements the complete execution pipeline:

```csharp
public class ToolExecutor : IToolExecutor
{
    public async Task<ToolResponse> ExecuteAsync(
        string toolName,
        IDictionary<string, object?> input,
        string? traceId = null,
        string? correlationId = null,
        string? requestedBy = null,
        CancellationToken cancellationToken = default);

    public IReadOnlyCollection<ToolDefinition> ListTools();
    public ToolDefinition? GetTool(string toolName);
}
```

**Execution Pipeline:**
1. ✅ Resolve tool from registry
2. ✅ Check if tool is enabled
3. ✅ Validate input against JSON schema
4. ✅ Execute tool with timeout (from ToolDefinition)
5. ✅ Measure execution time
6. ✅ Handle exceptions safely
7. ✅ Return standardized ToolResponse

**Error Handling:**
- Tool not found → `ExecutionStatus.NotFound`
- Tool disabled → `ExecutionStatus.Disabled`
- Input validation failed → `ExecutionStatus.ValidationError`
- Execution timeout → `ExecutionStatus.Timeout`
- Execution cancelled → `ExecutionStatus.Cancelled`
- Execution failed → `ExecutionStatus.Failed`
- Success → `ExecutionStatus.Success`

**Logging:**
- Tool execution start (with traceId, correlationId, tool name)
- Tool execution complete (with execution time)
- Validation failures (with details)
- Timeouts and cancellations
- Unexpected errors (with stack trace)

### 3. Kubernetes Tool Implementations

All Kubernetes tools implement `ITool` interface and use the `IKubernetesClient` (mock).

#### ListPodsTool
- **Name:** `list-pods`
- **Input:** `{namespace: string}`
- **Output:** `{pods: PodInfo[]}`
- **Timeout:** 30s
- **Idempotent:** True

#### GetPodLogsTool
- **Name:** `get-pod-logs`
- **Input:** `{namespace, podName, containerName?, tailLines?}`
- **Output:** `{logs: string[]}`
- **Timeout:** 30s
- **Idempotent:** True

#### GetDeploymentsTool
- **Name:** `get-deployments`
- **Input:** `{namespace: string}`
- **Output:** `{deployments: DeploymentInfo[]}`
- **Timeout:** 30s
- **Idempotent:** True

#### GetResourceUsageTool
- **Name:** `get-resource-usage`
- **Input:** `{namespace, podName?}`
- **Output:** `{resourceUsage: ResourceUsageInfo[]}`
- **Timeout:** 30s
- **Idempotent:** True

#### ExecuteCommandTool
- **Name:** `execute-command`
- **Input:** `{namespace, podName, command: string[]}`
- **Output:** `{stdout: string[], stderr: string[], exitCode: int}`
- **Timeout:** 60s
- **Idempotent:** False (commands may have side effects)

---

## 🚀 API Endpoints

### 1. List All Tools
```
GET /api/engine/tools
```

**Response:**
```json
{
  "tools": [
    {
      "name": "list-pods",
      "description": "Lists all pods in a Kubernetes namespace.",
      "version": "1.0.0",
      "category": "kubernetes",
      "tags": ["kubernetes", "pods", "list"],
      "isIdempotent": true,
      "isEnabled": true,
      "timeoutSeconds": 30,
      "registeredAt": "2026-03-17T00:00:00Z",
      "inputSchema": "{...JSON schema...}",
      "outputSchema": "{...JSON schema...}"
    }
    // ... more tools
  ],
  "count": 7
}
```

### 2. Get Tool Definition
```
GET /api/engine/tools/{toolName}
```

**Response:** Single tool definition (same as list above)

### 3. Execute Tool
```
POST /api/engine/tools/{toolName}/execute
```

**Request:**
```json
{
  "input": {
    "namespace": "default"
  },
  "traceId": "exec-12345abcde",
  "correlationId": "corr-98765xyz",
  "requestedBy": "user@example.com"
}
```

**Response (Success):**
```json
{
  "traceId": "exec-12345abcde",
  "correlationId": "corr-98765xyz",
  "status": "Success",
  "success": true,
  "output": {
    "pods": [
      {
        "name": "app-1",
        "namespace": "default",
        "status": "Running",
        "readyContainers": 1,
        "totalContainers": 1
      },
      {
        "name": "app-2",
        "namespace": "default",
        "status": "Running",
        "readyContainers": 1,
        "totalContainers": 1
      }
    ]
  },
  "errorMessage": null,
  "executionTimeMs": 52,
  "completedAt": "2026-03-17T10:30:45.123Z"
}
```

**Response (Validation Error):**
```json
{
  "traceId": "exec-12345abcde",
  "correlationId": "corr-98765xyz",
  "status": "ValidationError",
  "success": false,
  "output": null,
  "errorMessage": "Input validation failed: $.namespace: 'required' keyword failed validation.",
  "executionTimeMs": 5,
  "completedAt": "2026-03-17T10:30:45.128Z"
}
```

**Response (Tool Not Found):**
```json
{
  "traceId": "exec-12345abcde",
  "correlationId": "corr-98765xyz",
  "status": "NotFound",
  "success": false,
  "output": null,
  "errorMessage": "Tool 'invalid-tool' not found in registry.",
  "executionTimeMs": 2,
  "completedAt": "2026-03-17T10:30:45.130Z"
}
```

**Response (Timeout):**
```json
{
  "traceId": "exec-12345abcde",
  "correlationId": "corr-98765xyz",
  "status": "Timeout",
  "success": false,
  "output": null,
  "errorMessage": "Tool 'list-pods' execution exceeded timeout of 30 seconds.",
  "executionTimeMs": 30015,
  "completedAt": "2026-03-17T10:31:15.145Z"
}
```

### 4. Health Check
```
GET /health/tools
```

**Response:**
```json
{
  "status": "healthy",
  "registeredToolCount": 7,
  "enabledToolCount": 7,
  "timestamp": "2026-03-17T10:30:45Z"
}
```

---

## 📋 Example Request/Response Scenarios

### Scenario 1: List Pods - Success
**Request:**
```bash
curl -X POST http://localhost:5000/api/engine/tools/list-pods/execute \
  -H "Content-Type: application/json" \
  -d '{
    "input": {
      "namespace": "default"
    },
    "traceId": "trace-001",
    "correlationId": "user-session-123"
  }'
```

**Response (HTTP 200):**
```json
{
  "traceId": "trace-001",
  "correlationId": "user-session-123",
  "status": "Success",
  "success": true,
  "output": {
    "pods": [
      {
        "name": "app-1",
        "namespace": "default",
        "status": "Running",
        "readyContainers": 1,
        "totalContainers": 1
      },
      {
        "name": "app-2",
        "namespace": "default",
        "status": "Running",
        "readyContainers": 1,
        "totalContainers": 1
      }
    ]
  },
  "errorMessage": null,
  "executionTimeMs": 50,
  "completedAt": "2026-03-17T10:30:45.123Z"
}
```

---

### Scenario 2: Get Pod Logs - Success
**Request:**
```bash
curl -X POST http://localhost:5000/api/engine/tools/get-pod-logs/execute \
  -H "Content-Type: application/json" \
  -d '{
    "input": {
      "namespace": "default",
      "podName": "app-1",
      "containerName": "",
      "tailLines": 50
    },
    "traceId": "trace-002"
  }'
```

**Response (HTTP 200):**
```json
{
  "traceId": "trace-002",
  "correlationId": null,
  "status": "Success",
  "success": true,
  "output": {
    "logs": [
      "[2026-03-17T10:30:00Z] Application started in production mode.",
      "[2026-03-17T10:30:01Z] Listening on port 8080",
      "[2026-03-17T10:30:02Z] Ready to accept requests"
    ]
  },
  "errorMessage": null,
  "executionTimeMs": 48,
  "completedAt": "2026-03-17T10:30:45.150Z"
}
```

---

### Scenario 3: Execute Command - Missing Input
**Request:**
```bash
curl -X POST http://localhost:5000/api/engine/tools/execute-command/execute \
  -H "Content-Type: application/json" \
  -d '{
    "input": {
      "namespace": "default"
    },
    "traceId": "trace-003"
  }'
```

**Response (HTTP 400):**
```json
{
  "traceId": "trace-003",
  "correlationId": null,
  "status": "ValidationError",
  "success": false,
  "output": null,
  "errorMessage": "Input validation failed: $.podName: 'required' keyword failed validation.",
  "executionTimeMs": 3,
  "completedAt": "2026-03-17T10:30:45.167Z"
}
```

---

### Scenario 4: Execute Tool - Tool Not Found
**Request:**
```bash
curl -X POST http://localhost:5000/api/engine/tools/nonexistent-tool/execute \
  -H "Content-Type: application/json" \
  -d '{
    "input": {}
  }'
```

**Response (HTTP 404):**
```json
{
  "traceId": "generated-uuid",
  "correlationId": null,
  "status": "NotFound",
  "success": false,
  "output": null,
  "errorMessage": "Tool 'nonexistent-tool' not found in registry.",
  "executionTimeMs": 1,
  "completedAt": "2026-03-17T10:30:45.170Z"
}
```

---

## 🔐 Input Validation - JSON Schema

Each tool enforces strict input validation using JSON Schema. Example from **list-pods**:

```json
{
  "type": "object",
  "properties": {
    "namespace": {
      "type": "string",
      "description": "Kubernetes namespace to list pods from",
      "default": "default"
    }
  },
  "required": ["namespace"]
}
```

If input doesn't match schema, execution returns `ValidationError` with details:
```json
{
  "status": "ValidationError",
  "errorMessage": "Input validation failed: $.namespace: 'required' keyword failed validation."
}
```

---

## 📊 Tool Definition Metadata

Each tool has complete metadata describing inputs, outputs, and constraints:

```csharp
new ToolDefinition
{
    Name = "list-pods",                              // Unique tool name
    Description = "Lists all pods in a Kubernetes namespace.",
    Version = "1.0.0",                               // Semantic versioning
    Category = "kubernetes",                         // Classification
    Tags = ["kubernetes", "pods", "list"],          // Searchable tags
    IsIdempotent = true,                             // Can be retried safely
    TimeoutSeconds = 30,                             // Max execution time
    IsEnabled = true,                                // Can be disabled
    InputSchema = "{...JSON schema...}",             // Strict validation
    OutputSchema = "{...JSON schema...}"             // Contract for output
}
```

---

## 🎮 Dependency Injection Configuration

**File:** `ToolExecution.API.Program.cs`

```csharp
// Register Tool Engine
builder.Services.AddSingleton<IToolRegistry, InMemoryToolRegistry>();
builder.Services.AddScoped<IToolExecutor, ToolExecutor>();  // NEW
builder.Services.AddScoped<IToolExecutorService, ToolExecutorService>();  // Legacy

// Register Kubernetes Client (mock or real)
var kubernetesClient = builder.Configuration["KUBERNETES_CLIENT"] ?? "mock";
if (kubernetesClient == "real")
    builder.Services.AddSingleton<IKubernetesClient, KubernetesClient>();
else
    builder.Services.AddSingleton<IKubernetesClient, MockKubernetesClient>();

// Initialize and register tools
var toolRegistry = app.Services.GetRequiredService<IToolRegistry>();
var kubernetesClientInstance = app.Services.GetRequiredService<IKubernetesClient>();

// Sample tools
toolRegistry.Register(new EchoTool());
toolRegistry.Register(new MathAddTool());

// Kubernetes tools
toolRegistry.Register(new ListPodsTool(kubernetesClientInstance));
toolRegistry.Register(new GetPodLogsTool(kubernetesClientInstance));
toolRegistry.Register(new GetDeploymentsTool(kubernetesClientInstance));
toolRegistry.Register(new GetResourceUsageTool(kubernetesClientInstance));
toolRegistry.Register(new ExecuteCommandTool(kubernetesClientInstance));
```

---

## 📝 Logging Output

**Example logs during execution:**

```
2026-03-17 10:30:45.120Z [INF] Tool Engine initialized with 7 registered tools
2026-03-17 10:30:45.500Z [INF] Executing tool 'list-pods' with input parameters (TraceId: exec-001, CorrelationId: user-123)
2026-03-17 10:30:45.535Z [INF] Tool 'list-pods' execution completed successfully in 52ms (TraceId: exec-001)
2026-03-17 10:30:46.100Z [WRN] Tool 'invalid-tool' not found in registry (TraceId: exec-002)
2026-03-17 10:30:46.105Z [WRN] Input validation failed for tool 'execute-command': $.podName: 'required' keyword failed validation. (TraceId: exec-003)
2026-03-17 10:31:15.150Z [ERR] Tool 'list-pods' execution timed out after 30s (TraceId: exec-004)
```

---

## ✅ What's Preserved

- ✅ All existing endpoints in `ToolExecutionController`
- ✅ Mock Kubernetes logic in `KubernetesClient`
- ✅ Sample tools (Echo, MathAdd)
- ✅ Backward compatibility with `IToolExecutorService`
- ✅ Validators and validation pipeline
- ✅ OpenTelemetry integration
- ✅ Serilog logging

---

## 🚀 Next Steps (Not Implemented)

These features are ready for future implementation without breaking current code:

1. **Step Dependencies** - Execute tools in sequence with output chaining
2. **Dynamic Parameter Resolution** - Reference previous step outputs (${step1.output.pods})
3. **Parallel Execution** - Run multiple tools concurrently
4. **Agent Orchestration** - Coordinate complex multi-step workflows
5. **Tool Middleware** - Pre/post execution hooks
6. **Persistent Retry** - Track retries across restarts
7. **Tool Versioning** - Multiple versions of same tool
8. **Authorization** - Role-based tool access

---

## 🧪 Testing

To test the refactored service:

```bash
# List all tools
curl http://localhost:5000/api/engine/tools

# Execute list-pods
curl -X POST http://localhost:5000/api/engine/tools/list-pods/execute \
  -H "Content-Type: application/json" \
  -d '{"input": {"namespace": "default"}}'

# Execute with tracing
curl -X POST http://localhost:5000/api/engine/tools/get-pod-logs/execute \
  -H "Content-Type: application/json" \
  -d '{
    "input": {"namespace": "default", "podName": "app-1"},
    "traceId": "test-123",
    "correlationId": "session-xyz"
  }'

# Health check
curl http://localhost:5000/health/tools
```

---

## 📌 Summary

The ToolExecutionService is now a **production-ready execution engine** with:

✅ Unified execution pipeline for any tool
✅ Strict input validation
✅ JSON Schema support
✅ Execution context tracking
✅ Comprehensive logging
✅ Safe error handling
✅ Kubernetes tools implementing ITool
✅ Full backward compatibility
✅ Ready for agent orchestration

All changes follow **Clean Architecture principles** and maintain **incremental, non-breaking refactoring**.
