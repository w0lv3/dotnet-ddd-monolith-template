namespace Example.Application.Exceptions;

public sealed class ExampleNotFoundException : Exception
{
    public ExampleNotFoundException(Guid id)
        : base($"Example entity with identifier '{id}' was not found.")
    {
        Id = id;
    }

    public Guid Id { get; }
}
