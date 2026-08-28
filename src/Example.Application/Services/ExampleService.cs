using AutoMapper;
using Example.Application.Exceptions;
using Example.Application.Interfaces.Repositories;
using Example.Application.Interfaces.Services;
using Example.Application.Models.Examples;
using Example.Domain.Entities;
using Example.Domain.ValueObjects;
using FluentValidation;

namespace Example.Application.Services;

public sealed class ExampleService(
    IExampleRepository repository,
    IMapper mapper,
    IValidator<CreateExampleModel> createValidator,
    IValidator<UpdateExampleModel> updateValidator) : IExampleService
{
    public async Task<ExampleDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        EnsureIdentifier(id);

        var entity = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new ExampleNotFoundException(id);

        return mapper.Map<ExampleDto>(entity);
    }

    public async Task<IReadOnlyCollection<ExampleDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var entities = await repository.GetAllAsync(cancellationToken);

        return entities
            .Select(entity => mapper.Map<ExampleDto>(entity))
            .ToArray();
    }

    public async Task<ExampleDto> CreateAsync(
        CreateExampleModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        await createValidator.ValidateAndThrowAsync(model, cancellationToken);

        var entity = ExampleEntity.Create(Guid.NewGuid(), ExampleName.Create(model.Name));

        await repository.AddAsync(entity, cancellationToken);

        return mapper.Map<ExampleDto>(entity);
    }

    public async Task<ExampleDto> UpdateAsync(
        UpdateExampleModel model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(model);
        await updateValidator.ValidateAndThrowAsync(model, cancellationToken);

        var entity = await repository.GetByIdAsync(model.Id, cancellationToken)
            ?? throw new ExampleNotFoundException(model.Id);

        entity.Rename(ExampleName.Create(model.Name));
        await repository.UpdateAsync(entity, cancellationToken);

        return mapper.Map<ExampleDto>(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        EnsureIdentifier(id);

        var entity = await repository.GetByIdAsync(id, cancellationToken)
            ?? throw new ExampleNotFoundException(id);

        await repository.DeleteAsync(entity, cancellationToken);
    }

    private static void EnsureIdentifier(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Example entity identifier cannot be empty.", nameof(id));
        }
    }
}
