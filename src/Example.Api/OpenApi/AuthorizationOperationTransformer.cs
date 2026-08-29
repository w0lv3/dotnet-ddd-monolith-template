using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Example.Api.OpenApi;

public sealed class AuthorizationOperationTransformer : IOpenApiOperationTransformer
{
    internal const string BearerSchemeName = "Bearer";

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        var metadata = context.Description.ActionDescriptor.EndpointMetadata;
        if (metadata.OfType<IAllowAnonymous>().Any())
        {
            return Task.CompletedTask;
        }

        var authorization = metadata.OfType<IAuthorizeData>().ToArray();
        if (authorization.Length == 0)
        {
            return Task.CompletedTask;
        }

        var permissions = authorization
            .Select(authorizeData => authorizeData.Policy)
            .Where(policy => !string.IsNullOrWhiteSpace(policy))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(policy => policy, StringComparer.Ordinal)
            .ToArray();

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference(BearerSchemeName, context.Document)] = []
        });

        RemoveResponseContent(operation, StatusCodes.Status401Unauthorized);
        RemoveResponseContent(operation, StatusCodes.Status403Forbidden);

        if (permissions.Length > 0)
        {
            var requiredPermissions = new JsonArray();
            foreach (var permission in permissions)
            {
                requiredPermissions.Add(permission);
            }

            operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
            operation.Extensions["x-required-permissions"] =
                new JsonNodeExtension(requiredPermissions);
        }

        return Task.CompletedTask;
    }

    private static void RemoveResponseContent(OpenApiOperation operation, int statusCode)
    {
        if (operation.Responses?.TryGetValue(statusCode.ToString(), out var response) == true)
        {
            response.Content?.Clear();
        }
    }
}
