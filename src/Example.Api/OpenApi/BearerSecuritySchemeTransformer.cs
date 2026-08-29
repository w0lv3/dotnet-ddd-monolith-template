using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Example.Api.OpenApi;

public sealed class BearerSecuritySchemeTransformer(IHostEnvironment environment)
    : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Info = new OpenApiInfo
        {
            Title = $"{environment.ApplicationName} API",
            Version = context.DocumentName,
            Description =
                "A simplified layered Domain-Driven Design API. Protected operations require " +
                "JWT bearer tokens with the documented permissions in scope, scp, or roles claims."
        };
        document.Servers = [new OpenApiServer { Url = "/" }];
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??=
            new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[AuthorizationOperationTransformer.BearerSchemeName] =
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description =
                    "JWT bearer token. GET operations require examples.read; POST, PUT, and " +
                    "DELETE operations require examples.write. Permissions may be supplied " +
                    "through OAuth scope/scp claims or roles."
            };

        return Task.CompletedTask;
    }
}
