using System.Security.Claims;
using Example.Infrastructure.Identity.Cognito;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Example.Api.Authentication.Cognito;

internal static class CognitoAuthenticationExtensions
{
    public static AuthenticationBuilder AddCognitoAuthentication(
        this AuthenticationBuilder builder,
        CognitoOptions cognito,
        bool isDevelopment)
    {
        if (cognito.UseLocalStack && !isDevelopment)
        {
            throw new InvalidOperationException(
                "LocalStack Cognito is allowed only in the Development environment.");
        }

        if (!cognito.RequireHttpsMetadata && !(isDevelopment && cognito.UseLocalStack))
        {
            throw new InvalidOperationException(
                "Cognito HTTP metadata is allowed only for LocalStack in Development.");
        }

        if (cognito.UseLocalStack && string.IsNullOrWhiteSpace(cognito.JwksUri))
        {
            throw new InvalidOperationException(
                "Authentication:Cognito:JwksUri is required when UseLocalStack is true.");
        }

        return builder.AddJwtBearer(options =>
        {
            AuthenticationExtensions.ConfigureJwtBearer(
                options,
                cognito.Authority,
                cognito.Audience,
                cognito.RequireHttpsMetadata);
            options.TokenValidationParameters.ValidateAudience = cognito.Audience is not null;
            options.TokenValidationParameters.AudienceValidator = cognito.Audience is null
                ? null
                : (audiences, _, _) => audiences.Contains(cognito.Audience, StringComparer.Ordinal);
            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context => ValidateAndNormalizeToken(context, cognito)
            };

            if (cognito.JwksUri is not null)
            {
                var documentRetriever = new HttpDocumentRetriever
                {
                    RequireHttps = cognito.RequireHttpsMetadata
                };
                options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    cognito.JwksUri,
                    new JwksConfigurationRetriever(cognito.Authority),
                    documentRetriever);
            }
        });
    }

    private static Task ValidateAndNormalizeToken(
        TokenValidatedContext context,
        CognitoOptions cognito)
    {
        var principal = context.Principal!;

        if (!string.Equals(
                principal.FindFirstValue("token_use"),
                "access",
                StringComparison.Ordinal))
        {
            context.Fail("Only Cognito access tokens are accepted.");
            return Task.CompletedTask;
        }

        if (!string.Equals(
                principal.FindFirstValue("client_id"),
                cognito.ClientId,
                StringComparison.Ordinal))
        {
            context.Fail("The Cognito client_id claim is invalid.");
            return Task.CompletedTask;
        }

        if (cognito.ResourceServerIdentifier is null ||
            principal.Identity is not ClaimsIdentity identity)
        {
            return Task.CompletedTask;
        }

        var prefix = $"{cognito.ResourceServerIdentifier}/";
        var permissions = principal.FindAll("scope")
            .SelectMany(claim => claim.Value.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(scope => scope.StartsWith(prefix, StringComparison.Ordinal))
            .Select(scope => scope[prefix.Length..])
            .Distinct(StringComparer.Ordinal)
            .Select(permission => new Claim("permissions", permission))
            .ToArray();
        identity.AddClaims(permissions);

        return Task.CompletedTask;
    }
}
