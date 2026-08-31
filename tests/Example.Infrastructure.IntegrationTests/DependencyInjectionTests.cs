using Example.Application.Interfaces.Repositories;
using Example.Infrastructure.Persistence;
using Example.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Example.Infrastructure.IntegrationTests;

[Collection(PostgreSqlCollection.Name)]
public sealed class DependencyInjectionTests(PostgreSqlFixture fixture)
    : PostgreSqlIntegrationTest(fixture)
{
    [Fact]
    public void AddInfrastructure_RegistersNpgsqlDbContextAndScopedRepository()
    {
        var services = new ServiceCollection();
        services.AddInfrastructure(new TestConfiguration(Fixture.ConnectionString));

        using var provider = services.BuildServiceProvider();
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();
        var firstContext = firstScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var sameContext = firstScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var secondContext = secondScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var repository = firstScope.ServiceProvider.GetRequiredService<IExampleRepository>();

        Assert.Same(firstContext, sameContext);
        Assert.NotSame(firstContext, secondContext);
        Assert.IsType<ExampleRepository>(repository);
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", firstContext.Database.ProviderName);
        Assert.Equal(Fixture.ConnectionString, firstContext.Database.GetConnectionString());
    }

    [Fact]
    public void AddInfrastructure_WithoutDatabaseConnectionString_Throws()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddInfrastructure(new TestConfiguration(null)));

        Assert.Equal(
            "Connection string 'ConnectionStrings:Database' is required.",
            exception.Message);
    }

    private sealed class TestConfiguration(string? connectionString) : IConfiguration
    {
        public string? this[string key]
        {
            get => key == "ConnectionStrings:Database" ? connectionString : null;
            set => throw new NotSupportedException();
        }

        public IEnumerable<IConfigurationSection> GetChildren() => [];

        public IChangeToken GetReloadToken() => new CancellationChangeToken(CancellationToken.None);

        public IConfigurationSection GetSection(string key) =>
            new TestConfigurationSection(key, key == "ConnectionStrings" ? connectionString : null);
    }

    private sealed class TestConfigurationSection(string key, string? connectionString)
        : IConfigurationSection
    {
        public string? this[string childKey]
        {
            get => key == "ConnectionStrings" && childKey == "Database"
                ? connectionString
                : null;
            set => throw new NotSupportedException();
        }

        public string Key => key;

        public string Path => key;

        public string? Value { get; set; }

        public IEnumerable<IConfigurationSection> GetChildren() => [];

        public IChangeToken GetReloadToken() => new CancellationChangeToken(CancellationToken.None);

        public IConfigurationSection GetSection(string childKey) =>
            new TestConfigurationSection($"{Path}:{childKey}", null);
    }
}
