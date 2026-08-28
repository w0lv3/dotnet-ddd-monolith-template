using Example.Application.Models.Examples;
using Example.Domain.ValueObjects;
using FluentValidation;

namespace Example.Application.Validators.Examples;

public sealed class CreateExampleValidator : AbstractValidator<CreateExampleModel>
{
    public CreateExampleValidator()
    {
        RuleFor(model => model.Name)
            .NotEmpty()
            .MaximumLength(ExampleName.MaximumLength);
    }
}
