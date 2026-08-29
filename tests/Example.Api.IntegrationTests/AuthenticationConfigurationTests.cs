using Example.Api.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Example.Api.IntegrationTests;

public sealed class AuthenticationConfigurationTests
{
    [Fact]
    public void SelectedProvider_DoesNotRequireOtherProviderSettings()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Authentication:Provider"] = "Keycloak",
            ["Authentication:Keycloak:Authority"] = "https://issuer.example",
            ["Authentication:Keycloak:Audience"] = "example-api"
        });
        var services = new ServiceCollection();

        var result = services.AddApiAuthentication(configuration, false);

        Assert.Same(services, result);
    }

    [Fact]
    public void KeycloakHttpMetadata_OutsideDevelopmentIsRejected()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Authentication:Provider"] = "Keycloak",
            ["Authentication:Keycloak:Authority"] = "http://issuer.example",
            ["Authentication:Keycloak:Audience"] = "example-api",
            ["Authentication:Keycloak:RequireHttpsMetadata"] = "false"
        });
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddApiAuthentication(configuration, false));

        Assert.Contains("Development", exception.Message);
    }

    [Fact]
    public void LocalStackCognito_OutsideDevelopmentIsRejected()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Authentication:Provider"] = "Cognito",
            ["Authentication:Cognito:Region"] = "us-east-1",
            ["Authentication:Cognito:UserPoolId"] = "us-east-1_pool",
            ["Authentication:Cognito:ClientId"] = "client",
            ["Authentication:Cognito:Authority"] = "http://localhost:4566/us-east-1_pool",
            ["Authentication:Cognito:JwksUri"] =
                "http://localhost:4566/us-east-1_pool/.well-known/jwks.json",
            ["Authentication:Cognito:RequireHttpsMetadata"] = "false",
            ["Authentication:Cognito:UseLocalStack"] = "true"
        });
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddApiAuthentication(configuration, false));

        Assert.Contains("Development", exception.Message);
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
