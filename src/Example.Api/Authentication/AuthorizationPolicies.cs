using Microsoft.AspNetCore.Authorization;

namespace Example.Api.Authentication;

public static class AuthorizationPolicies
{
    public const string ExamplesRead = "examples.read";
    public const string ExamplesWrite = "examples.write";

    internal static void AddPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(ExamplesRead, policy => AddPermissionRequirement(policy, ExamplesRead));
        options.AddPolicy(ExamplesWrite, policy => AddPermissionRequirement(policy, ExamplesWrite));
    }

    private static void AddPermissionRequirement(
        AuthorizationPolicyBuilder policy,
        string permission)
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => context.User.HasPermission(permission));
    }
}
