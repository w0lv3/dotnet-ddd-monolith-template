using System.Net;
using System.Text.Json;

namespace Example.Api.IntegrationTests;

public sealed class OpenApiTests(ExampleApiFactory factory) : IClassFixture<ExampleApiFactory>
{
    [Fact]
    public async Task JsonDocument_ContainsStableApiMetadataAndOperations()
    {
        using var client = factory.CreateAnonymousHttpsClient();
        using var response = await client.GetAsync("/openapi/v1.json", CancellationToken.None);
        using var document = await ReadJsonAsync(response);
        var root = document.RootElement;
        var info = root.GetProperty("info");
        var paths = root.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("3.1", root.GetProperty("openapi").GetString());
        Assert.Equal("Example.Api API", info.GetProperty("title").GetString());
        Assert.Equal("v1", info.GetProperty("version").GetString());
        Assert.Contains("scope, scp, or roles", info.GetProperty("description").GetString());
        Assert.Equal("/", root.GetProperty("servers")[0].GetProperty("url").GetString());

        AssertOperation(paths, "/api/examples", "get", "GetExamples", "List examples");
        AssertOperation(paths, "/api/examples", "post", "CreateExample", "Create an example");
        AssertOperation(paths, "/api/examples/{id}", "get", "GetExampleById", "Get an example");
        AssertOperation(paths, "/api/examples/{id}", "put", "UpdateExample", "Update an example");
        AssertOperation(paths, "/api/examples/{id}", "delete", "DeleteExample", "Delete an example");
    }

    [Fact]
    public async Task ProtectedOperations_DescribeBearerAuthenticationAndPermissions()
    {
        using var client = factory.CreateAnonymousHttpsClient();
        using var response = await client.GetAsync("/openapi/v1.json", CancellationToken.None);
        using var document = await ReadJsonAsync(response);
        var root = document.RootElement;
        var scheme = root.GetProperty("components")
            .GetProperty("securitySchemes")
            .GetProperty("Bearer");
        var paths = root.GetProperty("paths");

        Assert.Equal("http", scheme.GetProperty("type").GetString());
        Assert.Equal("bearer", scheme.GetProperty("scheme").GetString());
        Assert.Equal("JWT", scheme.GetProperty("bearerFormat").GetString());

        AssertSecurity(paths, "/api/examples", "get", "examples.read");
        AssertSecurity(paths, "/api/examples/{id}", "get", "examples.read");
        AssertSecurity(paths, "/api/examples", "post", "examples.write");
        AssertSecurity(paths, "/api/examples/{id}", "put", "examples.write");
        AssertSecurity(paths, "/api/examples/{id}", "delete", "examples.write");
    }

    [Fact]
    public async Task Responses_DescribeProblemDetailsAndBodylessAuthenticationFailures()
    {
        using var client = factory.CreateAnonymousHttpsClient();
        using var response = await client.GetAsync("/openapi/v1.json", CancellationToken.None);
        using var document = await ReadJsonAsync(response);
        var paths = document.RootElement.GetProperty("paths");
        var createResponses = paths.GetProperty("/api/examples")
            .GetProperty("post")
            .GetProperty("responses");
        var deleteResponses = paths.GetProperty("/api/examples/{id}")
            .GetProperty("delete")
            .GetProperty("responses");

        Assert.False(createResponses.GetProperty("401").TryGetProperty("content", out _));
        Assert.False(createResponses.GetProperty("403").TryGetProperty("content", out _));

        var validationSchema = createResponses.GetProperty("400")
            .GetProperty("content")
            .GetProperty("application/problem+json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();
        var conflictSchema = createResponses.GetProperty("409")
            .GetProperty("content")
            .GetProperty("application/problem+json")
            .GetProperty("schema")
            .GetProperty("$ref")
            .GetString();

        Assert.EndsWith("/HttpValidationProblemDetails", validationSchema);
        Assert.EndsWith("/ProblemDetails", conflictSchema);
        Assert.False(deleteResponses.GetProperty("204").TryGetProperty("content", out _));
    }

    [Fact]
    public async Task YamlDocument_IsAvailableInDevelopment()
    {
        using var client = factory.CreateAnonymousHttpsClient();
        using var response = await client.GetAsync("/openapi/v1.yaml", CancellationToken.None);
        var content = await response.Content.ReadAsStringAsync(CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("yaml", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("openapi: '3.1", content);
        Assert.Contains("/api/examples:", content);
        Assert.Contains("Bearer:", content);
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var content = await response.Content.ReadAsStreamAsync(CancellationToken.None);
        return await JsonDocument.ParseAsync(content, cancellationToken: CancellationToken.None);
    }

    private static void AssertOperation(
        JsonElement paths,
        string path,
        string method,
        string operationId,
        string summary)
    {
        var operation = paths.GetProperty(path).GetProperty(method);

        Assert.Equal(operationId, operation.GetProperty("operationId").GetString());
        Assert.Equal(summary, operation.GetProperty("summary").GetString());
        Assert.False(string.IsNullOrWhiteSpace(operation.GetProperty("description").GetString()));
    }

    private static void AssertSecurity(
        JsonElement paths,
        string path,
        string method,
        string permission)
    {
        var operation = paths.GetProperty(path).GetProperty(method);
        var security = operation.GetProperty("security")[0];
        var permissions = operation.GetProperty("x-required-permissions")
            .EnumerateArray()
            .Select(item => item.GetString()!)
            .ToArray();

        Assert.Equal(0, security.GetProperty("Bearer").GetArrayLength());
        Assert.Equal([permission], permissions);
    }
}
