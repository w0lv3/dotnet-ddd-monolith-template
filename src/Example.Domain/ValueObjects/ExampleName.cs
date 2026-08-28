using Example.Domain.Exceptions;

namespace Example.Domain.ValueObjects;

public sealed record ExampleName
{
    public const int MaximumLength = 100;

    private ExampleName(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ExampleName Create(string? value)
    {
        var normalizedValue = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            throw new DomainException("Example name is required.");
        }

        if (normalizedValue.Length > MaximumLength)
        {
            throw new DomainException($"Example name cannot exceed {MaximumLength} characters.");
        }

        return new ExampleName(normalizedValue);
    }

    public override string ToString() => Value;
}
