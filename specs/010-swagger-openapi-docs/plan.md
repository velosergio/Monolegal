# Plan de Implementación: Documentación Swagger/OpenAPI

**Branch**: `010-swagger-openapi-docs` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

**Input**: Especificación de funcionalidad desde `specs/010-swagger-openapi-docs/spec.md`

## Summary

Exponer una página de documentación interactiva de la API en la ruta `/swagger`, alimentada por el documento OpenAPI que el backend ya genera mediante `Microsoft.AspNetCore.OpenApi` (`AddOpenApi` / `MapOpenApi`, disponible hoy en `/openapi/v1.json` solo en Development). La página debe listar todos los endpoints, mostrar sus modelos/DTO, documentar los códigos de estado posibles y permitir "Try it out".

Enfoque técnico: (1) añadir el paquete UI-only `Swashbuckle.AspNetCore.SwaggerUI` y habilitar `UseSwaggerUI` apuntando a `/openapi/v1.json`, servido bajo `/swagger`, dentro del gate `IsDevelopment()` ya existente; (2) enriquecer los metadatos OpenAPI de los endpoints de facturas existentes (resumen/descripción y respuestas `Produces` para `200/400/404`) para que la documentación sea completa y refleje los contratos reales de la spec 009; (3) declarar el esquema de seguridad (Bearer/JWT) en el documento OpenAPI para que "Try it out" pueda autorizar endpoints protegidos (la autenticación efectiva se aborda en una spec de seguridad independiente). No se modifica la lógica de negocio ni los contratos de los endpoints.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`, SDK 10.0.301)

**Primary Dependencies**: `Microsoft.AspNetCore.OpenApi` 10.0.6 (ya presente, generación del documento), `Swashbuckle.AspNetCore.SwaggerUI` 10.1.7 (NUEVO, solo la interfaz Swagger UI), Serilog.AspNetCore (ya presente)

**Storage**: N/A — la documentación se genera en memoria a partir del grafo de endpoints; no hay persistencia nueva

**Testing**: xUnit + Shouldly. Pruebas de integración con `WebApplicationFactory` (host de pruebas) en entorno Development para verificar: disponibilidad del documento `/openapi/v1.json`, presencia de las operaciones y esquemas esperados, y respuesta de la UI en `/swagger`

**Target Platform**: Servicio web Linux (contenedor Docker), backend administrativo

**Project Type**: Web service (backend ASP.NET Core con capas Domain/Application/Infrastructure/Api). Esta feature toca exclusivamente la capa `Api`

**Performance Goals**: La página de documentación es una utilidad de desarrollo/integración; no participa del presupuesto de ≤200 ms de los endpoints de negocio. La carga de la UI debe ser inmediata (assets estáticos servidos por el middleware)

**Constraints**: Exposición restringida a entornos no productivos (gate `IsDevelopment()`), conforme a la guía de seguridad de Microsoft y a los Assumptions de la spec; el documento OpenAPI debe permanecer sincronizado automáticamente con los endpoints (sin documento mantenido a mano)

**Scale/Scope**: 1 paquete nuevo, ~10 líneas de wiring en `Program.cs`, enriquecimiento de metadatos en 4 endpoints existentes (+`PayInvoice`/settings/workers opcionalmente), 1 entrada en `launchSettings.json`, y una suite de pruebas de integración de documentación

## Constitution Check

*GATE: Debe pasar antes de Phase 0. Re-evaluado tras Phase 1.*

| Principio | Evaluación | Estado |
|-----------|------------|--------|
| I. Arquitectura Limpia | El cambio vive íntegramente en la capa `Api` (composición/presentación). No introduce dependencias hacia afuera ni filtra detalles de Infrastructure a capas internas. | ✅ PASS |
| II. SOLID | El enriquecimiento de metadatos se hace por endpoint (SRP) mediante extensión de la configuración existente (OCP); no se modifican firmas ni contratos. | ✅ PASS |
| III. SDD (specs en español) | Spec 010 escrita y validada; todos los artefactos de este plan en español. | ✅ PASS |
| IV. Test-First (≥85%) | Pruebas de integración de documentación (documento OpenAPI presente, operaciones/esquemas esperados, UI accesible) se escriben antes del wiring. | ✅ PASS (compromiso) |
| V. Frontend Producción | No aplica: feature de backend (documentación de API). | ➖ N/A |
| VI. Observable y Mantenible | Documentación auto-generada desde el grafo de endpoints (mantenible, sin doc manual). Decisiones no obvias registradas en research.md. | ✅ PASS |
| Stack tecnológico | `Microsoft.AspNetCore.OpenApi` ya en el stack; se añade `Swashbuckle.AspNetCore.SwaggerUI` (solo UI), sin reintroducir el Swashbuckle completo ni cambiar el generador. | ✅ PASS |
| Seguridad | Documentación restringida a no-producción (gate de entorno). Se declara el esquema de seguridad Bearer en OpenAPI para habilitar "Try it out" autenticado; la autenticación efectiva se difiere a la spec de seguridad. | ⚠️ DEFERRED (auth efectiva) |
| Performance | La UI no afecta el presupuesto de los endpoints de negocio; assets estáticos. | ✅ PASS |

**Resultado del gate**: PASS. El único elemento diferido (autenticación JWT efectiva) corresponde a una spec de seguridad independiente y está documentado en los Assumptions de la spec; ningún principio NO NEGOCIABLE se incumple.

## Project Structure

### Documentation (this feature)

```text
specs/010-swagger-openapi-docs/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Phase 0 — decisiones técnicas
├── data-model.md        # Phase 1 — esquemas expuestos en el documento OpenAPI
├── quickstart.md        # Phase 1 — guía de validación end-to-end
├── contracts/           # Phase 1 — contratos de los recursos de documentación
│   ├── openapi-document.md   # GET /openapi/v1.json (documento OpenAPI)
│   └── swagger-ui.md         # GET /swagger (interfaz interactiva)
├── checklists/
│   └── requirements.md  # Checklist de calidad (ya existente)
└── tasks.md             # Phase 2 (/speckit-tasks — NO creado por /speckit-plan)
```

### Source Code (repository root)

```text
backend/
└── Api/
    ├── Api.csproj                         # + PackageReference Swashbuckle.AspNetCore.SwaggerUI 10.1.7
    ├── Program.cs                         # + UseSwaggerUI(/openapi/v1.json) bajo IsDevelopment();
    │                                       #   + esquema de seguridad Bearer en AddOpenApi (document transformer)
    ├── Properties/
    │   └── launchSettings.json            # (nuevo/editado) launchUrl = "swagger"
    └── Endpoints/
        ├── Invoices/
        │   ├── ListInvoices.cs            # + WithSummary/WithDescription + Produces 200/400
        │   ├── GetInvoiceById.cs          # + WithSummary/WithDescription + Produces 200/404
        │   ├── TransitionInvoice.cs       # + WithSummary/WithDescription + Produces 200/400/404
        │   ├── GetInvoiceStats.cs         # + WithSummary/WithDescription + Produces 200
        │   └── PayInvoice.cs              # (opcional) metadatos coherentes
        ├── Settings/                      # (opcional) metadatos coherentes
        └── Workers/                       # (opcional) metadatos coherentes

backend/Tests/Monolegal.Application.Tests/Documentation/
├── OpenApiDocumentTests.cs               # (nuevo) documento presente, operaciones y esquemas esperados
└── SwaggerUiTests.cs                     # (nuevo) /swagger responde en Development
```

**Structure Decision**: Web service por capas ya establecido en `backend/`. Esta feature añade exclusivamente código en la capa `Api` (paquete UI, wiring de middleware, metadatos de endpoints y esquema de seguridad), sin tocar `Domain`, `Application` ni `Infrastructure`, respetando la dirección de dependencias de la Arquitectura Limpia.

## Complexity Tracking

> Sin violaciones de la Constitución que requieran justificación. El único punto diferido (autenticación JWT efectiva) corresponde a una spec de seguridad independiente y no introduce complejidad adicional en esta feature.
