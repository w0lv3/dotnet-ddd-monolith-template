using Example.Infrastructure.Identity.Keycloak;
using Microsoft.AspNetCore.Authentication;

namespace Example.Api.Authentication.Keycloak;

internal static class KeycloakAuthenticationExtensions
{
    public static AuthenticationBuilder AddKeycloakAuthentication(
        this AuthenticationBuilder builder,
        KeycloakOptions keycloak,
        bool isDevelopment)
    {
        if (!keycloak.RequireHttpsMetadata && !isDevelopment)
        {
            throw new InvalidOperationException(
                "Keycloak HTTP metadata is allowed only in the Development environment.");
        }

        return builder.AddJwtBearer(options => AuthenticationExtensions.ConfigureJwtBearer(
            options,
            keycloak.Authority,
            keycloak.Audience,
            keycloak.RequireHttpsMetadata));
    }
}
