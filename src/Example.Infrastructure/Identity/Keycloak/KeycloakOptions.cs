namespace Example.Infrastructure.Identity.Keycloak;

public sealed class KeycloakOptions
{
    public const string SectionName = "Authentication:Keycloak";

    public string Authority { get; init; } = "";

    public string Audience { get; init; } = "";

    public bool RequireHttpsMetadata { get; init; } = true;
}
