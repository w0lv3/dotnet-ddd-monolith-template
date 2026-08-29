using System.Net;
using System.Net.Http.Json;
using Example.Api.Models.Examples;
using Example.Application.Models.Examples;
using NSubstitute;

namespace Example.Api.IntegrationTests;

public sealed class ExamplesControllerTests : IClassFixture<ExampleApiFactory>, IDisposable
{
    private readonly ExampleApiFactory factory;
    private readonly HttpClient client;

    public ExamplesControllerTests(ExampleApiFactory factory)
    {
        this.factory = factory;
        factory.ResetService();
        client = factory.CreateHttpsClient();
    }

    [Fact]
    public async Task GetAll_ReturnsSerializedExamples()
    {
        ExampleDto[] examples =
        [
            new(Guid.NewGuid(), "First", "Inactive"),
            new(Guid.NewGuid(), "Second", "Active")
        ];
        factory.ExampleService
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(examples);

        var response = await client.GetAsync("/api/examples", CancellationToken.None);
        var content = await response.Content.ReadFromJsonAsync<ExampleResponse[]>(
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(content);
        Assert.Equal(examples.Select(example => example.Id), content.Select(example => example.Id));
        await factory.ExampleService.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetById_ReturnsSerializedExample()
    {
        var example = new ExampleDto(Guid.NewGuid(), "Example name", "Inactive");
        factory.ExampleService
            .GetByIdAsync(example.Id, Arg.Any<CancellationToken>())
            .Returns(example);

        var response = await client.GetAsync(
            $"/api/examples/{example.Id}",
            CancellationToken.None);
        var content = await response.Content.ReadFromJsonAsync<ExampleResponse>(
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(example.Id, content?.Id);
        Assert.Equal(example.Name, content?.Name);
    }

    [Fact]
    public async Task Create_MapsRequestAndReturnsCreatedResponse()
    {
        var example = new ExampleDto(Guid.NewGuid(), "Example name", "Inactive");
        factory.ExampleService
            .CreateAsync(
                Arg.Is<CreateExampleModel>(model => model.Name == example.Name),
                Arg.Any<CancellationToken>())
            .Returns(example);

        var response = await client.PostAsJsonAsync(
            "/api/examples",
            new CreateExampleRequest(example.Name),
            CancellationToken.None);
        var content = await response.Content.ReadFromJsonAsync<ExampleResponse>(
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(example.Id, content?.Id);
        Assert.EndsWith($"/api/examples/{example.Id}", response.Headers.Location?.ToString());
        await factory.ExampleService.Received(1).CreateAsync(
            Arg.Is<CreateExampleModel>(model => model.Name == example.Name),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_UsesRouteIdentifierAndReturnsUpdatedResponse()
    {
        var id = Guid.NewGuid();
        var example = new ExampleDto(id, "Renamed example", "Inactive");
        factory.ExampleService
            .UpdateAsync(
                Arg.Is<UpdateExampleModel>(model => model.Id == id && model.Name == example.Name),
                Arg.Any<CancellationToken>())
            .Returns(example);

        var response = await client.PutAsJsonAsync(
            $"/api/examples/{id}",
            new UpdateExampleRequest(example.Name),
            CancellationToken.None);
        var content = await response.Content.ReadFromJsonAsync<ExampleResponse>(
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(example, new ExampleDto(content!.Id, content.Name, content.Status));
    }

    [Fact]
    public async Task Delete_ReturnsNoContent()
    {
        var id = Guid.NewGuid();

        var response = await client.DeleteAsync(
            $"/api/examples/{id}",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await factory.ExampleService.Received(1).DeleteAsync(id, Arg.Any<CancellationToken>());
    }

    public void Dispose()
    {
        client.Dispose();
    }
}
