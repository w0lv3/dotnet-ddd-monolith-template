using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Example.Application.Models.Examples;
using NSubstitute;

namespace Example.Api.IntegrationTests;

public sealed class EntraIdAuthenticationTests : IDisposable
{
    private const string Audience = "api://11111111-1111-1111-1111-111111111111";
    private const string ClientId = "11111111-1111-1111-1111-111111111111";
    private const string Issuer =
        "https://login.microsoftonline.com/22222222-2222-2222-2222-222222222222/v2.0";
    private readonly ProviderApiFactory factory;
    private readonly HttpClient client;

    public EntraIdAuthenticationTests()
    {
        factory = new ProviderApiFactory(
            Issuer,
            Audience,
            new Dictionary<string, string?>
            {
                ["Authentication:Provider"] = "EntraId",
                ["Authentication:EntraId:Instance"] = "https://login.microsoftonline.com/",
                ["Authentication:EntraId:TenantId"] = "22222222-2222-2222-2222-222222222222",
                ["Authentication:EntraId:ClientId"] = ClientId,
                ["Authentication:EntraId:Audience"] = Audience,
                ["Authentication:EntraId:AllowWebApiToBeAuthorizedByACL"] = "true"
            });
        client = factory.CreateHttpsClient();
        factory.ExampleService
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ExampleDto>());
    }

    [Theory]
    [InlineData("scp")]
    [InlineData("roles")]
    public async Task DelegatedScopeOrApplicationRole_ReturnsSuccess(string permissionClaim)
    {
        SetToken(new Claim(permissionClaim, "examples.read"));

        var response = await client.GetAsync("/api/examples", CancellationToken.None);

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task MissingPermission_ReturnsForbidden()
    {
        SetToken(new Claim("oid", "33333333-3333-3333-3333-333333333333"));

        var response = await client.GetAsync("/api/examples", CancellationToken.None);

        Assert.True(
            response.StatusCode == HttpStatusCode.Forbidden,
            response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task WrongAudience_ReturnsUnauthorized()
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateToken(
                [new Claim("scp", "examples.read")],
                "api://wrong"));

        var response = await client.GetAsync("/api/examples", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private void SetToken(params Claim[] claims)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateToken(claims));
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();
    }
}
