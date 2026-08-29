namespace Example.Infrastructure.Identity.Cognito;

public sealed class CognitoOptions
{
    public const string SectionName = "Authentication:Cognito";

    public string Region { get; init; } = "";

    public string UserPoolId { get; init; } = "";

    public string ClientId { get; init; } = "";

    public string Authority { get; init; } = "";

    public string? Audience { get; init; }

    public string? ResourceServerIdentifier { get; init; }

    public string? JwksUri { get; init; }

    public bool RequireHttpsMetadata { get; init; } = true;

    public bool UseLocalStack { get; init; }
}
