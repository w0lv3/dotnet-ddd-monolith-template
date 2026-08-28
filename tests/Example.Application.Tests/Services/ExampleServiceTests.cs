using AutoMapper;
using Example.Application.Exceptions;
using Example.Application.Interfaces.Repositories;
using Example.Application.Mappings;
using Example.Application.Models.Examples;
using Example.Application.Services;
using Example.Application.Validators.Examples;
using Example.Domain.Entities;
using Example.Domain.ValueObjects;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Example.Application.Tests.Services;

public sealed class ExampleServiceTests
{
    private readonly IExampleRepository repository = Substitute.For<IExampleRepository>();
    private readonly ExampleService service;

    public ExampleServiceTests()
    {
        var configuration = new MapperConfiguration(
            expression => expression.AddProfile<ApplicationMappingProfile>(),
            NullLoggerFactory.Instance);

        service = new ExampleService(
            repository,
            configuration.CreateMapper(),
            new CreateExampleValidator(),
            new UpdateExampleValidator());
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingEntity_ReturnsDto()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        var entity = CreateEntity();
        repository.GetByIdAsync(entity.Id, cancellationToken).Returns(entity);

        var result = await service.GetByIdAsync(entity.Id, cancellationToken);

        Assert.Equal(entity.Id, result.Id);
        Assert.Equal(entity.Name.Value, result.Name);
        await repository.Received(1).GetByIdAsync(entity.Id, cancellationToken);
    }

    [Fact]
    public async Task GetByIdAsync_WithMissingEntity_ThrowsExampleNotFoundException()
    {
        var id = Guid.NewGuid();
        repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ExampleEntity?)null);

        var exception = await Assert.ThrowsAsync<ExampleNotFoundException>(
            () => service.GetByIdAsync(id, CancellationToken.None));

        Assert.Equal(id, exception.Id);
    }

    [Fact]
    public async Task GetByIdAsync_WithEmptyIdentifier_DoesNotCallRepository()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.GetByIdAsync(Guid.Empty, CancellationToken.None));

        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedDtos()
    {
        var entities = new[] { CreateEntity(), CreateEntity() };
        repository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(entities);

        var result = await service.GetAllAsync(CancellationToken.None);

        Assert.Equal(entities.Select(entity => entity.Id), result.Select(dto => dto.Id));
    }

    [Fact]
    public async Task CreateAsync_WithValidModel_AddsAndReturnsEntity()
    {
        var cancellationToken = new CancellationTokenSource().Token;

        var result = await service.CreateAsync(new CreateExampleModel("Example name"), cancellationToken);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Example name", result.Name);
        await repository.Received(1).AddAsync(
            Arg.Is<ExampleEntity>(entity => entity.Id == result.Id),
            cancellationToken);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidModel_DoesNotCallRepository()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(
                new CreateExampleModel(""),
                CancellationToken.None));

        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public async Task UpdateAsync_WithExistingEntity_RenamesAndUpdatesEntity()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        var entity = CreateEntity();
        repository.GetByIdAsync(entity.Id, cancellationToken).Returns(entity);

        var result = await service.UpdateAsync(
            new UpdateExampleModel(entity.Id, "Renamed example"),
            cancellationToken);

        Assert.Equal("Renamed example", entity.Name.Value);
        Assert.Equal("Renamed example", result.Name);
        await repository.Received(1).UpdateAsync(entity, cancellationToken);
    }

    [Fact]
    public async Task UpdateAsync_WithMissingEntity_ThrowsExampleNotFoundException()
    {
        var id = Guid.NewGuid();
        repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ExampleEntity?)null);

        await Assert.ThrowsAsync<ExampleNotFoundException>(
            () => service.UpdateAsync(
                new UpdateExampleModel(id, "Example name"),
                CancellationToken.None));

        await repository.DidNotReceiveWithAnyArgs().UpdateAsync(default!, default);
    }

    [Fact]
    public async Task DeleteAsync_WithExistingEntity_DeletesEntity()
    {
        var cancellationToken = new CancellationTokenSource().Token;
        var entity = CreateEntity();
        repository.GetByIdAsync(entity.Id, cancellationToken).Returns(entity);

        await service.DeleteAsync(entity.Id, cancellationToken);

        await repository.Received(1).DeleteAsync(entity, cancellationToken);
    }

    [Fact]
    public async Task DeleteAsync_WithMissingEntity_ThrowsExampleNotFoundException()
    {
        var id = Guid.NewGuid();
        repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ExampleEntity?)null);

        await Assert.ThrowsAsync<ExampleNotFoundException>(
            () => service.DeleteAsync(id, CancellationToken.None));

        await repository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }

    private static ExampleEntity CreateEntity()
    {
        return ExampleEntity.Create(Guid.NewGuid(), ExampleName.Create("Example name"));
    }
}
