using FluentValidation;
using ToolExecution.Domain.Models;

namespace ToolExecution.Application.Validators;

/// <summary>
/// Validator for GetDeploymentsArguments.
/// Ensures the namespace is required and valid.
/// </summary>
public class GetDeploymentsArgumentsValidator : AbstractValidator<GetDeploymentsArguments>
{
    public GetDeploymentsArgumentsValidator()
    {
        RuleFor(x => x.Namespace)
            .NotEmpty()
            .WithMessage("Namespace is required")
            .MaximumLength(253)
            .WithMessage("Namespace must not exceed 253 characters");
    }
}
