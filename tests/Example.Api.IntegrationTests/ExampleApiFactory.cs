using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Example.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;
using NSubstitute.ClearExtensions;

namespace Example.Api.IntegrationTests;

public sealed class ExampleApiFactory : WebApplicationFactory<Program>
{
    private const string Audience = "example-api";
    private const string Issuer = "https://issuer.example";
    private static readonly SymmetricSecurityKey SigningKey = new(RandomNumberGenerator.GetBytes(32));

    public IExampleService ExampleService { get; } = Substitute.For<IExampleService>();

    public void ResetService()
    {
        ExampleService.ClearSubstitute();
    }

    public HttpClient CreateHttpsClient()
    {
        var client = CreateAnonymousHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateToken("scope", "examples.read", "examples.write"));
        return client;
    }

    public HttpClient CreateAnonymousHttpsClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    public string CreateToken(string scopeClaimType = "scope", params string[] scopes)
    {
        List<Claim> claims =
        [
            new("sub", "test-user"),
            new("email", "test@example.com")
        ];

        if (scopes.Length > 0)
        {
            claims.Add(new Claim(scopeClaimType, string.Join(' ', scopes)));
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Audience = Audience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = Issuer,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            SigningCredentials = new SigningCredentials(SigningKey, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(claims)
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] =
                    "Host=localhost;Port=1;Database=test;Username=test;Password=test",
                ["Authentication:Provider"] = "Keycloak",
                ["Authentication:Keycloak:Authority"] = Issuer,
                ["Authentication:Keycloak:Audience"] = Audience,
                ["Authentication:Keycloak:RequireHttpsMetadata"] = "true"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IExampleService>();
            services.AddSingleton(ExampleService);
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    var configuration = new OpenIdConnectConfiguration
                    {
                        Issuer = Issuer
                    };
                    configuration.SigningKeys.Add(SigningKey);
                    options.ConfigurationManager =
                        new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                });
        });
    }
}
