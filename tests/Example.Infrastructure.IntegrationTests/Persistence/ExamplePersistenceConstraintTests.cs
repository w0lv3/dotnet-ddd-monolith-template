using System.Data.Common;
using Example.Domain.Entities;
using Example.Domain.Enums;
using Example.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Example.Infrastructure.IntegrationTests.Persistence;

[Collection(PostgreSqlCollection.Name)]
public sealed class ExamplePersistenceConstraintTests(PostgreSqlFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    [Fact]
    public async Task Schema_HasExpectedTableColumnsAndPrimaryKey()
    {
        await using var dbContext = Fixture.CreateDbContext();
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();

        await using var columnCommand = connection.CreateCommand();
        columnCommand.CommandText = """
            SELECT "column_name", "data_type", "is_nullable", "character_maximum_length"
            FROM information_schema.columns
            WHERE "table_schema" = 'public' AND "table_name" = 'Examples'
            ORDER BY "ordinal_position"
            """;

        await using var reader = await columnCommand.ExecuteReaderAsync();
        var columns = new List<(string Name, string Type, string Nullable, int? MaximumLength)>();
        while (await reader.ReadAsync())
        {
            columns.Add((
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetInt32(3)));
        }

        Assert.Equal(
            [
                ("Id", "uuid", "NO", (int?)null),
                ("Name", "character varying", "NO", (int?)ExampleName.MaximumLength),
                ("Status", "character varying", "NO", (int?)16)
            ],
            columns);

        await reader.DisposeAsync();
        await using var keyCommand = connection.CreateCommand();
        keyCommand.CommandText = """
            SELECT constraint_name
            FROM information_schema.table_constraints
            WHERE table_schema = 'public'
              AND table_name = 'Examples'
              AND constraint_type = 'PRIMARY KEY'
            """;

        Assert.Equal("PK_Examples", await keyCommand.ExecuteScalarAsync());
    }

    [Fact]
    public async Task RequiredName_IsEnforcedByPostgreSql()
    {
        await using var dbContext = Fixture.CreateDbContext();

        await Assert.ThrowsAnyAsync<DbException>(() => dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"Examples\" (\"Id\", \"Name\", \"Status\") VALUES ({Guid.NewGuid()}, {(string?)null}, {"Inactive"})"));
    }

    [Fact]
    public async Task NameMaximumLength_IsEnforcedByPostgreSql()
    {
        await using var dbContext = Fixture.CreateDbContext();

        await Assert.ThrowsAnyAsync<DbException>(() => dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO \"Examples\" (\"Id\", \"Name\", \"Status\") VALUES ({Guid.NewGuid()}, {new string('x', ExampleName.MaximumLength + 1)}, {"Inactive"})"));
    }

    [Fact]
    public async Task DuplicateIdentifier_IsRejected()
    {
        var id = Guid.NewGuid();
        await using var dbContext = Fixture.CreateDbContext();
        dbContext.Examples.Add(ExampleEntity.Create(id, ExampleName.Create("First")));
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear();
        dbContext.Examples.Add(ExampleEntity.Create(id, ExampleName.Create("Second")));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => dbContext.SaveChangesAsync(CancellationToken.None));
    }

    [Fact]
    public async Task StatusConversion_StoresEnumName()
    {
        var entity = ExampleEntity.Create(Guid.NewGuid(), ExampleName.Create("Status example"));
        entity.Activate();

        await using var dbContext = Fixture.CreateDbContext();
        dbContext.Examples.Add(entity);
        await dbContext.SaveChangesAsync();

        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"Status\" FROM \"Examples\" WHERE \"Id\" = @id";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = entity.Id;
        command.Parameters.Add(parameter);

        Assert.Equal(nameof(ExampleStatus.Active), await command.ExecuteScalarAsync());
    }
}
