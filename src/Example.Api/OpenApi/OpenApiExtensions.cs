using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Example.Api.OpenApi;

public static class OpenApiExtensions
{
    public static IServiceCollection AddApiOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<AuthorizationOperationTransformer>();
        });

        return services;
    }

    public static IEndpointRouteBuilder MapApiOpenApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapOpenApi("/openapi/{documentName}.json").AllowAnonymous();
        endpoints.MapGet("/openapi/{documentName}.yaml", WriteYamlAsync)
            .AllowAnonymous()
            .ExcludeFromDescription();

        return endpoints;
    }

    private static async Task WriteYamlAsync(
        HttpContext httpContext,
        string documentName,
        CancellationToken cancellationToken)
    {
        var documentProvider = httpContext.RequestServices
            .GetRequiredKeyedService<IOpenApiDocumentProvider>(documentName);
        var document = await documentProvider.GetOpenApiDocumentAsync(cancellationToken);

        if (document.Components?.Schemas is not null)
        {
            var environment = httpContext.RequestServices.GetRequiredService<IHostEnvironment>();
            var applicationName = environment.ApplicationName.EndsWith(".Api", StringComparison.Ordinal)
                ? environment.ApplicationName[..^4]
                : environment.ApplicationName;
            var schemas = document.Components.Schemas
                .OrderBy(
                    schema => schema.Key.Replace(applicationName, string.Empty, StringComparison.Ordinal),
                    StringComparer.Ordinal)
                .ThenBy(schema => schema.Key, StringComparer.Ordinal)
                .ToArray();
            document.Components.Schemas.Clear();
            foreach (var schema in schemas)
            {
                document.Components.Schemas.Add(schema.Key, schema.Value);
            }
        }

        httpContext.Response.ContentType = "application/yaml";
        var yaml = await document.SerializeAsYamlAsync(
            OpenApiSpecVersion.OpenApi3_1,
            cancellationToken);
        await httpContext.Response.WriteAsync(yaml, cancellationToken);
    }
}
