namespace Example.Infrastructure.IntegrationTests;

public abstract class PostgreSqlIntegrationTest(PostgreSqlFixture fixture) : IAsyncLifetime
{
    protected PostgreSqlFixture Fixture { get; } = fixture;

    public Task InitializeAsync() => Fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
