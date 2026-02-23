using ToolExecution.Domain.Models;

namespace ToolExecution.Application.Contracts;

public interface IToolExecutionOrchestratorService
{
    Task<ToolResult> ExecuteAsync(ToolCall call, CancellationToken cancellationToken = default);
}
