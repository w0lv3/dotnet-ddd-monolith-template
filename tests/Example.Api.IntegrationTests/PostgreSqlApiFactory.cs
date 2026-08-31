using System.Security.Claims;
using System.Text.Encodings.Web;
using Example.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace Example.Api.IntegrationTests;

public sealed class PostgreSqlApiFactory : WebApplicationFactory<Program>
{
    private const string TestAuthenticationScheme = "Test";
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    public async Task StartAsync()
    {
        await container.StartAsync();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    public async Task StopAsync()
    {
        Dispose();
        await container.DisposeAsync();
    }

    public HttpClient CreateHttpsClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var connectionString = container.GetConnectionString();
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Database", connectionString);
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = connectionString,
                ["Authentication:Provider"] = "Keycloak",
                ["Authentication:Keycloak:Authority"] = "https://issuer.example",
                ["Authentication:Keycloak:Audience"] = "example-api",
                ["Authentication:Keycloak:RequireHttpsMetadata"] = "true"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthenticationScheme;
                    options.DefaultChallengeScheme = TestAuthenticationScheme;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationScheme,
                    _ => { });
        });
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Claim[] claims =
            [
                new("sub", "integration-test-user"),
                new("email", "integration-test@example.com"),
                new("scope", "examples.read examples.write")
            ];
            var identity = new ClaimsIdentity(claims, TestAuthenticationScheme);
            var ticket = new AuthenticationTicket(
                new ClaimsPrincipal(identity),
                TestAuthenticationScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}

public sealed class PostgreSqlApiFixture : IAsyncLifetime
{
    public PostgreSqlApiFactory Factory { get; } = new();

    public Task InitializeAsync()
    {
        return Factory.StartAsync();
    }

    public Task DisposeAsync()
    {
        return Factory.StopAsync();
    }
}
