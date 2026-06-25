using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Backend.Api.OpenApi;

/// <summary>
/// Document transformer que declara el esquema de seguridad Bearer (JWT) en el documento
/// OpenAPI (spec 010, US3, FR-011). Habilita el botón "Authorize" de Swagger UI para que
/// "Try it out" pueda invocar endpoints protegidos. La autenticación efectiva se implementa
/// en una spec de seguridad independiente; aquí solo se documenta el esquema.
/// </summary>
public sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Introduzca el token JWT de administrador (sin el prefijo 'Bearer ')."
        };

        return Task.CompletedTask;
    }
}
