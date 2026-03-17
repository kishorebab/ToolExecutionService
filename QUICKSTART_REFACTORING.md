# Quick Start Guide - Refactored ToolExecutionService

## 🚀 Getting Started

The refactored ToolExecutionService is ready to use. No breaking changes to existing code.

### Build & Run
```bash
cd ToolExecution.API
dotnet run
```

Open browser: http://localhost:5000/swagger

---

## 📌 Key Changes at a Glance

| Aspect | What Changed | Impact |
|--------|-------------|--------|
| **Core Service** | Added `ToolExecutor` implementing generic `IToolExecutor` | Supports ANY tool, not just specific ones |
| **Tool Interface** | `ITool` interface (existing) + 5 new Kubernetes implementations | Tools registered in registry at startup |
| **Input Validation** | JSON Schema validation in ToolExecutor pipeline | Invalid inputs rejected before execution |
| **Execution Context** | New `ExecutionContext` model with planId, stepId, namespace | Supports agent plan orchestration |
| **Error Handling** | Structured `ExecutionStatus` enum (7 states) | Never throws raw exceptions to API |
| **Logging** | Tool executor logs all stages + decisions | Full execution tracing |
| **Backward Compat** | `ToolExecutorService` (legacy) still exists | Old code continues working |

---

## 🛠️ How to Use - Example Code

### Option 1: Use ToolsController (Recommended)
The controller is pre-configured and ready to use.

```bash
# List all tools
curl http://localhost:5000/api/engine/tools

# Execute a tool
curl -X POST http://localhost:5000/api/engine/tools/list-pods/execute \
  -H "Content-Type: application/json" \
  -d '{
    "input": {"namespace": "default"},
    "traceId": "my-trace-001",
    "correlationId": "user-session-123"
  }'
```

### Option 2: Inject IToolExecutor in Your Code
```csharp
public class MyService
{
    private readonly IToolExecutor _executor;

    public MyService(IToolExecutor executor)
    {
        _executor = executor;
    }

    public async Task ExecuteListPodsAsync()
    {
        var input = new Dictionary<string, object?>
        {
            { "namespace", "default" }
        };

        var response = await _executor.ExecuteAsync(
            toolName: "list-pods",
            input: input,
            traceId: Guid.NewGuid().ToString("N"),
            correlationId: "user-123"
        );

        if (response.IsSuccess)
        {
            var pods = response.Output["pods"];
            // Handle success
        }
        else
        {
            Console.WriteLine($"Error: {response.ErrorMessage}");
        }
    }
}
```

---

## 📋 Available Tools

### Kubernetes Tools (New)
1. **list-pods** - Lists pods in a namespace
2. **get-pod-logs** - Gets logs from a pod
3. **get-deployments** - Lists deployments
4. **get-resource-usage** - Gets CPU/memory metrics
5. **execute-command** - Runs command in pod

### Sample Tools (Existing)
1. **echo** - Returns input as output
2. **math_add** - Adds two numbers

---

## 🔍 Tool Schema - How to Validate Input

Before executing a tool, get its schema:

```bash
curl http://localhost:5000/api/engine/tools/list-pods
```

Response includes `inputSchema` (JSON Schema):
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

Your input must match this schema, or execution will return `ValidationError`.

---

## 📊 Response Status Codes & Meanings

```
HTTP 200 → Status: "Success"           ✅ Tool executed, output available
HTTP 400 → Status: "ValidationError"   ❌ Input didn't match schema
HTTP 404 → Status: "NotFound"          ❌ Tool not registered
HTTP 408 → Status: "Timeout"           ❌ Tool took too long
HTTP 499 → Status: "Cancelled"         ❌ Execution was cancelled
HTTP 500 → Status: "Failed"            ❌ Tool threw an exception
```

All responses include `executionTimeMs` and `completedAt` timestamp.

---

## 🔐 Input Validation Rules

The system validates inputs **before** executing tools using JSON Schema.

### What Gets Validated?
✅ Required fields present
✅ Type correctness (string, number, array, etc.)
✅ Array item types
✅ Custom constraints (from schema)

### What Happens if Invalid?
Returns immediately with `ValidationError`:
```json
{
  "status": "ValidationError",
  "success": false,
  "errorMessage": "Input validation failed: $.namespace: 'required' keyword failed validation."
}
```

**No tool execution happens** - fast fail with clear error message.

---

## 🎯 Execution Context - For Plan Orchestration

When tools are executed as part of a larger plan:

```csharp
var context = ExecutionContext.CreateForPlanStep(
    planId: "plan-abc123",
    stepId: "1",
    correlationId: "workflow-xyz"
);

var response = await _executor.ExecuteAsync(
    "list-pods",
    input,
    traceId: context.ExecutionId,
    correlationId: context.CorrelationId
);
```

This context will be logged and traced throughout execution, enabling:
- Plan-level debugging
- Step-level timing
- Correlation across service boundaries

---

## 🧪 Example: Complete Workflow

### Scenario: Get logs from app-1 pod

**Step 1: Get tool schema**
```bash
curl http://localhost:5000/api/engine/tools/get-pod-logs
```

**Step 2: Build request from schema**
```json
{
  "input": {
    "namespace": "default",
    "podName": "app-1",
    "containerName": "",
    "tailLines": 50
  },
  "traceId": "exec-001",
  "correlationId": "user-session-123"
}
```

**Step 3: Execute tool**
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
    "traceId": "exec-001",
    "correlationId": "user-session-123"
  }'
```

**Step 4: Handle response**
```json
{
  "traceId": "exec-001",
  "correlationId": "user-session-123",
  "status": "Success",
  "success": true,
  "output": {
    "logs": [
      "[2026-03-17T10:30:00Z] Application started",
      "[2026-03-17T10:30:01Z] Ready to accept requests"
    ]
  },
  "executionTimeMs": 47,
  "completedAt": "2026-03-17T10:30:45.123Z"
}
```

---

## 🔄 How Tools Work - Internally

### Registration (at startup)
```csharp
var registry = app.Services.GetRequiredService<IToolRegistry>();
registry.Register(new ListPodsTool(kubernetesClient));
```

### Execution (when you call the API)
1. ToolsController receives HTTP request
2. Calls `IToolExecutor.ExecuteAsync("list-pods", input, ...)`
3. ToolExecutor resolves tool from registry
4. Validates input against tool's JSON schema
5. Calls tool's `ExecuteAsync()` method
6. Tool uses KubernetesClient to get data
7. Returns ToolResponse with success/failure
8. Controller converts to HTTP response

### All errors caught
If ANY step fails, you get a properly structured ToolResponse with:
- `success: false`
- `status` explaining what went wrong
- `errorMessage` with details
- `executionTimeMs` for diagnostics

Never a raw 500 error.

---

## 🛡️ Error Cases - What to Expect

### Tool Not Found
```bash
curl -X POST http://localhost:5000/api/engine/tools/nonexistent/execute \
  -H "Content-Type: application/json" \
  -d '{"input": {}}'
```

Response (HTTP 404):
```json
{
  "status": "NotFound",
  "success": false,
  "errorMessage": "Tool 'nonexistent' not found in registry."
}
```

### Missing Required Input
```bash
curl -X POST http://localhost:5000/api/engine/tools/list-pods/execute \
  -H "Content-Type: application/json" \
  -d '{"input": {}}' # missing "namespace"
```

Response (HTTP 400):
```json
{
  "status": "ValidationError",
  "success": false,
  "errorMessage": "Input validation failed: $.namespace: 'required' keyword failed validation."
}
```

### Wrong Input Type
```bash
curl -X POST http://localhost:5000/api/engine/tools/math_add/execute \
  -H "Content-Type: application/json" \
  -d '{"input": {"a": "not-a-number", "b": 5}}' # a should be number
```

Response (HTTP 400):
```json
{
  "status": "ValidationError",
  "success": false,
  "errorMessage": "Input validation failed: $.a: 'type' keyword failed validation."
}
```

### Tool Timeout
If a tool takes longer than its `timeoutSeconds`:

Response (HTTP 408):
```json
{
  "status": "Timeout",
  "success": false,
  "errorMessage": "Tool 'list-pods' execution exceeded timeout of 30 seconds."
}
```

---

## 📝 Logging - What Gets Logged

When you execute a tool, you'll see logs like:

```
[INF] Executing tool 'list-pods' with input parameters (TraceId: exec-001, CorrelationId: user-123)
[INF] Tool 'list-pods' execution completed successfully in 52ms (TraceId: exec-001)
```

Or on error:

```
[WRN] Tool 'invalid-tool' not found in registry (TraceId: exec-002)
[WRN] Input validation failed for tool 'list-pods': $.namespace: 'required' keyword failed validation. (TraceId: exec-002)
[ERR] Tool 'list-pods' execution timed out after 30s (TraceId: exec-004)
```

Logs include:
- Tool name being executed
- Input parameters (when successful)
- Execution time in milliseconds
- TraceId for correlation
- Error details (when failed)

---

## 🚀 Creating a New Tool

To add a new tool to the system:

### 1. Create Tool Class
```csharp
namespace ToolExecution.Infrastructure.Tools;

public class MyCustomTool : ITool
{
    public ToolDefinition Definition { get; }

    public MyCustomTool()
    {
        Definition = new ToolDefinition
        {
            Name = "my-tool",
            Description = "Does something useful",
            Version = "1.0.0",
            Category = "custom",
            Tags = ["custom", "myapp"],
            IsIdempotent = true,
            TimeoutSeconds = 30,
            IsEnabled = true,
            InputSchema = """{"type": "object", "properties": {...}}""",
            OutputSchema = """{"type": "object", "properties": {...}}"""
        };
    }

    public async Task<ToolResponse> ExecuteAsync(
        ToolRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Validate inputs
            // Execute logic
            // Return success
            
            return ToolResponse.CreateSuccess(
                request.TraceId,
                outputDictionary,
                stopwatch.ElapsedMilliseconds,
                request.CorrelationId);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return ToolResponse.CreateFailure(
                request.TraceId,
                ex.Message,
                stopwatch.ElapsedMilliseconds,
                ExecutionStatus.Failed,
                request.CorrelationId);
        }
    }
}
```

### 2. Register in Program.cs
```csharp
var toolRegistry = app.Services.GetRequiredService<IToolRegistry>();
toolRegistry.Register(new MyCustomTool());
```

### 3. Use immediately
```bash
curl http://localhost:5000/api/engine/tools/my-tool
```

That's it! The tool is now available via the API.

---

## 📚 Documentation Organization

- **REFACTORING_COMPLETE.md** (this folder) - Complete refactoring guide + API docs
- **README.md** - How to run the service
- **ARCHITECTURE.md** - High-level design
- Each `/Tools` implementation has XML documentation comments

---

## ✨ Key Benefits of Refactoring

Before:
❌ Tight coupling between controllers and specific tool types
❌ No unified execution pipeline
❌ Input validation scattered
❌ Hard to add new tools

After:
✅ Add any tool by implementing ITool
✅ Unified validation and execution
✅ Full tracing and logging
✅ Safe error handling
✅ Ready for agent orchestration
✅ Easy to test (mock tools, etc.)

---

## 🎓 Next: Agent Orchestration

With this foundation, you can now build:

1. **WorkflowPlanner** - Creates execution plans from agent decisions
2. **StepExecutor** - Runs each step, captures outputs
3. **OutputResolver** - Maps step outputs to next step inputs (e.g., `${step1.pods[0].name}`)
4. **WorkflowExecutor** - Orchestrates the entire flow

All without touching the tool execution engine!

---

## 📞 Support

- Check logs with: `docker logs <container>`
- Health check: `curl http://localhost:5000/health/tools`
- List tools: `curl http://localhost:5000/api/engine/tools`
- Swagger UI: http://localhost:5000/swagger

Have questions? Check the code comments or the refactoring guide.
