using Microsoft.AspNetCore.Authorization;

namespace Example.Api.Authentication;

public static class AuthorizationPolicies
{
    public const string ExamplesRead = "examples.read";
    public const string ExamplesWrite = "examples.write";

    internal static void AddPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(ExamplesRead, policy => AddScopeRequirement(policy, ExamplesRead));
        options.AddPolicy(ExamplesWrite, policy => AddScopeRequirement(policy, ExamplesWrite));
    }

    private static void AddScopeRequirement(AuthorizationPolicyBuilder policy, string scope)
    {
        policy.RequireAuthenticatedUser();
        policy.RequireAssertion(context => context.User.HasScope(scope));
    }
}
