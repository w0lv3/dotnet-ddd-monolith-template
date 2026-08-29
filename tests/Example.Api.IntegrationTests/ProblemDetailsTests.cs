using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Example.Api.Models.Examples;
using Example.Application.Exceptions;
using Example.Application.Models.Examples;
using Example.Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;

namespace Example.Api.IntegrationTests;

public sealed class ProblemDetailsTests : IClassFixture<ExampleApiFactory>, IDisposable
{
    private readonly ExampleApiFactory factory;
    private readonly HttpClient client;

    public ProblemDetailsTests(ExampleApiFactory factory)
    {
        this.factory = factory;
        factory.ResetService();
        client = factory.CreateHttpsClient();
    }

    [Fact]
    public async Task ValidationException_ReturnsValidationProblemDetails()
    {
        var exception = new ValidationException(
            [new ValidationFailure(nameof(CreateExampleModel.Name), "Name is required.")]);
        factory.ExampleService
            .CreateAsync(Arg.Any<CreateExampleModel>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ExampleDto>(exception));

        var response = await client.PostAsJsonAsync(
            "/api/examples",
            new CreateExampleRequest(""),
            CancellationToken.None);
        using var document = await ReadProblemAsync(response);

        AssertProblem(response, document.RootElement, HttpStatusCode.BadRequest);
        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty("Name", out _));
    }

    [Fact]
    public async Task NotFoundException_ReturnsNotFoundProblemDetails()
    {
        var id = Guid.NewGuid();
        factory.ExampleService
            .GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ExampleDto>(new ExampleNotFoundException(id)));

        var response = await client.GetAsync(
            $"/api/examples/{id}",
            CancellationToken.None);
        using var document = await ReadProblemAsync(response);

        AssertProblem(response, document.RootElement, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DomainException_ReturnsConflictProblemDetails()
    {
        factory.ExampleService
            .CreateAsync(Arg.Any<CreateExampleModel>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ExampleDto>(new DomainException("Domain conflict.")));

        var response = await client.PostAsJsonAsync(
            "/api/examples",
            new CreateExampleRequest("Example name"),
            CancellationToken.None);
        using var document = await ReadProblemAsync(response);

        AssertProblem(response, document.RootElement, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ArgumentException_ReturnsBadRequestProblemDetails()
    {
        var id = Guid.NewGuid();
        factory.ExampleService
            .GetByIdAsync(id, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ExampleDto>(new ArgumentException("Invalid identifier.")));

        var response = await client.GetAsync(
            $"/api/examples/{id}",
            CancellationToken.None);
        using var document = await ReadProblemAsync(response);

        AssertProblem(response, document.RootElement, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AnonymousRequest_IsRejectedBeforeCallingApplicationService()
    {
        using var anonymousClient = factory.CreateAnonymousHttpsClient();

        var response = await anonymousClient.GetAsync("/api/examples", CancellationToken.None);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.Select(header => header.Scheme));
        await factory.ExampleService.DidNotReceiveWithAnyArgs().GetAllAsync(default);
    }

    [Fact]
    public async Task UnauthorizedException_ForAuthenticatedUser_ReturnsForbiddenProblemDetails()
    {
        ConfigureUnauthorizedException();

        var response = await client.GetAsync("/api/examples", CancellationToken.None);
        using var document = await ReadProblemAsync(response);

        AssertProblem(response, document.RootElement, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UnexpectedException_ReturnsGenericInternalServerError()
    {
        factory.ExampleService
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyCollection<ExampleDto>>(
                new InvalidOperationException("Sensitive internal detail.")));

        var response = await client.GetAsync("/api/examples", CancellationToken.None);
        using var document = await ReadProblemAsync(response);

        AssertProblem(response, document.RootElement, HttpStatusCode.InternalServerError);
        Assert.Equal("An unexpected error occurred.", document.RootElement.GetProperty("detail").GetString());
        Assert.DoesNotContain("Sensitive", document.RootElement.GetRawText());
    }

    [Fact]
    public async Task MalformedJson_ReturnsBadRequestProblemDetails()
    {
        using var content = new StringContent("{", Encoding.UTF8, "application/json");

        var response = await client.PostAsync(
            "/api/examples",
            content,
            CancellationToken.None);
        using var document = await ReadProblemAsync(response);

        AssertProblem(response, document.RootElement, HttpStatusCode.BadRequest);
        await factory.ExampleService.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    private void ConfigureUnauthorizedException()
    {
        factory.ExampleService
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyCollection<ExampleDto>>(
                new UnauthorizedAccessException("Access denied.")));
    }

    private static async Task<JsonDocument> ReadProblemAsync(HttpResponseMessage response)
    {
        var stream = await response.Content.ReadAsStreamAsync(CancellationToken.None);
        return await JsonDocument.ParseAsync(
            stream,
            cancellationToken: CancellationToken.None);
    }

    private static void AssertProblem(
        HttpResponseMessage response,
        JsonElement problem,
        HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("type").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("instance").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }

    public void Dispose()
    {
        client.Dispose();
    }
}
