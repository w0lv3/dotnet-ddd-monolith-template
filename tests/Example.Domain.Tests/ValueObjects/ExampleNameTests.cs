using Example.Domain.Exceptions;
using Example.Domain.ValueObjects;

namespace Example.Domain.Tests.ValueObjects;

public sealed class ExampleNameTests
{
    [Fact]
    public void Create_WithValidValue_CreatesName()
    {
        var name = ExampleName.Create("Example name");

        Assert.Equal("Example name", name.Value);
        Assert.Equal("Example name", name.ToString());
    }

    [Fact]
    public void Create_TrimsLeadingAndTrailingWhitespace()
    {
        var name = ExampleName.Create("  Example name  ");

        Assert.Equal("Example name", name.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingValue_ThrowsDomainException(string? value)
    {
        Assert.Throws<DomainException>(() => ExampleName.Create(value));
    }

    [Fact]
    public void Create_WithValueOverMaximumLength_ThrowsDomainException()
    {
        var value = new string('a', ExampleName.MaximumLength + 1);

        Assert.Throws<DomainException>(() => ExampleName.Create(value));
    }

    [Fact]
    public void Names_WithSameNormalizedValue_AreEqual()
    {
        var first = ExampleName.Create("Example name");
        var second = ExampleName.Create("  Example name  ");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Names_WithDifferentCase_AreNotEqual()
    {
        var first = ExampleName.Create("Example name");
        var second = ExampleName.Create("example name");

        Assert.NotEqual(first, second);
    }
}
