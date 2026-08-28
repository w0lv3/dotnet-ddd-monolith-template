using AutoMapper;
using Example.Application.Mappings;
using Example.Application.Models.Examples;
using Example.Domain.Entities;
using Example.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace Example.Application.Tests.Mappings;

public sealed class ApplicationMappingProfileTests
{
    private readonly MapperConfiguration configuration = new(
        expression => expression.AddProfile<ApplicationMappingProfile>(),
        NullLoggerFactory.Instance);

    [Fact]
    public void Configuration_IsValid()
    {
        configuration.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_ExampleEntity_MapsApplicationDto()
    {
        var entity = ExampleEntity.Create(Guid.NewGuid(), ExampleName.Create("Example name"));
        entity.Activate();
        var mapper = configuration.CreateMapper();

        var dto = mapper.Map<ExampleDto>(entity);

        Assert.Equal(entity.Id, dto.Id);
        Assert.Equal(entity.Name.Value, dto.Name);
        Assert.Equal(entity.Status.ToString(), dto.Status);
    }
}
