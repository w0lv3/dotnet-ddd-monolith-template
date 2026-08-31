using System.Net;
using System.Net.Http.Json;
using Example.Api.Models.Examples;
using Microsoft.AspNetCore.Mvc;

namespace Example.Api.IntegrationTests;

public sealed class ExamplesCrudTests : IClassFixture<PostgreSqlApiFixture>, IDisposable
{
    private readonly HttpClient client;

    public ExamplesCrudTests(PostgreSqlApiFixture fixture)
    {
        client = fixture.Factory.CreateHttpsClient();
    }

    [Fact]
    public async Task CrudFlow_PersistsThroughRealApplicationAndPostgreSql()
    {
        var createResponse = await client.PostAsJsonAsync(
            "/api/examples",
            new CreateExampleRequest("Original example"),
            CancellationToken.None);
        var created = await createResponse.Content.ReadFromJsonAsync<ExampleResponse>(
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Original example", created.Name);
        Assert.Equal("Inactive", created.Status);
        Assert.EndsWith($"/api/examples/{created.Id}", createResponse.Headers.Location?.ToString());

        var getResponse = await client.GetAsync(
            $"/api/examples/{created.Id}",
            CancellationToken.None);
        var fetched = await getResponse.Content.ReadFromJsonAsync<ExampleResponse>(
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(created, fetched);

        var listResponse = await client.GetAsync("/api/examples", CancellationToken.None);
        var listed = await listResponse.Content.ReadFromJsonAsync<ExampleResponse[]>(
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.NotNull(listed);
        Assert.Equal([created], listed);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/examples/{created.Id}",
            new UpdateExampleRequest("Updated example"),
            CancellationToken.None);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ExampleResponse>(
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Updated example", updated.Name);
        Assert.Equal("Inactive", updated.Status);

        var deleteResponse = await client.DeleteAsync(
            $"/api/examples/{created.Id}",
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var missingResponse = await client.GetAsync(
            $"/api/examples/{created.Id}",
            CancellationToken.None);
        var problem = await missingResponse.Content.ReadFromJsonAsync<ProblemDetails>(
            CancellationToken.None);

        Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
        Assert.Equal("application/problem+json", missingResponse.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(problem);
        Assert.Equal((int)HttpStatusCode.NotFound, problem.Status);
        Assert.Equal($"/api/examples/{created.Id}", problem.Instance);
    }

    public void Dispose()
    {
        client.Dispose();
    }
}
