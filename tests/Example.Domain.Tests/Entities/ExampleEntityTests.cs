using Example.Domain.Entities;
using Example.Domain.Enums;
using Example.Domain.Exceptions;
using Example.Domain.ValueObjects;

namespace Example.Domain.Tests.Entities;

public sealed class ExampleEntityTests
{
    [Fact]
    public void Create_WithValidArguments_CreatesInactiveEntity()
    {
        var id = Guid.NewGuid();
        var name = ExampleName.Create("Example name");

        var entity = ExampleEntity.Create(id, name);

        Assert.Equal(id, entity.Id);
        Assert.Equal(name, entity.Name);
        Assert.Equal(ExampleStatus.Inactive, entity.Status);
    }

    [Fact]
    public void Create_WithEmptyIdentifier_ThrowsDomainException()
    {
        var name = ExampleName.Create("Example name");

        Assert.Throws<DomainException>(() => ExampleEntity.Create(Guid.Empty, name));
    }

    [Fact]
    public void Create_WithNullName_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => ExampleEntity.Create(Guid.NewGuid(), null));
    }

    [Fact]
    public void Rename_WithValidName_ChangesName()
    {
        var entity = CreateEntity();
        var newName = ExampleName.Create("Renamed example");

        entity.Rename(newName);

        Assert.Equal(newName, entity.Name);
    }

    [Fact]
    public void Rename_WithNullName_ThrowsAndPreservesName()
    {
        var entity = CreateEntity();
        var originalName = entity.Name;

        Assert.Throws<DomainException>(() => entity.Rename(null));
        Assert.Equal(originalName, entity.Name);
    }

    [Fact]
    public void Activate_WhenInactive_ChangesStatusToActive()
    {
        var entity = CreateEntity();

        entity.Activate();

        Assert.Equal(ExampleStatus.Active, entity.Status);
    }

    [Fact]
    public void Activate_WhenActive_ThrowsAndPreservesStatus()
    {
        var entity = CreateEntity();
        entity.Activate();

        Assert.Throws<DomainException>(entity.Activate);
        Assert.Equal(ExampleStatus.Active, entity.Status);
    }

    [Fact]
    public void Deactivate_WhenActive_ChangesStatusToInactive()
    {
        var entity = CreateEntity();
        entity.Activate();

        entity.Deactivate();

        Assert.Equal(ExampleStatus.Inactive, entity.Status);
    }

    [Fact]
    public void Deactivate_WhenInactive_ThrowsAndPreservesStatus()
    {
        var entity = CreateEntity();

        Assert.Throws<DomainException>(entity.Deactivate);
        Assert.Equal(ExampleStatus.Inactive, entity.Status);
    }

    private static ExampleEntity CreateEntity()
    {
        return ExampleEntity.Create(Guid.NewGuid(), ExampleName.Create("Example name"));
    }
}
