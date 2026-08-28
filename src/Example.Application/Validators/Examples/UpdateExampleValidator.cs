using Example.Application.Models.Examples;
using Example.Domain.ValueObjects;
using FluentValidation;

namespace Example.Application.Validators.Examples;

public sealed class UpdateExampleValidator : AbstractValidator<UpdateExampleModel>
{
    public UpdateExampleValidator()
    {
        RuleFor(model => model.Id)
            .NotEmpty();

        RuleFor(model => model.Name)
            .NotEmpty()
            .MaximumLength(ExampleName.MaximumLength);
    }
}
