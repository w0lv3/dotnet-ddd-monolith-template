using Example.Domain.Entities;
using Example.Domain.ValueObjects;
using Example.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Example.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class TransactionTests(PostgreSqlFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    [Fact]
    public async Task CommittedTransaction_PersistsRepositoryChanges()
    {
        var entity = CreateEntity();
        await using (var dbContext = Fixture.CreateDbContext())
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            await new ExampleRepository(dbContext).AddAsync(entity, CancellationToken.None);
            await transaction.CommitAsync();
        }

        await using var queryContext = Fixture.CreateDbContext();
        Assert.NotNull(await new ExampleRepository(queryContext)
            .GetByIdAsync(entity.Id, CancellationToken.None));
    }

    [Fact]
    public async Task RolledBackTransaction_DiscardsRepositoryChanges()
    {
        var entity = CreateEntity();
        await using (var dbContext = Fixture.CreateDbContext())
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync();
            await new ExampleRepository(dbContext).AddAsync(entity, CancellationToken.None);
            await transaction.RollbackAsync();
        }

        await using var queryContext = Fixture.CreateDbContext();
        Assert.Null(await new ExampleRepository(queryContext)
            .GetByIdAsync(entity.Id, CancellationToken.None));
    }

    private static ExampleEntity CreateEntity() =>
        ExampleEntity.Create(Guid.NewGuid(), ExampleName.Create("Transactional example"));
}
