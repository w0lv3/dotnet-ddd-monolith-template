using Example.Domain.Enums;
using Example.Domain.Exceptions;
using Example.Domain.ValueObjects;

namespace Example.Domain.Entities;

public sealed class ExampleEntity
{
    private ExampleEntity(Guid id, ExampleName name)
    {
        Id = id;
        Name = name;
        Status = ExampleStatus.Inactive;
    }

    public Guid Id { get; }

    public ExampleName Name { get; private set; }

    public ExampleStatus Status { get; private set; }

    public static ExampleEntity Create(Guid id, ExampleName? name)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Example entity identifier cannot be empty.");
        }

        return new ExampleEntity(id, EnsureName(name));
    }

    public void Rename(ExampleName? name)
    {
        Name = EnsureName(name);
    }

    public void Activate()
    {
        EnsureStatusTransition(ExampleStatus.Inactive, ExampleStatus.Active);
        Status = ExampleStatus.Active;
    }

    public void Deactivate()
    {
        EnsureStatusTransition(ExampleStatus.Active, ExampleStatus.Inactive);
        Status = ExampleStatus.Inactive;
    }

    private static ExampleName EnsureName(ExampleName? name)
    {
        return name ?? throw new DomainException("Example name is required.");
    }

    private void EnsureStatusTransition(ExampleStatus expectedStatus, ExampleStatus targetStatus)
    {
        if (Status != expectedStatus)
        {
            throw new DomainException($"Example entity cannot transition from '{Status}' to '{targetStatus}'.");
        }
    }
}
