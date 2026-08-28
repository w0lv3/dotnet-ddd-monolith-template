using Example.Domain.Entities;
using Example.Domain.Enums;
using Example.Domain.ValueObjects;
using Example.Infrastructure.Persistence.Repositories;

namespace Example.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class ExampleRepositoryTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task AddAndGetByIdAsync_RoundTripsEntity()
    {
        var entity = CreateEntity();
        entity.Activate();

        await AddAsync(entity);

        await using var dbContext = fixture.CreateDbContext();
        var repository = new ExampleRepository(dbContext);
        var persistedEntity = await repository.GetByIdAsync(entity.Id, CancellationToken.None);

        Assert.NotNull(persistedEntity);
        Assert.Equal(entity.Id, persistedEntity.Id);
        Assert.Equal(entity.Name, persistedEntity.Name);
        Assert.Equal(ExampleStatus.Active, persistedEntity.Status);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsStoredEntity()
    {
        var entity = CreateEntity();
        await AddAsync(entity);

        await using var dbContext = fixture.CreateDbContext();
        var repository = new ExampleRepository(dbContext);
        var entities = await repository.GetAllAsync(CancellationToken.None);

        Assert.Contains(entities, persistedEntity => persistedEntity.Id == entity.Id);
    }

    [Fact]
    public async Task UpdateAsync_PersistsRenamedEntity()
    {
        var entity = CreateEntity();
        await AddAsync(entity);
        entity.Rename(ExampleName.Create("Renamed example"));

        await using (var updateContext = fixture.CreateDbContext())
        {
            var repository = new ExampleRepository(updateContext);
            await repository.UpdateAsync(entity, CancellationToken.None);
        }

        await using var queryContext = fixture.CreateDbContext();
        var queryRepository = new ExampleRepository(queryContext);
        var persistedEntity = await queryRepository.GetByIdAsync(entity.Id, CancellationToken.None);

        Assert.NotNull(persistedEntity);
        Assert.Equal("Renamed example", persistedEntity.Name.Value);
    }

    [Fact]
    public async Task DeleteAsync_RemovesEntity()
    {
        var entity = CreateEntity();
        await AddAsync(entity);

        await using (var deleteContext = fixture.CreateDbContext())
        {
            var repository = new ExampleRepository(deleteContext);
            await repository.DeleteAsync(entity, CancellationToken.None);
        }

        await using var queryContext = fixture.CreateDbContext();
        var queryRepository = new ExampleRepository(queryContext);
        var persistedEntity = await queryRepository.GetByIdAsync(entity.Id, CancellationToken.None);

        Assert.Null(persistedEntity);
    }

    [Fact]
    public async Task GetAllAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new ExampleRepository(dbContext);
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => repository.GetAllAsync(cancellationTokenSource.Token));
    }

    private async Task AddAsync(ExampleEntity entity)
    {
        await using var dbContext = fixture.CreateDbContext();
        var repository = new ExampleRepository(dbContext);
        await repository.AddAsync(entity, CancellationToken.None);
    }

    private static ExampleEntity CreateEntity()
    {
        return ExampleEntity.Create(Guid.NewGuid(), ExampleName.Create("Example name"));
    }
}
