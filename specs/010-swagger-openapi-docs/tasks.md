---
description: "Task list — Documentación Swagger/OpenAPI (spec 010)"
---

# Tasks: Documentación Swagger/OpenAPI

**Input**: Documentos de diseño en `specs/010-swagger-openapi-docs/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: INCLUIDOS. La Constitución exige desarrollo Test-First (Principio IV) y el plan (research.md D5) define pruebas de integración del documento OpenAPI y de la UI. Las pruebas se escriben primero y deben FALLAR antes de implementar.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: Historia de usuario asociada (US1, US2, US3)
- Cada tarea incluye la ruta de archivo exacta

## Path Conventions

Backend ASP.NET Core por capas. Esta feature toca exclusivamente la capa `Api` (`backend/Api/`) y las pruebas (`backend/Tests/`).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Incorporar las dependencias y la configuración base para la documentación interactiva.

- [x] T001 Añadir `<PackageReference Include="Swashbuckle.AspNetCore.SwaggerUI" Version="10.1.7" />` al `ItemGroup` de paquetes en `backend/Api/Api.csproj` (research.md D1)
- [x] T002 [P] Añadir `<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.6" />` al `ItemGroup` de paquetes en `backend/Tests/Tests.csproj` para habilitar `WebApplicationFactory` en las pruebas de documentación (research.md D5)
- [x] T003 [P] Crear `backend/Api/Properties/launchSettings.json` con el perfil `https` en entorno `Development`, `launchBrowser: true` y `launchUrl: "swagger"` (research.md D1; contracts/swagger-ui.md)
- [x] T004 Restaurar paquetes y verificar compilación: ejecutar `dotnet restore` y `dotnet build` desde `backend/` (confirma que T001–T002 resuelven correctamente)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Habilitar el renderizado de la UI Swagger sobre el documento OpenAPI ya existente. Sin esto, `/swagger` devuelve 404 y ninguna historia es verificable.

**⚠️ CRITICAL**: Ninguna historia de usuario puede completarse hasta que esta fase esté lista.

- [x] T005 Habilitar `app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Monolegal API v1"))` dentro del bloque `if (app.Environment.IsDevelopment())` existente en `backend/Api/Program.cs` (junto a `MapOpenApi`), de modo que la UI se sirva en `/swagger` solo en Development (research.md D1, D3; contracts/swagger-ui.md)
- [x] T006 [P] Configurar `AddOpenApi` en `backend/Api/Program.cs` con metadatos del documento (`info.title = "Monolegal API"`, `info.version = "v1"`) para que el documento OpenAPI tenga título y versión deterministas (contracts/openapi-document.md, invariante 2)
- [x] T007 Crear el directorio de pruebas `backend/Tests/Monolegal.Application.Tests/Documentation/` y una clase base/fixture `DocumentationTestFactory` basada en `WebApplicationFactory<Program>` que fuerce `ASPNETCORE_ENVIRONMENT=Development`, en `backend/Tests/Monolegal.Application.Tests/Documentation/DocumentationTestFactory.cs` (research.md D5)

**Checkpoint**: La UI Swagger se sirve en `/swagger` en Development y existe la infraestructura de pruebas de integración.

---

## Phase 3: User Story 1 - Descubrir y explorar la API (Priority: P1) 🎯 MVP

**Goal**: La página `/swagger` carga y lista todos los endpoints (los 4 de facturas) con método, ruta y descripción.

**Independent Test**: Levantar el backend en Development, abrir `/swagger` y confirmar que carga sin error y lista las 4 operaciones de facturas con su método, ruta y descripción.

### Tests for User Story 1 (escribir primero, deben FALLAR) ⚠️

- [x] T008 [P] [US1] Crear `SwaggerUiTests` que verifique que `GET /swagger` (siguiendo redirección a `/swagger/index.html`) devuelve `200` y `text/html` en Development, en `backend/Tests/Monolegal.Application.Tests/Documentation/SwaggerUiTests.cs` (FR-001; contracts/swagger-ui.md)
- [x] T009 [P] [US1] Crear `OpenApiDocumentTests` que solicite `GET /openapi/v1.json`, parsee el JSON y verifique la presencia de las rutas `/api/invoices` (GET), `/api/invoices/{id}` (GET), `/api/invoices/transition/{id}` (POST) y `/api/invoices/stats` (GET) con sus `operationId`, en `backend/Tests/Monolegal.Application.Tests/Documentation/OpenApiDocumentTests.cs` (FR-002; contracts/openapi-document.md, invariante 3)

### Implementation for User Story 1

- [x] T010 [US1] Verificar/añadir `.WithName(...)` y `.WithTags("Invoices")` consistentes y un `.WithSummary(...)` breve en `backend/Api/Endpoints/Invoices/ListInvoices.cs` y `backend/Api/Endpoints/Invoices/GetInvoiceById.cs` para que cada operación tenga identificador, agrupación y resumen visibles en la lista (FR-002, FR-003)
- [x] T011 [US1] Verificar/añadir `.WithName(...)`, `.WithTags("Invoices")` y `.WithSummary(...)` en `backend/Api/Endpoints/Invoices/TransitionInvoice.cs` y `backend/Api/Endpoints/Invoices/GetInvoiceStats.cs` (FR-002, FR-003)
- [x] T012 [US1] Ejecutar `dotnet test --filter "FullyQualifiedName~Documentation"` desde `backend/` y confirmar que T008–T009 pasan (la página lista todos los endpoints)

**Checkpoint**: `/swagger` carga y lista todos los endpoints — MVP funcional y verificable de forma independiente.

---

## Phase 4: User Story 2 - Comprender modelos, DTO y códigos de estado (Priority: P1)

**Goal**: Cada operación muestra su descripción detallada, sus parámetros, los esquemas de los DTO de entrada/salida y los códigos de estado posibles (`200`/`400`/`404`).

**Independent Test**: En `/swagger`, revisar cada operación de facturas y la sección Schemas, confirmando que se muestran los DTO con sus campos y los códigos de estado declarados de cada operación.

### Tests for User Story 2 (escribir primero, deben FALLAR) ⚠️

- [x] T013 [P] [US2] Ampliar `OpenApiDocumentTests` para verificar que `components.schemas` contiene `InvoiceListItemDto`, `InvoiceDetailDto`, `TransitionRequest`, `InvoiceStatsDto` y el esquema de respuesta paginada, en `backend/Tests/Monolegal.Application.Tests/Documentation/OpenApiDocumentTests.cs` (FR-005; data-model.md §3; contracts/openapi-document.md, invariante 5)
- [x] T014 [P] [US2] Añadir test que verifique que cada operación declara los códigos de respuesta de su contrato (`GET /api/invoices`→200/400; `GET /api/invoices/{id}`→200/404; `POST .../transition/{id}`→200/400/404; `GET .../stats`→200), en `backend/Tests/Monolegal.Application.Tests/Documentation/OpenApiDocumentTests.cs` (FR-006; data-model.md §2)
- [x] T015 [P] [US2] Añadir test que verifique que el esquema del enum `InvoiceStatus` enumera los valores en minúscula (`pending`, `primerrecordatorio`, `segundorecordatorio`, `desactivado`, `pagado`), en `backend/Tests/Monolegal.Application.Tests/Documentation/OpenApiDocumentTests.cs` (contracts/openapi-document.md, invariante 7)

### Implementation for User Story 2

- [x] T016 [US2] Enriquecer `backend/Api/Endpoints/Invoices/ListInvoices.cs` con `.WithDescription(...)`, `.Produces<PagedResponse<InvoiceListItemDto>>(StatusCodes.Status200OK)` y `.ProducesValidationProblem()` (400) (FR-003, FR-004, FR-006; research.md D2)
- [x] T017 [P] [US2] Enriquecer `backend/Api/Endpoints/Invoices/GetInvoiceById.cs` con `.WithDescription(...)`, `.Produces<InvoiceDetailDto>(StatusCodes.Status200OK)` y `.Produces(StatusCodes.Status404NotFound)` (FR-003, FR-004, FR-006)
- [x] T018 [P] [US2] Enriquecer `backend/Api/Endpoints/Invoices/TransitionInvoice.cs` con `.WithDescription(...)`, `.Produces<InvoiceDetailDto>(200)`, `.ProducesValidationProblem()` (400) y `.Produces(404)` (FR-003, FR-004, FR-006)
- [x] T019 [P] [US2] Enriquecer `backend/Api/Endpoints/Invoices/GetInvoiceStats.cs` con `.WithDescription(...)` y `.Produces<InvoiceStatsDto>(StatusCodes.Status200OK)` (FR-003, FR-004, FR-006)
- [x] T020 [US2] Ejecutar `dotnet test --filter "FullyQualifiedName~Documentation"` desde `backend/` y confirmar que T013–T015 pasan (esquemas y códigos de estado presentes)

**Checkpoint**: La documentación muestra descripciones completas, esquemas de DTO y códigos de estado — US1 y US2 funcionan de forma independiente.

---

## Phase 5: User Story 3 - Probar los endpoints con "Try it out" (Priority: P2)

**Goal**: "Try it out" permite ejecutar peticiones reales desde la página, incluido el botón "Authorize" (Bearer) para endpoints protegidos.

**Independent Test**: En `/swagger`, activar "Try it out" en `GET /api/invoices/stats`, ejecutar y confirmar que se muestra el `200` y el cuerpo reales; confirmar que existe el botón "Authorize" (Bearer).

### Tests for User Story 3 (escribir primero, deben FALLAR) ⚠️

- [x] T021 [P] [US3] Añadir test que verifique que `components.securitySchemes` contiene un esquema `Bearer` (`type: http`, `scheme: bearer`, `bearerFormat: JWT`) en el documento OpenAPI, en `backend/Tests/Monolegal.Application.Tests/Documentation/OpenApiDocumentTests.cs` (FR-011; contracts/openapi-document.md, invariante 6; data-model.md §4)

### Implementation for User Story 3

- [x] T022 [US3] Crear un *document transformer* `BearerSecuritySchemeTransformer` que añada el esquema de seguridad `Bearer` (JWT) al documento OpenAPI, en `backend/Api/OpenApi/BearerSecuritySchemeTransformer.cs` (research.md D4; FR-011)
- [x] T023 [US3] Registrar el transformer en `AddOpenApi(options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>())` en `backend/Api/Program.cs` (research.md D4)
- [x] T024 [US3] Ejecutar `dotnet test --filter "FullyQualifiedName~Documentation"` desde `backend/` y confirmar que T021 pasa; realizar la verificación manual de "Try it out" según `quickstart.md` §4 (FR-007, FR-008)

**Checkpoint**: "Try it out" y "Authorize" operativos — las tres historias funcionan de forma independiente.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Coherencia, restricción por entorno y validación end-to-end.

- [x] T025 [P] (Opcional) Aplicar metadatos coherentes (`WithSummary`/`WithDescription`/`Produces`) a los endpoints no-factura ya registrados: `backend/Api/Endpoints/Invoices/PayInvoice.cs`, `backend/Api/Endpoints/Settings/GetInvoiceTransitions.cs`, `backend/Api/Endpoints/Settings/UpdateInvoiceTransitions.cs`, `backend/Api/Endpoints/Workers/TriggerTransitions.cs` (FR-002, FR-003)
- [x] T026 [P] Añadir test que verifique que en entorno `Production` tanto `GET /swagger` como `GET /openapi/v1.json` devuelven `404`, en `backend/Tests/Monolegal.Application.Tests/Documentation/EnvironmentExposureTests.cs` (research.md D3; contracts invariantes de disponibilidad)
- [x] T027 Ejecutar la validación completa de `quickstart.md` (pasos 1–6) contra el backend en ejecución y registrar resultados
- [x] T028 [P] Ejecutar la suite completa `dotnet test` desde `backend/` y confirmar 0 fallos; verificar que no se introducen regresiones en las pruebas de los endpoints de la spec 009

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sin dependencias — puede empezar de inmediato (T001–T004; T004 depende de T001–T002)
- **Foundational (Phase 2)**: Depende de Setup — BLOQUEA todas las historias (T005 depende de T001; T007 depende de T002)
- **User Stories (Phase 3–5)**: Dependen de Foundational
  - US1 (P1) → US2 (P1) comparten archivos de endpoints; ejecutar US2 tras US1 para evitar conflictos en los mismos archivos
  - US3 (P2) es independiente de los archivos de endpoints (toca `Program.cs` + transformer) y puede solaparse con US2 si lo realiza otra persona, salvo el registro en `Program.cs`
- **Polish (Phase 6)**: Depende de las historias deseadas completas

### User Story Dependencies

- **US1 (P1)**: Tras Foundational. Es el MVP.
- **US2 (P1)**: Tras Foundational. Comparte los 4 archivos de endpoints con US1 → secuenciar tras US1.
- **US3 (P2)**: Tras Foundational. Independiente de los endpoints; coordina el registro en `Program.cs` (T023) con T005/T006.

### Within Each User Story

- Tests primero (deben fallar) → implementación → verificación de tests en verde
- Coherencia: metadatos antes de respuestas tipadas; transformer antes de su registro

### Parallel Opportunities

- Setup: T002 y T003 en paralelo (distintos archivos); T001 independiente
- US1: T008 y T009 en paralelo (archivos de test distintos)
- US2: T013, T014, T015 en paralelo (asserts añadidos por personas coordinando el mismo archivo de test → preferible coordinar); T017, T018, T019 en paralelo (archivos de endpoint distintos); T016 toca ListInvoices
- US3: T021 (test) independiente; T022 (nuevo archivo) en paralelo con tests
- Polish: T025, T026, T028 en paralelo

---

## Parallel Example: User Story 2

```bash
# Implementación de endpoints en paralelo (archivos distintos):
Task: "Enriquecer GetInvoiceById.cs con Produces 200/404"
Task: "Enriquecer TransitionInvoice.cs con Produces 200/400/404"
Task: "Enriquecer GetInvoiceStats.cs con Produces 200"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1: Setup (T001–T004)
2. Completar Phase 2: Foundational (T005–T007) — CRÍTICO, desbloquea todo
3. Completar Phase 3: US1 (T008–T012)
4. **PARAR y VALIDAR**: `/swagger` carga y lista todos los endpoints
5. Demo del MVP

### Incremental Delivery

1. Setup + Foundational → la UI existe en `/swagger`
2. US1 → la página lista los endpoints → Demo (MVP)
3. US2 → descripciones, esquemas y códigos de estado → Demo
4. US3 → "Try it out" + Authorize → Demo
5. Polish → coherencia, gate de producción y validación end-to-end

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes
- US1 y US2 comparten los 4 archivos de endpoints → secuenciar (no marcar [P] entre fases sobre el mismo archivo)
- La documentación está restringida a `Development` (research.md D3); validar el gate en T026
- Verificar que los tests fallan antes de implementar (Test-First, Constitución IV)
- Confirmar tras T028 que no hay regresiones en las pruebas de la spec 009
