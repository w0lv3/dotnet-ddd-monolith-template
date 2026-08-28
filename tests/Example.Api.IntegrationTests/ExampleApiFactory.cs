using System.Security.Claims;
using Example.Application.Interfaces.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using NSubstitute.ClearExtensions;

namespace Example.Api.IntegrationTests;

public sealed class ExampleApiFactory : WebApplicationFactory<Program>
{
    public IExampleService ExampleService { get; } = Substitute.For<IExampleService>();

    public void ResetService()
    {
        ExampleService.ClearSubstitute();
    }

    public HttpClient CreateHttpsClient()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] =
                    "Host=localhost;Port=1;Database=test;Username=test;Password=test"
            });
        });
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IExampleService>();
            services.AddSingleton(ExampleService);
            services.AddSingleton<IStartupFilter, TestUserStartupFilter>();
        });
    }

    private sealed class TestUserStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return applicationBuilder =>
            {
                applicationBuilder.Use(async (httpContext, nextMiddleware) =>
                {
                    if (httpContext.Request.Headers.ContainsKey("X-Test-Authenticated"))
                    {
                        var identity = new ClaimsIdentity(
                            [new Claim(ClaimTypes.NameIdentifier, "test-user")],
                            "Test");
                        httpContext.User = new ClaimsPrincipal(identity);
                    }

                    await nextMiddleware(httpContext);
                });

                next(applicationBuilder);
            };
        }
    }
}
