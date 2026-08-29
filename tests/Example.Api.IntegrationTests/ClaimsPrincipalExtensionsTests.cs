using System.Security.Claims;
using Example.Api.Authentication;
using Example.Application.Interfaces.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Example.Api.IntegrationTests;

public sealed class ClaimsPrincipalExtensionsTests
{
    [Fact]
    public void Extensions_ReadCommonClaims()
    {
        var principal = CreatePrincipal();

        Assert.Equal("user-123", principal.GetUserId());
        Assert.Equal("user@example.com", principal.GetEmail());
        Assert.Equal(["admin", "operator"], principal.GetRoles());
        Assert.Equal(["team-one", "team-two"], principal.GetGroups());
        Assert.True(principal.HasScope("examples.read"));
        Assert.True(principal.HasScope("examples.write"));
        Assert.False(principal.HasScope("examples.delete"));
    }

    [Fact]
    public void CurrentUser_UsesTheRequestPrincipal()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:Provider"] = "Keycloak",
                ["Authentication:Authority"] = "https://issuer.example",
                ["Authentication:Audience"] = "example-api"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiAuthentication(configuration);
        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = CreatePrincipal()
        };

        using var scope = serviceProvider.CreateScope();
        var currentUser = scope.ServiceProvider.GetRequiredService<ICurrentUser>();

        Assert.True(currentUser.IsAuthenticated);
        Assert.Equal("user-123", currentUser.UserId);
        Assert.Equal("user@example.com", currentUser.Email);
    }

    private static ClaimsPrincipal CreatePrincipal()
    {
        Claim[] claims =
        [
            new("sub", "user-123"),
            new("email", "user@example.com"),
            new("roles", "admin"),
            new("roles", "operator"),
            new("groups", "team-one"),
            new("groups", "team-two"),
            new("scope", "examples.read profile"),
            new("scp", "examples.write")
        ];

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }
}
