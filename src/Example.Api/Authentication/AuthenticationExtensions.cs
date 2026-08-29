using Example.Api.Authentication.Cognito;
using Example.Api.Authentication.EntraId;
using Example.Api.Authentication.Keycloak;
using Example.Application.Interfaces.Identity;
using Example.Infrastructure.Identity.Cognito;
using Example.Infrastructure.Identity.EntraId;
using Example.Infrastructure.Identity.Keycloak;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Example.Api.Authentication;

public static class AuthenticationExtensions
{
    private const string ConfigurationSection = "Authentication";

    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        var section = configuration.GetRequiredSection(ConfigurationSection);
        var providerName = section["Provider"];

        if (!Enum.TryParse<AuthenticationProvider>(providerName, true, out var provider))
        {
            throw new InvalidOperationException(
                $"Authentication:Provider must be one of: {string.Join(", ", Enum.GetNames<AuthenticationProvider>())}.");
        }

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        var authentication = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme);

        switch (provider)
        {
            case AuthenticationProvider.Keycloak:
                authentication.AddKeycloakAuthentication(
                    configuration.GetKeycloakOptions(),
                    isDevelopment);
                break;
            case AuthenticationProvider.Cognito:
                authentication.AddCognitoAuthentication(
                    configuration.GetCognitoOptions(),
                    isDevelopment);
                break;
            case AuthenticationProvider.EntraId:
                authentication.AddEntraIdAuthentication(
                    services,
                    configuration,
                    configuration.GetEntraIdOptions());
                break;
            default:
                throw new InvalidOperationException($"Unsupported authentication provider: {provider}.");
        }

        services.AddAuthorization(AuthorizationPolicies.AddPolicies);

        return services;
    }

    internal static void ConfigureJwtBearer(
        JwtBearerOptions options,
        string authority,
        string? audience,
        bool requireHttpsMetadata)
    {
        options.Authority = authority;
        options.Audience = audience;
        options.RequireHttpsMetadata = requireHttpsMetadata;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = audience is not null,
            ValidateLifetime = true,
            NameClaimType = "sub",
            RoleClaimType = "roles"
        };
    }
}
