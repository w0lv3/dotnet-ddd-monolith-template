using Microsoft.Extensions.Configuration;

namespace Example.Infrastructure.Identity.EntraId;

public static class EntraIdExtensions
{
    public static EntraIdOptions GetEntraIdOptions(this IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(EntraIdOptions.SectionName);

        return new EntraIdOptions
        {
            TenantId = GetRequiredValue(section, nameof(EntraIdOptions.TenantId)),
            ClientId = GetRequiredValue(section, nameof(EntraIdOptions.ClientId)),
            Instance = GetRequiredValue(section, nameof(EntraIdOptions.Instance)),
            Audience = NullIfWhiteSpace(section[nameof(EntraIdOptions.Audience)])
        };
    }

    private static string GetRequiredValue(IConfigurationSection section, string key)
    {
        return string.IsNullOrWhiteSpace(section[key])
            ? throw new InvalidOperationException($"{EntraIdOptions.SectionName}:{key} must be configured.")
            : section[key]!;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
