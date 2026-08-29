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

public sealed class ProviderApiFactory(
    string issuer,
    string audience,
    IReadOnlyDictionary<string, string?> authenticationConfiguration)
    : WebApplicationFactory<Program>
{
    private readonly SymmetricSecurityKey signingKey = new(RandomNumberGenerator.GetBytes(32));

    public IExampleService ExampleService { get; } = Substitute.For<IExampleService>();

    public HttpClient CreateHttpsClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    public JwtBearerOptions GetBearerOptions()
    {
        return Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
    }

    public string? GetConfigurationValue(string key)
    {
        return Services.GetRequiredService<IConfiguration>()[key];
    }

    public string CreateToken(
        IEnumerable<Claim> claims,
        string? tokenAudience = null,
        string? tokenIssuer = null)
    {
        var expectedAudience = tokenAudience ?? audience;
        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(new SecurityTokenDescriptor
        {
            Audience = expectedAudience,
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = tokenIssuer ?? issuer,
            NotBefore = DateTime.UtcNow.AddMinutes(-1),
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(claims)
        });

        if (!handler.ReadJsonWebToken(token).Audiences.Contains(expectedAudience, StringComparer.Ordinal))
        {
            throw new InvalidOperationException("The generated test token is missing its audience.");
        }

        return token;
    }

    public void ResetService()
    {
        ExampleService.ClearSubstitute();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        foreach (var (key, value) in authenticationConfiguration)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            Dictionary<string, string?> values = new(authenticationConfiguration)
            {
                ["ConnectionStrings:Database"] =
                    "Host=localhost;Port=1;Database=test;Username=test;Password=test"
            };
            configuration.AddInMemoryCollection(values);
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
                        Issuer = issuer
                    };
                    configuration.SigningKeys.Add(signingKey);
                    options.ConfigurationManager =
                        new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                    options.TokenValidationParameters.ValidIssuer = issuer;
                    options.TokenValidationParameters.IssuerValidator = null;
                });
        });
    }
}
