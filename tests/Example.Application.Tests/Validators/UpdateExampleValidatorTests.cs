using Example.Application.Models.Examples;
using Example.Application.Validators.Examples;
using Example.Domain.ValueObjects;

namespace Example.Application.Tests.Validators;

public sealed class UpdateExampleValidatorTests
{
    private readonly UpdateExampleValidator validator = new();

    [Fact]
    public void Validate_WithValidModel_IsValid()
    {
        var result = validator.Validate(new UpdateExampleModel(Guid.NewGuid(), "Example name"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyIdentifier_IsInvalid()
    {
        var result = validator.Validate(new UpdateExampleModel(Guid.Empty, "Example name"));

        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(UpdateExampleModel.Id));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_IsInvalid(string? name)
    {
        var result = validator.Validate(new UpdateExampleModel(Guid.NewGuid(), name));

        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(UpdateExampleModel.Name));
    }

    [Fact]
    public void Validate_WithNameOverMaximumLength_IsInvalid()
    {
        var model = new UpdateExampleModel(
            Guid.NewGuid(),
            new string('a', ExampleName.MaximumLength + 1));

        var result = validator.Validate(model);

        Assert.Contains(result.Errors, failure => failure.PropertyName == nameof(UpdateExampleModel.Name));
    }
}
