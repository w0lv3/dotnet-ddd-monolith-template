using Example.Application.Interfaces.Repositories;
using Example.Application.Interfaces.Services;
using Example.Application.Models.Examples;
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

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetRequiredService<IExampleService>());
        Assert.NotNull(provider.GetRequiredService<IValidator<CreateExampleModel>>());
        Assert.NotNull(provider.GetRequiredService<IValidator<UpdateExampleModel>>());
    }
}
