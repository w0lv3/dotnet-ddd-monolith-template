using AutoMapper;
using Example.Application.Models.Examples;
using Example.Domain.Entities;

namespace Example.Application.Mappings;

public sealed class ApplicationMappingProfile : Profile
{
    public ApplicationMappingProfile()
    {
        CreateMap<ExampleEntity, ExampleDto>()
            .ConstructUsing(entity => new ExampleDto(
                entity.Id,
                entity.Name.Value,
                entity.Status.ToString()));
    }
}
