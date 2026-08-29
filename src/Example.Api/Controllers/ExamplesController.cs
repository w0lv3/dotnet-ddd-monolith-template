using Example.Api.Authentication;
using Example.Api.Models.Examples;
using Example.Application.Interfaces.Services;
using Example.Application.Models.Examples;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Example.Api.Controllers;

[ApiController]
[Route("api/examples")]
public sealed class ExamplesController(IExampleService exampleService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ExamplesRead)]
    [ProducesResponseType<IReadOnlyCollection<ExampleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyCollection<ExampleResponse>>> GetAll(
        CancellationToken cancellationToken)
    {
        var examples = await exampleService.GetAllAsync(cancellationToken);

        return Ok(examples.Select(ToResponse).ToArray());
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ExamplesRead)]
    [ProducesResponseType<ExampleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ExampleResponse>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var example = await exampleService.GetByIdAsync(id, cancellationToken);

        return Ok(ToResponse(example));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ExamplesWrite)]
    [ProducesResponseType<ExampleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
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
    [Authorize(Policy = AuthorizationPolicies.ExamplesWrite)]
    [ProducesResponseType<ExampleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<HttpValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
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
    [Authorize(Policy = AuthorizationPolicies.ExamplesWrite)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
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
