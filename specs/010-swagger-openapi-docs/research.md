# Research — Documentación Swagger/OpenAPI (spec 010)

Fase 0 del plan. Resuelve las decisiones técnicas (sin NEEDS CLARIFICATION pendientes) para exponer la documentación interactiva en `/swagger`.

## Contexto de partida (estado actual del código)

- `backend/Api/Program.cs` ya registra `builder.Services.AddOpenApi()` y, bajo `app.Environment.IsDevelopment()`, llama a `app.MapOpenApi()` → el documento OpenAPI se sirve en `/openapi/v1.json` (solo Development).
- **No existe interfaz de usuario**: `Microsoft.AspNetCore.OpenApi` solo genera el documento; no renderiza UI ni ofrece "Try it out". Acceder a `/swagger` hoy devuelve 404.
- Los endpoints de facturas (`ListInvoices`, `GetInvoiceById`, `TransitionInvoice`, `GetInvoiceStats`) ya usan `.WithName(...)` y `.WithTags("Invoices")`, pero **no** declaran resumen, descripción ni respuestas (`Produces`), por lo que la documentación generada es incompleta en descripciones y códigos de estado.

## D1 — Cómo renderizar la UI en `/swagger`

**Decisión**: Añadir el paquete **`Swashbuckle.AspNetCore.SwaggerUI` 10.1.7** (solo la interfaz Swagger UI) y habilitar `app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Monolegal API v1"))`. La UI queda disponible en `/swagger` y consume el documento que ya genera `MapOpenApi`.

**Rationale**:
- En .NET 9/10 Swashbuckle dejó de ser dependencia por defecto; Microsoft separó la generación del documento (`Microsoft.AspNetCore.OpenApi`) del renderizado. El paquete `SwaggerUI` aporta únicamente los assets de la interfaz, sin reintroducir el generador completo de Swashbuckle.
- La ruta por defecto de `UseSwaggerUI` es `/swagger`, exactamente la pedida por el roadmap (Spec 2.2), e incluye "Try it out" nativo (FR-007/FR-008).
- Reutiliza el documento `/openapi/v1.json` ya existente: una única fuente de verdad, sin doc mantenido a mano (FR-009/FR-010).

**Alternativas consideradas**:
- **Scalar (`Scalar.AspNetCore`)**: UI moderna, pero su ruta por defecto es `/scalar` y el roadmap exige `/swagger` con terminología "Try it out" clásica de Swagger UI. Descartada por fidelidad al requisito.
- **ReDoc**: solo lectura, **sin "Try it out"** → incumple FR-007. Descartada.
- **Swashbuckle.AspNetCore completo**: reintroduciría un segundo generador de documento (`AddSwaggerGen`) redundante con el OpenAPI nativo. Descartada por duplicación y mayor superficie.

## D2 — Completitud de la documentación (descripciones, modelos y códigos de estado)

**Decisión**: Enriquecer los metadatos OpenAPI de cada endpoint existente con `.WithSummary(...)`, `.WithDescription(...)` y `.Produces<T>(StatusCodes...)` / `.ProducesValidationProblem()` para declarar explícitamente las respuestas `200`, `400` y `404` según corresponda a cada endpoint (alineado con los contratos de la spec 009).

**Rationale**:
- FR-003 exige descripción del propósito; FR-006 exige listar los códigos de estado posibles con su significado. La inferencia automática de .NET capta el tipo de respuesta `200` y los DTO, pero no los `400`/`404` ni las descripciones de propósito, que deben declararse.
- Los esquemas de los DTO (`InvoiceListItemDto`, `PagedResponse<T>`, `InvoiceDetailDto`, `TransitionRequest`, `InvoiceStatsDto`) se infieren automáticamente desde las firmas de los handlers y aparecen en la sección de esquemas (FR-005), sin trabajo manual adicional.
- El `JsonStringEnumConverter` con `LowerCaseNamingPolicy` (ya configurado) hace que `InvoiceStatus` aparezca como enum de cadenas en minúscula en los esquemas, coherente con el contrato real.

**Alternativas consideradas**:
- **No enriquecer (dejar solo `WithName`/`WithTags`)**: la documentación carecería de descripciones y de los códigos `400/404` → incumple FR-003 y FR-006. Descartada.
- **XML doc comments + `<GenerateDocumentationFile>`**: válido, pero más verboso y peor integrado con Minimal APIs que las extensiones fluidas `WithSummary/WithDescription/Produces`. Descartada por ergonomía.

## D3 — Exposición por entorno (desarrollo vs producción)

**Decisión**: Mantener tanto `MapOpenApi()` como `UseSwaggerUI()` **dentro del bloque `if (app.Environment.IsDevelopment())`** ya existente. La documentación NO se expone en producción por defecto.

**Rationale**:
- Guía de seguridad de Microsoft (limitar divulgación de información): las UIs de OpenAPI deben habilitarse solo en entornos de desarrollo.
- Coincide con el Assumption de la spec ("exposición en producción restringida/deshabilitada por defecto") y con el estado actual del código (`MapOpenApi` ya está bajo ese gate).

**Alternativas consideradas**:
- **Exponer en producción tras autenticación**: posible a futuro, pero excede el alcance de esta spec y depende de la spec de seguridad. Diferida.

## D4 — "Try it out" sobre endpoints protegidos (esquema de seguridad)

**Decisión**: Declarar un **esquema de seguridad Bearer (JWT)** en el documento OpenAPI mediante un *document transformer* registrado en `AddOpenApi`, de modo que Swagger UI muestre el botón "Authorize" y permita ejecutar "Try it out" contra endpoints protegidos (FR-011).

**Rationale**:
- La constitución exige JWT Admin-only; aunque la autenticación efectiva se implementa en otra spec, declarar el esquema ahora deja la documentación lista para autorizar pruebas y satisface FR-011 sin acoplarse a la lógica de auth.
- Es metadato puro del documento OpenAPI; no introduce middleware de autenticación ni cambia el comportamiento de los endpoints.

**Alternativas consideradas**:
- **Omitir el esquema de seguridad**: "Try it out" no podría autorizar endpoints protegidos cuando exista la auth → incumpliría FR-011. Descartada.

## D5 — Estrategia de pruebas

**Decisión**: Pruebas de integración con `WebApplicationFactory` forzando entorno `Development`:
1. `GET /openapi/v1.json` responde `200` y el documento contiene las rutas e identificadores de operación esperados (`ListInvoices`, `GetInvoiceById`, `TransitionInvoice`, `GetInvoiceStats`) y los esquemas de los DTO clave.
2. `GET /swagger` (o su `index.html`) responde con la UI (`200`).

**Rationale**: Verifica de forma falsable FR-001, FR-002 y FR-005 sin depender de un navegador. La verificación visual de "Try it out" (FR-007/FR-008) se cubre como paso manual en `quickstart.md`.

**Alternativas consideradas**:
- **Pruebas E2E con navegador (Playwright)**: aportarían cobertura de la interacción "Try it out", pero son costosas para una utilidad de documentación; se reservan para jornadas de usuario de negocio. Se documenta la verificación manual en quickstart.

## Resumen de decisiones

| ID | Decisión | Impacto |
|----|----------|---------|
| D1 | `Swashbuckle.AspNetCore.SwaggerUI` 10.1.7 sirviendo `/openapi/v1.json` en `/swagger` | +1 paquete, wiring en `Program.cs` |
| D2 | Enriquecer metadatos (`WithSummary`/`WithDescription`/`Produces`) por endpoint | Edición de los 4 endpoints de facturas |
| D3 | UI y documento solo en `Development` | Reutiliza gate existente |
| D4 | Declarar esquema de seguridad Bearer en el documento OpenAPI | Document transformer en `AddOpenApi` |
| D5 | Pruebas de integración del documento y la UI; verificación manual de "Try it out" | Nueva carpeta `Documentation/` en Tests |
