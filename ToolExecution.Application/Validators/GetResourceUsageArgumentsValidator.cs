using FluentValidation;
using ToolExecution.Domain.Models;

namespace ToolExecution.Application.Validators;

/// <summary>
/// Validator for GetResourceUsageArguments.
/// Ensures the namespace is required, PodName is optional but valid when provided.
/// </summary>
public class GetResourceUsageArgumentsValidator : AbstractValidator<GetResourceUsageArguments>
{
    public GetResourceUsageArgumentsValidator()
    {
        RuleFor(x => x.Namespace)
            .NotEmpty()
            .WithMessage("Namespace is required")
            .MaximumLength(253)
            .WithMessage("Namespace must not exceed 253 characters");

        RuleFor(x => x.PodName)
            .MaximumLength(253)
            .When(x => x.PodName != null)
            .WithMessage("PodName must not exceed 253 characters");
    }
}
