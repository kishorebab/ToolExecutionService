using FluentValidation;
using ToolExecution.Domain.Models;

namespace ToolExecution.Application.Validators;

/// <summary>
/// Validator for GetPodLogsArguments.
/// Ensures all required fields are present and valid, and TailLines is within acceptable range.
/// </summary>
public class GetPodLogsArgumentsValidator : AbstractValidator<GetPodLogsArguments>
{
    public GetPodLogsArgumentsValidator()
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

        RuleFor(x => x.TailLines)
            .GreaterThan(0)
            .WithMessage("TailLines must be greater than 0")
            .LessThanOrEqualTo(5000)
            .WithMessage("TailLines must not exceed 5000 lines");

        RuleFor(x => x.ContainerName)
            .MaximumLength(253)
            .When(x => x.ContainerName != null)
            .WithMessage("ContainerName must not exceed 253 characters");
    }
}
