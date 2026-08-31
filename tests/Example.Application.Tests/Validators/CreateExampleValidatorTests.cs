using Example.Application.Models.Examples;
using Example.Application.Validators.Examples;
using Example.Domain.ValueObjects;

namespace Example.Application.Tests.Validators;

public sealed class CreateExampleValidatorTests
{
    private readonly CreateExampleValidator validator = new();

    [Fact]
    public void Validate_WithValidModel_IsValid()
    {
        var result = validator.Validate(new CreateExampleModel("Example name"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithNameAtMaximumLength_IsValid()
    {
        var model = new CreateExampleModel(new string('a', ExampleName.MaximumLength));

        var result = validator.Validate(model);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_IsInvalid(string? name)
    {
        var result = validator.Validate(new CreateExampleModel(name));

        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(CreateExampleModel.Name));
    }

    [Fact]
    public void Validate_WithNameOverMaximumLength_IsInvalid()
    {
        var model = new CreateExampleModel(new string('a', ExampleName.MaximumLength + 1));

        var result = validator.Validate(model);

        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(CreateExampleModel.Name));
    }
}
