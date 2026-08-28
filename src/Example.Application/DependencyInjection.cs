using Example.Application.Interfaces.Services;
using Example.Application.Mappings;
using Example.Application.Services;
using Example.Application.Validators.Examples;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Example.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(configuration => configuration.AddProfile<ApplicationMappingProfile>());
        services.AddValidatorsFromAssemblyContaining<CreateExampleValidator>();
        services.AddScoped<IExampleService, ExampleService>();

        return services;
    }
}
