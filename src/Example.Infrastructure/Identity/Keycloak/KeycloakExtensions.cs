using Microsoft.Extensions.Configuration;

namespace Example.Infrastructure.Identity.Keycloak;

public static class KeycloakExtensions
{
    public static KeycloakOptions GetKeycloakOptions(this IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(KeycloakOptions.SectionName);

        return new KeycloakOptions
        {
            Authority = GetRequiredValue(section, nameof(KeycloakOptions.Authority)),
            Audience = GetRequiredValue(section, nameof(KeycloakOptions.Audience)),
            RequireHttpsMetadata = GetBoolean(
                section,
                nameof(KeycloakOptions.RequireHttpsMetadata),
                true)
        };
    }

    private static string GetRequiredValue(IConfigurationSection section, string key)
    {
        return string.IsNullOrWhiteSpace(section[key])
            ? throw new InvalidOperationException($"{KeycloakOptions.SectionName}:{key} must be configured.")
            : section[key]!;
    }

    private static bool GetBoolean(IConfigurationSection section, string key, bool defaultValue)
    {
        var value = section[key];

        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : bool.TryParse(value, out var result)
                ? result
                : throw new InvalidOperationException(
                    $"{KeycloakOptions.SectionName}:{key} must be true or false.");
    }
}
