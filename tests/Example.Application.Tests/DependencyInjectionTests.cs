using Example.Application.Interfaces.Repositories;
using Example.Application.Interfaces.Services;
using Example.Application.Models.Examples;
using Example.Application.Services;
using Example.Application.Validators.Examples;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Example.Application.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersApplicationServices()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddSingleton(Substitute.For<IExampleRepository>());

        services.AddApplication();

        AssertScoped<IExampleService, ExampleService>(services);
        AssertScoped<IValidator<CreateExampleModel>, CreateExampleValidator>(services);
        AssertScoped<IValidator<UpdateExampleModel>, UpdateExampleValidator>(services);

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IExampleService>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IValidator<CreateExampleModel>>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IValidator<UpdateExampleModel>>());
    }

    private static void AssertScoped<TService, TImplementation>(IServiceCollection services)
    {
        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(TService) &&
                descriptor.ImplementationType == typeof(TImplementation) &&
                descriptor.Lifetime == ServiceLifetime.Scoped);
    }
}
