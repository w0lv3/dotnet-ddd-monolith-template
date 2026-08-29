using Microsoft.Extensions.Configuration;

namespace Example.Infrastructure.Identity.Cognito;

public static class CognitoExtensions
{
    public static CognitoOptions GetCognitoOptions(this IConfiguration configuration)
    {
        var section = configuration.GetRequiredSection(CognitoOptions.SectionName);
        var region = GetRequiredValue(section, nameof(CognitoOptions.Region));
        var userPoolId = GetRequiredValue(section, nameof(CognitoOptions.UserPoolId));
        var authority = section[nameof(CognitoOptions.Authority)];

        return new CognitoOptions
        {
            Region = region,
            UserPoolId = userPoolId,
            ClientId = GetRequiredValue(section, nameof(CognitoOptions.ClientId)),
            Authority = string.IsNullOrWhiteSpace(authority)
                ? $"https://cognito-idp.{region}.amazonaws.com/{userPoolId}"
                : authority,
            Audience = NullIfWhiteSpace(section[nameof(CognitoOptions.Audience)]),
            ResourceServerIdentifier = NullIfWhiteSpace(
                section[nameof(CognitoOptions.ResourceServerIdentifier)]),
            JwksUri = NullIfWhiteSpace(section[nameof(CognitoOptions.JwksUri)]),
            RequireHttpsMetadata = GetBoolean(
                section,
                nameof(CognitoOptions.RequireHttpsMetadata),
                true),
            UseLocalStack = GetBoolean(section, nameof(CognitoOptions.UseLocalStack), false)
        };
    }

    private static string GetRequiredValue(IConfigurationSection section, string key)
    {
        return string.IsNullOrWhiteSpace(section[key])
            ? throw new InvalidOperationException($"{CognitoOptions.SectionName}:{key} must be configured.")
            : section[key]!;
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool GetBoolean(IConfigurationSection section, string key, bool defaultValue)
    {
        var value = section[key];

        return string.IsNullOrWhiteSpace(value)
            ? defaultValue
            : bool.TryParse(value, out var result)
                ? result
                : throw new InvalidOperationException(
                    $"{CognitoOptions.SectionName}:{key} must be true or false.");
    }
}
