using FluentValidation;
using ToolExecution.Domain.Models;

namespace ToolExecution.Application.Validators;

/// <summary>
/// Validator for ListPodsArguments.
/// Ensures the namespace is required and valid.
/// </summary>
public class ListPodsArgumentsValidator : AbstractValidator<ListPodsArguments>
{
    public ListPodsArgumentsValidator()
    {
        RuleFor(x => x.Namespace)
            .NotEmpty()
            .WithMessage("Namespace is required")
            .MaximumLength(253)
            .WithMessage("Namespace must not exceed 253 characters");
    }
}
