# ToolExecutionService

ASP.NET Core (.NET 8) service implementing a Tool Execution Service for safe Kubernetes operations.

Projects:
- ToolExecution.API
- ToolExecution.Application
- ToolExecution.Domain
- ToolExecution.Infrastructure

Run (from repo root):

```powershell
dotnet run --project ToolExecution.API
```

Build docker image:

```powershell
docker build -t tool-execution-service .
```
