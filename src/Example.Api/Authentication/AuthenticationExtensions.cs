using Example.Application.Interfaces.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Example.Api.Authentication;

public static class AuthenticationExtensions
{
    private const string ConfigurationSection = "Authentication";

    public static IServiceCollection AddApiAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(ConfigurationSection);
        var providerName = section["Provider"];

        if (!Enum.TryParse<AuthenticationProvider>(providerName, true, out _))
        {
            throw new InvalidOperationException(
                $"Authentication:Provider must be one of: {string.Join(", ", Enum.GetNames<AuthenticationProvider>())}.");
        }

        var authority = GetRequiredValue(section, "Authority");
        var audience = GetRequiredValue(section, "Audience");

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                options.Audience = audience;
                options.RequireHttpsMetadata = section.GetValue("RequireHttpsMetadata", true);
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    NameClaimType = "sub",
                    RoleClaimType = "roles"
                };
            });
        services.AddAuthorization(AuthorizationPolicies.AddPolicies);

        return services;
    }

    private static string GetRequiredValue(IConfigurationSection section, string key)
    {
        var value = section[key];

        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"Authentication:{key} must be configured.")
            : value;
    }
}
