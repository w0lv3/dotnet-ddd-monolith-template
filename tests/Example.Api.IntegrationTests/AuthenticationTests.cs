using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Example.Api.Models.Examples;
using Example.Application.Models.Examples;
using NSubstitute;

namespace Example.Api.IntegrationTests;

public sealed class AuthenticationTests : IClassFixture<ExampleApiFactory>
{
    private readonly ExampleApiFactory factory;

    public AuthenticationTests(ExampleApiFactory factory)
    {
        this.factory = factory;
        factory.ResetService();
    }

    [Fact]
    public async Task NoToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateAnonymousHttpsClient();

        var response = await client.GetAsync("/api/examples", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await factory.ExampleService.DidNotReceiveWithAnyArgs().GetAllAsync(default);
    }

    [Fact]
    public async Task InvalidToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateAnonymousHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-jwt");

        var response = await client.GetAsync("/api/examples", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        await factory.ExampleService.DidNotReceiveWithAnyArgs().GetAllAsync(default);
    }

    [Fact]
    public async Task MissingScope_ReturnsForbidden()
    {
        using var client = factory.CreateAnonymousHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateToken());

        var response = await client.GetAsync("/api/examples", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await factory.ExampleService.DidNotReceiveWithAnyArgs().GetAllAsync(default);
    }

    [Theory]
    [InlineData("scope")]
    [InlineData("scp")]
    public async Task RequiredScope_ReturnsSuccess(string scopeClaimType)
    {
        factory.ExampleService
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ExampleDto>());
        using var client = factory.CreateAnonymousHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateToken(scopeClaimType, "examples.read"));

        var response = await client.GetAsync("/api/examples", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await factory.ExampleService.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReadScope_CannotAccessWriteEndpoint()
    {
        using var client = factory.CreateAnonymousHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateToken("scope", "examples.read"));

        var response = await client.PostAsJsonAsync(
            "/api/examples",
            new CreateExampleRequest("Example name"),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await factory.ExampleService.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    [Fact]
    public async Task WriteScope_CanAccessWriteEndpoint()
    {
        var example = new ExampleDto(Guid.NewGuid(), "Example name", "Inactive");
        factory.ExampleService
            .CreateAsync(Arg.Any<CreateExampleModel>(), Arg.Any<CancellationToken>())
            .Returns(example);
        using var client = factory.CreateAnonymousHttpsClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateToken("scope", "examples.write"));

        var response = await client.PostAsJsonAsync(
            "/api/examples",
            new CreateExampleRequest(example.Name),
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await factory.ExampleService.Received(1).CreateAsync(
            Arg.Is<CreateExampleModel>(model => model.Name == example.Name),
            Arg.Any<CancellationToken>());
    }
}
