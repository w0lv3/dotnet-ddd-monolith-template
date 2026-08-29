namespace Example.Infrastructure.Identity.EntraId;

public sealed class EntraIdOptions
{
    public const string SectionName = "Authentication:EntraId";

    public string TenantId { get; init; } = "";

    public string ClientId { get; init; } = "";

    public string Instance { get; init; } = "";

    public string? Audience { get; init; }
}
