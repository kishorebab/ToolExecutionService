using FluentValidation;
using ToolExecution.Domain.Models;

namespace ToolExecution.Application.Validators;

/// <summary>
/// Validator for ExecuteCommandArguments.
/// Ensures all required fields are present: namespace, pod name, and a non-empty command list.
/// </summary>
public class ExecuteCommandArgumentsValidator : AbstractValidator<ExecuteCommandArguments>
{
    public ExecuteCommandArgumentsValidator()
    {
        RuleFor(x => x.Namespace)
            .NotEmpty()
            .WithMessage("Namespace is required")
            .MaximumLength(253)
            .WithMessage("Namespace must not exceed 253 characters");

        RuleFor(x => x.PodName)
            .NotEmpty()
            .WithMessage("PodName is required")
            .MaximumLength(253)
            .WithMessage("PodName must not exceed 253 characters");

        RuleFor(x => x.Command)
            .NotEmpty()
            .WithMessage("Command list cannot be empty")
            .Must(x => x.Count > 0)
            .WithMessage("At least one command argument is required");

        RuleForEach(x => x.Command)
            .NotEmpty()
            .WithMessage("Command arguments cannot be empty strings");
    }
}
