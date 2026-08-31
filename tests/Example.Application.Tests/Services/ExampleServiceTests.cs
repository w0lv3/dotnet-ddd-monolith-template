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
        Assert.Equal(entity.Status.ToString(), result.Status);
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
        Assert.Equal($"Example entity with identifier '{id}' was not found.", exception.Message);
        await repository.Received(1).GetByIdAsync(id, CancellationToken.None);
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
        var cancellationToken = new CancellationTokenSource().Token;
        var entities = new[] { CreateEntity(), CreateEntity() };
        entities[1].Activate();
        repository.GetAllAsync(cancellationToken).Returns(entities);

        var result = await service.GetAllAsync(cancellationToken);

        Assert.Collection(
            result,
            dto => AssertDtoMatchesEntity(dto, entities[0]),
            dto => AssertDtoMatchesEntity(dto, entities[1]));
        await repository.Received(1).GetAllAsync(cancellationToken);
    }

    [Fact]
    public async Task CreateAsync_WithNullModel_DoesNotCallRepository()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.CreateAsync(null!, CancellationToken.None));

        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public async Task CreateAsync_WithValidModel_AddsAndReturnsEntity()
    {
        var cancellationToken = new CancellationTokenSource().Token;

        var result = await service.CreateAsync(new CreateExampleModel("  Example name  "), cancellationToken);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Example name", result.Name);
        Assert.Equal("Inactive", result.Status);
        await repository.Received(1).AddAsync(
            Arg.Is<ExampleEntity>(entity =>
                entity.Id == result.Id &&
                entity.Name.Value == "Example name" &&
                entity.Status == Example.Domain.Enums.ExampleStatus.Inactive),
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
            new UpdateExampleModel(entity.Id, "  Renamed example  "),
            cancellationToken);

        Assert.Equal("Renamed example", entity.Name.Value);
        Assert.Equal("Renamed example", result.Name);
        await repository.Received(1).UpdateAsync(entity, cancellationToken);
    }

    [Fact]
    public async Task UpdateAsync_WithNullModel_DoesNotCallRepository()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.UpdateAsync(null!, CancellationToken.None));

        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidModel_DoesNotAccessRepositoryOrMutateEntity()
    {
        var entity = CreateEntity();
        var originalName = entity.Name;

        await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateAsync(
                new UpdateExampleModel(entity.Id, " "),
                CancellationToken.None));

        Assert.Equal(originalName, entity.Name);
        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyIdentifier_DoesNotCallRepository()
    {
        await Assert.ThrowsAsync<ValidationException>(
            () => service.UpdateAsync(
                new UpdateExampleModel(Guid.Empty, "Example name"),
                CancellationToken.None));

        Assert.Empty(repository.ReceivedCalls());
    }

    [Fact]
    public async Task UpdateAsync_WithMissingEntity_ThrowsExampleNotFoundException()
    {
        var id = Guid.NewGuid();
        repository.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((ExampleEntity?)null);

        var exception = await Assert.ThrowsAsync<ExampleNotFoundException>(
            () => service.UpdateAsync(
                new UpdateExampleModel(id, "Example name"),
                CancellationToken.None));

        Assert.Equal(id, exception.Id);
        await repository.Received(1).GetByIdAsync(id, CancellationToken.None);
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

        var exception = await Assert.ThrowsAsync<ExampleNotFoundException>(
            () => service.DeleteAsync(id, CancellationToken.None));

        Assert.Equal(id, exception.Id);
        await repository.Received(1).GetByIdAsync(id, CancellationToken.None);
        await repository.DidNotReceiveWithAnyArgs().DeleteAsync(default!, default);
    }

    [Fact]
    public async Task DeleteAsync_WithEmptyIdentifier_DoesNotCallRepository()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.DeleteAsync(Guid.Empty, CancellationToken.None));

        Assert.Empty(repository.ReceivedCalls());
    }

    private static void AssertDtoMatchesEntity(ExampleDto dto, ExampleEntity entity)
    {
        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.Name.Value, dto.Name);
        Assert.Equal(entity.Status.ToString(), dto.Status);
    }

    private static ExampleEntity CreateEntity()
    {
        return ExampleEntity.Create(Guid.NewGuid(), ExampleName.Create("Example name"));
    }
}
