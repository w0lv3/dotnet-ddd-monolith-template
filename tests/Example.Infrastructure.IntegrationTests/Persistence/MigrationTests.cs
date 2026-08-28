using Microsoft.EntityFrameworkCore;

namespace Example.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class MigrationTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task InitialMigration_IsApplied()
    {
        await using var dbContext = fixture.CreateDbContext();

        var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

        Assert.Contains(appliedMigrations, migration => migration.EndsWith("_InitialCreate"));
        Assert.Empty(pendingMigrations);
    }
}
