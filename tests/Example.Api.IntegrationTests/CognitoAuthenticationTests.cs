using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Example.Application.Models.Examples;
using NSubstitute;

namespace Example.Api.IntegrationTests;

public sealed class CognitoAuthenticationTests : IDisposable
{
    private const string Audience = "https://api.example.com";
    private const string ClientId = "cognito-client";
    private const string Issuer = "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_pool";
    private readonly ProviderApiFactory factory;
    private readonly HttpClient client;

    public CognitoAuthenticationTests()
    {
        factory = new ProviderApiFactory(
            Issuer,
            Audience,
            new Dictionary<string, string?>
            {
                ["Authentication:Provider"] = "Cognito",
                ["Authentication:Cognito:Region"] = "us-east-1",
                ["Authentication:Cognito:UserPoolId"] = "us-east-1_pool",
                ["Authentication:Cognito:ClientId"] = ClientId,
                ["Authentication:Cognito:Authority"] = Issuer,
                ["Authentication:Cognito:Audience"] = Audience,
                ["Authentication:Cognito:ResourceServerIdentifier"] = "example-api"
            });
        client = factory.CreateHttpsClient();
        factory.ExampleService
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ExampleDto>());
    }

    [Fact]
    public async Task QualifiedCognitoScope_IsNormalizedToCommonPermission()
    {
        Assert.Equal("Cognito", factory.GetConfigurationValue("Authentication:Provider"));
        Assert.Equal(Audience, factory.GetConfigurationValue("Authentication:Cognito:Audience"));
        Assert.Equal(Audience, factory.GetBearerOptions().TokenValidationParameters.ValidAudience);
        SetToken(
            new Claim("token_use", "access"),
            new Claim("client_id", ClientId),
            new Claim("scope", "openid example-api/examples.read"));

        var response = await client.GetAsync("/api/examples", CancellationToken.None);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"{response.StatusCode}: {response.Headers.WwwAuthenticate}");
    }

    [Theory]
    [InlineData("id", ClientId)]
    [InlineData("access", "wrong-client")]
    public async Task InvalidTokenUseOrClientId_ReturnsUnauthorized(
        string tokenUse,
        string clientId)
    {
        SetToken(
            new Claim("token_use", tokenUse),
            new Claim("client_id", clientId),
            new Claim("scope", "example-api/examples.read"));

        var response = await client.GetAsync("/api/examples", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WrongAudience_ReturnsUnauthorized()
    {
        SetToken(
            "https://wrong.example.com",
            new Claim("token_use", "access"),
            new Claim("client_id", ClientId),
            new Claim("scope", "example-api/examples.read"));

        var response = await client.GetAsync("/api/examples", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private void SetToken(params Claim[] claims)
    {
        SetToken(Audience, claims);
    }

    private void SetToken(string tokenAudience, params Claim[] claims)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateToken(claims, tokenAudience));
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }
}
