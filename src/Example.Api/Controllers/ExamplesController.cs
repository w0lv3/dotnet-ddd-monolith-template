using Example.Api.Authentication;
using Example.Api.Models.Examples;
using Example.Application.Interfaces.Services;
using Example.Application.Models.Examples;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Example.Api.Controllers;

[ApiController]
[Route("api/examples")]
[ProducesResponseType(StatusCodes.Status401Unauthorized, Description = "Authentication is required.")]
[ProducesResponseType(StatusCodes.Status403Forbidden, Description = "The bearer token lacks the required permission.")]
public sealed class ExamplesController(IExampleService exampleService) : ControllerBase
{
    [HttpGet]
    [EndpointName("GetExamples")]
    [EndpointSummary("List examples")]
    [EndpointDescription("Returns all examples. Requires the examples.read permission.")]
    [Authorize(Policy = AuthorizationPolicies.ExamplesRead)]
    [ProducesResponseType<IReadOnlyCollection<ExampleResponse>>(StatusCodes.Status200OK, Description = "The examples were returned.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json", Description = "An unexpected error occurred.")]
    public async Task<ActionResult<IReadOnlyCollection<ExampleResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var examples = await exampleService.GetAllAsync(cancellationToken);

        return Ok(examples.Select(ToResponse).ToArray());
    }

    [HttpGet("{id:guid}")]
    [EndpointName("GetExampleById")]
    [EndpointSummary("Get an example")]
    [EndpointDescription("Returns one example by identifier. Requires the examples.read permission.")]
    [Authorize(Policy = AuthorizationPolicies.ExamplesRead)]
    [ProducesResponseType<ExampleResponse>(StatusCodes.Status200OK, Description = "The example was returned.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json", Description = "The identifier is invalid.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json", Description = "The example was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json", Description = "An unexpected error occurred.")]
    public async Task<ActionResult<ExampleResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var example = await exampleService.GetByIdAsync(id, cancellationToken);

        return Ok(ToResponse(example));
    }

    [HttpPost]
    [EndpointName("CreateExample")]
    [EndpointSummary("Create an example")]
    [EndpointDescription("Creates an example. Requires the examples.write permission.")]
    [Authorize(Policy = AuthorizationPolicies.ExamplesWrite)]
    [ProducesResponseType<ExampleResponse>(StatusCodes.Status201Created, Description = "The example was created.")]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json", Description = "The request failed validation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json", Description = "An example with the same name already exists.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json", Description = "An unexpected error occurred.")]
    public async Task<ActionResult<ExampleResponse>> Create(
        CreateExampleRequest request,
        CancellationToken cancellationToken)
    {
        var example = await exampleService.CreateAsync(
            new CreateExampleModel(request.Name),
            cancellationToken);
        var response = ToResponse(example);

        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [EndpointName("UpdateExample")]
    [EndpointSummary("Update an example")]
    [EndpointDescription("Updates an example by identifier. Requires the examples.write permission.")]
    [Authorize(Policy = AuthorizationPolicies.ExamplesWrite)]
    [ProducesResponseType<ExampleResponse>(StatusCodes.Status200OK, Description = "The example was updated.")]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json", Description = "The request failed validation.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json", Description = "The example was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json", Description = "An example with the same name already exists.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json", Description = "An unexpected error occurred.")]
    public async Task<ActionResult<ExampleResponse>> Update(
        Guid id,
        UpdateExampleRequest request,
        CancellationToken cancellationToken)
    {
        var example = await exampleService.UpdateAsync(
            new UpdateExampleModel(id, request.Name),
            cancellationToken);

        return Ok(ToResponse(example));
    }

    [HttpDelete("{id:guid}")]
    [EndpointName("DeleteExample")]
    [EndpointSummary("Delete an example")]
    [EndpointDescription("Deletes an example by identifier. Requires the examples.write permission.")]
    [Authorize(Policy = AuthorizationPolicies.ExamplesWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent, Description = "The example was deleted.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json", Description = "The identifier is invalid.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json", Description = "The example was not found.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json", Description = "An unexpected error occurred.")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await exampleService.DeleteAsync(id, cancellationToken);

        return NoContent();
    }

    private static ExampleResponse ToResponse(ExampleDto example)
    {
        return new ExampleResponse(example.Id, example.Name, example.Status);
    }
}
