# Clean Architecture Overview - ToolExecutionService

## Architectural Layers

### 🏠 **Domain Layer** (`ToolExecution.Domain`)
**Responsibility:** Pure business models and domain logic. No dependencies on other layers.

**Contents:**
- `Models/ToolCall.cs` - Internal domain model for tool calls (used by application internals)
- `Models/ToolResult.cs` - Internal domain model for tool results
- `Models/ToolExecutionMetrics.cs` - Metrics data structure
- `Models/ToolExecutionRequest<T>` - Generic request wrapper (public API contract)
- `Models/ToolExecutionResponse<T>` - Generic response wrapper (public API contract)
- `Models/ToolExecutionStatus.cs` - Enum for execution status
- `Models/*Dto.cs` - Tool-specific argument and output models
  - `GetPodLogsArguments` / `GetPodLogsOutput`
  - `ListPodsArguments` / `ListPodsOutput`
  - `GetDeploymentsArguments` / `GetDeploymentsOutput`
  - `GetResourceUsageArguments` / `GetResourceUsageOutput`
  - `ExecuteCommandArguments` / `ExecuteCommandOutput`

**Key Principle:** Domain models are **environment-agnostic** and contain all business rules.

---

### 🔌 **Infrastructure Layer** (`ToolExecution.Infrastructure`)
**Responsibility:** External service integrations, I/O operations, and cross-cutting concerns.

**Contents:**
- `Clients/IKubernetesClient.cs` - Abstract interface for Kubernetes operations
- `Clients/KubernetesClient.cs` - Implementation (mock for demo)
- `Policies/PolicyProvider.cs` - Retry policies configuration

**Dependencies:**
- ✅ Depends on `Domain` models
- ❌ Does NOT depend on `Application` or `API` layers

**Key Principle:** Infrastructure implements domain interfaces; application depends on Infrastructure abstractions.

---

### 🎯 **Application Layer** (`ToolExecution.Application`)
**Responsibility:** Use cases, business logic orchestration, validation, and service contracts.

**Contents:**
- `Contracts/IToolExecutorService.cs` - Public service interface (what clients should use)
- `Services/ToolExecutorService.cs` - Strongly-typed service implementation
- `Validators/` - FluentValidation validators for arguments
  - `GetPodLogsArgumentsValidator`
  - `ListPodsArgumentsValidator`
  - `GetDeploymentsArgumentsValidator`
  - `GetResourceUsageArgumentsValidator`
  - `ExecuteCommandArgumentsValidator`

**Dependencies:**
- ✅ Depends on `Domain` models
- ✅ Depends on `Infrastructure` abstractions
- ❌ Does NOT depend on `API` layer

**Key Principle:** Application layer is framework-agnostic. Could be called from Web API, gRPC, CLI, etc.

---

### 🌐 **API Layer** (`ToolExecution.API`)
**Responsibility:** HTTP transport, request routing, serialization, and middleware.

**Contents:**
- `Controllers/ToolExecutionController.cs` - REST endpoint definitions
- `Middleware/RequestValidationMiddleware.cs` - Request validation pipeline
- `Program.cs` - Dependency injection and middleware configuration

**Dependencies:**
- ✅ Depends on `Domain` models
- ✅ Depends on `Application` contracts
- ✅ Depends on `Infrastructure` abstractions

**Key Principle:** Thin layer that translates HTTP to application service calls. All business logic lives in Application layer.

---

## 🔄 Component Interaction Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Client sends HTTP POST to /api/tools/get-pod-logs             │
└──────────┬──────────────────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. RequestValidationMiddleware (API Layer)                       │
│    - Deserializes ToolExecutionRequest<GetPodLogsArguments>      │
│    - Routes to correct validator                                 │
│    - Validates arguments via FluentValidation                    │
│    - Returns 400 if invalid, continues if valid                  │
└──────────┬──────────────────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. ToolExecutionController.GetPodLogs() (API Layer)              │
│    - Receives strongly-typed ToolExecutionRequest<...>           │
│    - Calls IToolExecutorService.GetPodLogsAsync()                │
│    - Returns ToolExecutionResponse<GetPodLogsOutput>             │
└──────────┬──────────────────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. ToolExecutorService.GetPodLogsAsync() (Application Layer)     │
│    - Starts OpenTelemetry Activity for tracing                   │
│    - Starts stopwatch for latency measurement                    │
│    - Wraps call with Polly retry policy                          │
│    - Calls IKubernetesClient.GetPodLogsAsync(args)               │
│    - Maps ToolResult to ToolExecutionResponse<GetPodLogsOutput>  │
│    - Records latency in ToolExecutionMetrics                     │
└──────────┬──────────────────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. KubernetesClient.GetPodLogsAsync() (Infrastructure Layer)     │
│    - Receives strongly-typed GetPodLogsArguments                 │
│    - Performs async operation (real K8s call or mock)            │
│    - Tags Activity with operation details                        │
│    - Returns ToolResult with GetPodLogsOutput                    │
└──────────┬──────────────────────────────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. Response bubbles back through layers (Application → API)      │
│    - ToolExecutionResponse<GetPodLogsOutput> serialized to JSON  │
│    - HTTP 200 OK with full tracing context                       │
│    - Client receives strongly-typed response                     │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📐 Data Flow Example: get-pod-logs

### Request Journey

```json
HTTP POST /api/tools/get-pod-logs
Header: "traceId: abc-123"
Body: {
  "traceId": "abc-123",
  "sessionId": "session-001",
  "toolName": "get-pod-logs",
  "arguments": {
    "namespace": "default",
    "podName": "myapp-pod",
    "containerName": "app",
    "tailLines": 100
  }
}
```

### Step-by-Step Processing

```
┌── API Layer ──────────────────────────────────────────┐
│                                                        │
│ 1. RequestValidationMiddleware receives raw JSON      │
│    ├─ Deserializes to ToolExecutionRequest            │
│    │   <GetPodLogsArguments>                           │
│    ├─ Gets GetPodLogsArgumentsValidator               │
│    └─ Validates arguments                             │
│       ✅ namespace: "default" (not empty) ✓           │
│       ✅ podName: "myapp-pod" (not empty) ✓           │
│       ✅ tailLines: 100 (1-5000) ✓                    │
│       → Validation PASSES                             │
│                                                        │
│ 2. ToolExecutionController.GetPodLogs()               │
│    receives ToolExecutionRequest<                      │
│         GetPodLogsArguments> {                         │
│      traceId: "abc-123",                              │
│      sessionId: "session-001",                        │
│      toolName: "get-pod-logs",                        │
│      arguments: { /* validated */ }                   │
│    }                                                   │
│    → Calls executor.GetPodLogsAsync(request)          │
└────────────────────┬─────────────────────────────────┘
                     │
                     ▼
┌── Application Layer ───────────────────────────────────┐
│                                                        │
│ 3. ToolExecutorService.GetPodLogsAsync()              │
│    ├─ StartActivity("GetPodLogs")                     │
│    ├─ SetTag("traceId", "abc-123")                    │
│    ├─ StartStopwatch()                                │
│    ├─ _retryPolicy.ExecuteAsync(async () => {        │
│    │   return await _k8s.GetPodLogsAsync(             │
│    │     request.Arguments  // GetPodLogsArguments    │
│    │   );                                              │
│    │ })                                                │
│    └─ On success:                                      │
│       return ToolExecutionResponse<                    │
│           GetPodLogsOutput> {                         │
│         traceId: "abc-123",                           │
│         sessionId: "session-001",                     │
│         toolName: "get-pod-logs",                     │
│         success: true,                                │
│         output: GetPodLogsOutput {                    │
│           logs: [...]                                 │
│         },                                             │
│         metrics: {                                     │
│           latencyMs: 50                               │
│         },                                             │
│         error: null                                   │
│       }                                                │
└────────────────────┬─────────────────────────────────┘
                     │
                     ▼
┌── Infrastructure Layer ────────────────────────────────┐
│                                                        │
│ 4. KubernetesClient.GetPodLogsAsync(                  │
│      args: GetPodLogsArguments                        │
│    )                                                   │
│    ├─ StartActivity("GetPodLogs")                     │
│    ├─ SetTags(namespace, podName, containerName...)  │
│    ├─ Delay(50ms) [simulated operation]              │
│    └─ Return ToolResult {                            │
│         toolName: "get-pod-logs",                    │
│         success: true,                                │
│         output: GetPodLogsOutput {                    │
│           logs: [                                      │
│             "[2026-02-25...] App started",            │
│             "[2026-02-25...] Listening on 8080",      │
│             "[2026-02-25...] Ready"                   │
│           ]                                            │
│         },                                             │
│         metrics: { latencyMs: 50 }                    │
│       }                                                │
└────────────────────┬─────────────────────────────────┘
                     │
                     ▼
┌── API Layer ──────────────────────────────────────────┐
│                                                        │
│ 5. Controller.GetPodLogs()                            │
│    ├─ Receives ToolExecutionResponse<GetPodLogsOutput>│
│    ├─ Checks response.Success (true)                  │
│    └─ return Ok(response)                             │
│                                                        │
│ 6. HTTP Response                                       │
│    Status: 200 OK                                      │
│    Content-Type: application/json                      │
│    Body: { fully typed JSON }                          │
└────────────────────┬─────────────────────────────────┘
                     │
                     ▼
┌── Client Receives ─────────────────────────────────────┐
│                                                        │
│ 200 OK                                                 │
│ {                                                      │
│   "traceId": "abc-123",                               │
│   "sessionId": "session-001",                         │
│   "toolName": "get-pod-logs",                         │
│   "success": true,                                    │
│   "output": {                                          │
│     "logs": ["[...application logs..."]               │
│   },                                                   │
│   "metrics": {                                         │
│     "latencyMs": 50                                   │
│   },                                                   │
│   "error": null                                       │
│ }                                                      │
└────────────────────────────────────────────────────────┘
```

---

## 🎓 Design Patterns Used

### 1. **Generic Wrapper Pattern**
```csharp
public class ToolExecutionRequest<TArguments> where TArguments : class
public class ToolExecutionResponse<TOutput> where TOutput : class
```
- Enables type-safe request/response at compile time
- Reduces code duplication
- Better Swagger schema generation

### 2. **Repository Pattern** (via Dependency Injection)
```csharp
// Infrastructure implements, Application depends on
IKubernetesClient _k8s;
```

### 3. **Strategy Pattern** (Validators)
```csharp
// Each validator is a strategy for its argument type
IValidator<GetPodLogsArguments> validator;
```

### 4. **Decorator Pattern** (Middleware)
```csharp
// RequestValidationMiddleware decorates the request pipeline
app.UseMiddleware<RequestValidationMiddleware>();
```

### 5. **Template Method Pattern** (Service layer)
```csharp
// Each method follows same: start activity → retry → measure latency
public async Task<ToolExecutionResponse<TOutput>> ExecuteAsync(...)
```

---

## 🔐 Separation of Concerns

| Concern | Layer | Pattern |
|---------|-------|---------|
| **Business Rules** | Domain + Application | Entities, Value Objects, Validators |
| **HTTP Transport** | API | Controllers, Middleware |
| **External Services** | Infrastructure | Clients, Repositories |
| **Validation** | Application | FluentValidation |
| **Tracing** | Application | OpenTelemetry |
| **Retry Logic** | Application | Polly |
| **Serialization** | API | Json.NET (implicit in ASP.NET) |

---

## 🔄 Extensibility Points

### Adding a New Tool

**1. Domain Layer** - Define the contract:
```csharp
// ToolExecution.Domain/Models/MyToolDto.cs
public class MyToolArguments
{
    public string RequiredField { get; set; }
}

public class MyToolOutput
{
    public string Result { get; set; }
}
```

**2. Application Layer** - Create validator:
```csharp
// ToolExecution.Application/Validators/MyToolArgumentsValidator.cs
public class MyToolArgumentsValidator : AbstractValidator<MyToolArguments>
{
    public MyToolArgumentsValidator()
    {
        RuleFor(x => x.RequiredField).NotEmpty();
    }
}
```

**3. Application Layer** - Add to service interface and implementation:
```csharp
// IToolExecutorService.cs
Task<ToolExecutionResponse<MyToolOutput>> MyToolAsync(
    ToolExecutionRequest<MyToolArguments> request,
    CancellationToken cancellationToken);

// ToolExecutorService.cs
public async Task<ToolExecutionResponse<MyToolOutput>> MyToolAsync(...)
{
    // Implementation
}
```

**4. Infrastructure Layer** - Add to Kubernetes client:
```csharp
// IKubernetesClient.cs
Task<ToolResult> MyToolAsync(MyToolArguments args, CancellationToken ct);

// KubernetesClient.cs
public async Task<ToolResult> MyToolAsync(...)
{
    // Implementation
}
```

**5. API Layer** - Add controller endpoint:
```csharp
[HttpPost("my-tool")]
public async Task<ActionResult<ToolExecutionResponse<MyToolOutput>>> MyTool(
    [FromBody] ToolExecutionRequest<MyToolArguments> request,
    CancellationToken ct)
{
    var response = await ExecuteToolWithTracingAsync(
        () => _executor.MyToolAsync(request, ct),
        request.TraceId);
    return response.Success ? Ok(response) : BadRequest(response);
}
```

**6. DI Registration** - Register in Program.cs:
```csharp
builder.Services.AddSingleton<IValidator<MyToolArguments>, MyToolArgumentsValidator>();
```

---

## ✅ Clean Architecture Principles Maintained

✅ **Independence of Frameworks** - Business logic doesn't depend on ASP.NET, Polly, or any framework  
✅ **Testability** - All layers can be unit tested independently  
✅ **Independence of Database** - ToolResult uses in-memory models, no ORM  
✅ **Independence of UI** - API is interchangeable with gRPC, CLI, etc.  
✅ **Independence of any external agency** - Service interfaces abstract all externals  
✅ **Centered on Entities/Business Rules** - Domain models are the core  

---

## 📊 Layer Dependencies (Correct Arrangement)

```
┌─────────────────────────────────┐
│ API Layer                       │ (Web framework)
│ Controllers, Middleware          │ Depends on ↓
├─────────────────────────────────┤
│ Application Layer               │ (Use cases)
│ Services, Validators             │ Depends on ↓ & Infrastructure
├─────────────────────────────────┤
│ Infrastructure Layer            │ (External services)
│ Kubernetes Client, Policies      │ Depends on ↓
├─────────────────────────────────┤
│ Domain Layer                    │ (Pure business)
│ Models, Entities                 │ ZERO dependencies
└─────────────────────────────────┘
```

**Dependency Rule:** Code that sits farther out can depend on code farther in.  
**Inverse is forbidden:** Inner layers can NEVER depend on outer layers.

---

This architecture ensures the service remains:
- **Flexible** - Easy to change implementations
- **Testable** - Each layer mockable independently
- **Maintainable** - Clear responsibilities
- **Scalable** - New tools add minimal coupling
- **Resilient** - Retry and tracing built-in across all tools
