using Example.Infrastructure.Identity.EntraId;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;

namespace Example.Api.Authentication.EntraId;

internal static class EntraIdAuthenticationExtensions
{
    public static AuthenticationBuilder AddEntraIdAuthentication(
        this AuthenticationBuilder builder,
        IServiceCollection services,
        IConfiguration configuration,
        EntraIdOptions entraId)
    {
        builder.AddMicrosoftIdentityWebApi(
            configuration.GetRequiredSection(EntraIdOptions.SectionName),
            JwtBearerDefaults.AuthenticationScheme);
        services.AddSingleton<IPostConfigureOptions<JwtBearerOptions>>(
            new EntraJwtBearerPostConfigureOptions(entraId));

        return builder;
    }

    private sealed class EntraJwtBearerPostConfigureOptions(EntraIdOptions entraId)
        : IPostConfigureOptions<JwtBearerOptions>
    {
        public void PostConfigure(string? name, JwtBearerOptions options)
        {
            if (!string.Equals(name, JwtBearerDefaults.AuthenticationScheme, StringComparison.Ordinal))
            {
                return;
            }

            options.MapInboundClaims = false;
            options.TokenValidationParameters.ValidateIssuerSigningKey = true;
            options.TokenValidationParameters.ValidateIssuer = true;
            options.TokenValidationParameters.ValidateAudience = true;
            options.TokenValidationParameters.ValidateLifetime = true;
            options.TokenValidationParameters.ValidAudience = entraId.Audience ?? entraId.ClientId;
            options.TokenValidationParameters.NameClaimType = "sub";
            options.TokenValidationParameters.RoleClaimType = "roles";
        }
    }
}
