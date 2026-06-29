---
description: "Task list for feature implementation"
---

# Tasks: Tests de Integración de la API

**Input**: Design documents from `/specs/021-integration-tests-api/`

**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/api-test-matrix.md, quickstart.md

**Tests**: Esta feature **es** una suite de pruebas de integración (Principio IV). Por tanto, las tareas de implementación consisten en escribir los tests de integración HTTP; no hay código de producción que añadir.

**Organization**: Las tareas se agrupan por historia de usuario (US1–US4) para permitir implementación y verificación independientes. Cada historia se materializa en su propio archivo de test para habilitar paralelismo.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivo distinto, sin dependencias pendientes)
- **[Story]**: Historia de usuario a la que pertenece (US1–US4)
- Rutas de archivo exactas incluidas en cada descripción

## Path Conventions

- Proyecto de tests backend: `backend/Tests/Tests.csproj`
- Dobles compartidos: `backend/Tests/Infrastructure/Support/`
- Nuevas clases de test: `backend/Tests/Infrastructure/Api/`
- Endpoints bajo prueba (sin cambios): `backend/Api/Endpoints/Invoices/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Preparar la estructura de la suite sin tocar código de producción

- [X] T001 Crear la carpeta `backend/Tests/Infrastructure/Api/` y verificar que `backend/Tests/Tests.csproj` ya referencia `Microsoft.AspNetCore.Mvc.Testing` y `../Api/Api.csproj` (no requiere cambios; confirmar con `dotnet build backend/Tests/Tests.csproj`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infraestructura de pruebas reutilizable que TODAS las historias consumen (parte de US4 / FR-001, FR-014, FR-015)

**⚠️ CRITICAL**: Ninguna historia puede implementarse hasta completar esta fase

- [X] T002 Crear la fábrica de aplicación de pruebas compartida `InvoiceApiFactory : WebApplicationFactory<Program>` en `backend/Tests/Infrastructure/Support/InvoiceApiFactory.cs`: en `ConfigureWebHost` fijar `UseEnvironment("Development")`, `UseSetting("MONGODB_URI", "mongodb://localhost:27017/invoice_api_tests")`, y en `ConfigureServices` `RemoveAll<IHostedService>()` y reemplazar `ISystemSettingsRepository`, `IInvoiceRepository`, `IClientRepository`, `IClientEmailResolver` e `IInvoiceTransitionNotifier` por los dobles de `Support/` (`InMemorySystemSettingsRepository`, `InMemoryInvoiceRepository`, `InMemoryClientRepository`, `FakeClientEmailResolver`, `FakeTransitionNotifier`); exponer las propiedades `Invoices`/`Clients` y los helpers `SeedClient(Client)`/`SeedInvoice(Invoice)` (patrón de `InvoiceCrudEndpointsTests`)

**Checkpoint**: Fábrica lista — las historias US1–US4 pueden implementarse en paralelo (archivos distintos)

---

## Phase 3: User Story 1 - Contrato de listado de facturas (Priority: P1) 🎯 MVP

**Goal**: Verificar de extremo a extremo `GET /api/invoices`: 200 + estructura, filtro por estado y rechazo de parámetros inválidos.

**Independent Test**: Sembrar facturas en estados mixtos y comprobar 200 con `data`/`total`/`pageSize`, filtro por estado y 400 ante parámetros inválidos, vía `HttpClient` real.

- [X] T003 [US1] Crear la clase `ListInvoicesApiTests` (con `[Trait("Category","Application")]`) en `backend/Tests/Infrastructure/Api/ListInvoicesApiTests.cs` usando `InvoiceApiFactory`; test: `GET /api/invoices` con N facturas → 200 y cuerpo con `data`, `total`, `pageSize`; y con 25 facturas y `?page=1&pageSize=10` → `data.length`==10 y `total`==25 (matriz #1, #3 — FR-003)
- [X] T004 [US1] Añadir a `backend/Tests/Infrastructure/Api/ListInvoicesApiTests.cs` el test de filtro `GET /api/invoices?status=primerrecordatorio` → 200 con sólo facturas de ese estado y `total` coherente (matriz #2 — FR-004)
- [X] T005 [US1] Añadir a `backend/Tests/Infrastructure/Api/ListInvoicesApiTests.cs` los tests de parámetros inválidos → 400: `?status=foo`, `?page=0`, `?pageSize=51` (matriz #4, #5, #6 — FR-005)
- [X] T006 [US1] Añadir a `backend/Tests/Infrastructure/Api/ListInvoicesApiTests.cs` el test de página fuera de rango `?page=99&pageSize=10` → 200 con `data` vacío y `total` real (matriz #17 — FR-003)

**Checkpoint**: El listado queda cubierto de extremo a extremo; US1 verificable de forma independiente

---

## Phase 4: User Story 2 - Respuestas 404 por identificador (Priority: P1)

**Goal**: Verificar `GET /api/invoices/{id}` (200/404) y el 404 del endpoint de transición ante id inexistente o con formato inválido, sin errores no controlados.

**Independent Test**: Con base vacía, solicitar detalle/transición sobre ids inexistente y de formato inválido y comprobar 404 uniforme; con factura sembrada, detalle → 200 objeto completo.

- [X] T007 [P] [US2] Crear la clase `InvoiceNotFoundApiTests` (con `[Trait("Category","Application")]`) en `backend/Tests/Infrastructure/Api/InvoiceNotFoundApiTests.cs` usando `InvoiceApiFactory`; tests: detalle de factura sembrada → 200 con objeto completo (`id`, `status`, `amount`, `items`); y `GET /api/invoices/no-existe` → 404 (matriz #7, #8 — FR-006)
- [X] T008 [US2] Añadir a `backend/Tests/Infrastructure/Api/InvoiceNotFoundApiTests.cs` el test de detalle con id de formato inválido → 404 uniforme (sin 500) (matriz #9 — FR-007)
- [X] T009 [US2] Añadir a `backend/Tests/Infrastructure/Api/InvoiceNotFoundApiTests.cs` el test `POST /api/invoices/transition/no-existe` con cuerpo válido → 404 (matriz #14 — FR-011)

**Checkpoint**: Manejo de "no encontrado" cubierto en detalle y transición; US2 verificable de forma independiente

---

## Phase 5: User Story 3 - Validación de transiciones (Priority: P1)

**Goal**: Verificar `POST /api/invoices/transition/{id}`: transición permitida persiste (200), prohibida rechaza (400) sin cambios, y cuerpo inválido rechaza (400).

**Independent Test**: Sembrar factura en estado conocido; ejercitar transición permitida (200 + estado persistido), prohibida (400 + sin cambios) y cuerpo inválido (400) vía HTTP.

- [X] T010 [P] [US3] Crear la clase `TransitionInvoiceApiTests` (con `[Trait("Category","Application")]`) en `backend/Tests/Infrastructure/Api/TransitionInvoiceApiTests.cs` usando `InvoiceApiFactory`; test: factura `primerrecordatorio` + body `{ "newStatus": "segundorecordatorio" }` → 200 con `status` actualizado y persistido (verificar releyendo vía `GET /api/invoices/{id}` o `factory.Invoices`) (matriz #10 — FR-008)
- [X] T011 [US3] Añadir a `backend/Tests/Infrastructure/Api/TransitionInvoiceApiTests.cs` el test de transición prohibida: factura `pending` + body `{ "newStatus": "desactivado" }` → 400 y estado sin cambios (matriz #11 — FR-009)
- [X] T012 [US3] Añadir a `backend/Tests/Infrastructure/Api/TransitionInvoiceApiTests.cs` los tests de cuerpo inválido → 400: `newStatus` inexistente (`"foo"`) y `newStatus` ausente (cuerpo `{}`) (matriz #12, #13 — FR-010)

**Checkpoint**: Reglas de transición cubiertas vía HTTP; US3 verificable de forma independiente

---

## Phase 6: User Story 4 - Infraestructura reutilizable y agregados (Priority: P2)

**Goal**: Demostrar el aislamiento/determinismo de la suite y cubrir `GET /api/invoices/stats` incluyendo base vacía y el invariante Σ(byStatus)==total.

**Independent Test**: Ejecutar stats con base vacía (200, ceros) y con varias facturas (invariante de suma); verificar que dos instancias de fábrica no comparten datos.

- [X] T013 [P] [US4] Crear la clase `InvoiceStatsApiTests` (con `[Trait("Category","Application")]`) en `backend/Tests/Infrastructure/Api/InvoiceStatsApiTests.cs` usando `InvoiceApiFactory`; test: `GET /api/invoices/stats` con base vacía → 200 con `totalInvoices`==0 y `byStatus`/`byClient` vacíos (no error) (matriz #15 — FR-002)
- [X] T014 [US4] Añadir a `backend/Tests/Infrastructure/Api/InvoiceStatsApiTests.cs` el test: con facturas en varios estados, `GET /api/invoices/stats` → 200 y Σ(`byStatus`)==`totalInvoices` (matriz #16 — FR-002)
- [X] T015 [US4] Crear la clase `ApiTestIsolationTests` en `backend/Tests/Infrastructure/Api/ApiTestIsolationTests.cs`; test que construye dos instancias de `InvoiceApiFactory`, siembra datos sólo en una y verifica que la otra no los observa (datos aislados, sin dependencia del orden) (matriz #18 — FR-012, FR-013)

**Checkpoint**: Todas las historias funcionan de forma independiente; infraestructura de pruebas validada

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Verificación final de calidad y determinismo

- [X] T016 [P] Ejecutar la validación de `quickstart.md`: `dotnet test backend/Tests/Tests.csproj --filter "FullyQualifiedName~Infrastructure.Api"` dos veces seguidas y confirmar resultado idéntico (cero fallos, cero omitidos) (SC-004, SC-005)
- [X] T017 [P] Revisar las nuevas clases en `backend/Tests/Infrastructure/Api/`: cero `Skip`/`[Ignore]`, `[Trait("Category","Application")]` consistente, y ejecutar `dotnet format backend/Tests/Tests.csproj --verify-no-changes` (Principio IV / FR-017)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sin dependencias — puede empezar de inmediato
- **Foundational (Phase 2)**: Depende de Setup — **BLOQUEA** todas las historias (T002 es prerequisito de US1–US4)
- **User Stories (Phase 3–6)**: Todas dependen de T002. Una vez completado, pueden ejecutarse en paralelo (archivos distintos)
- **Polish (Phase 7)**: Depende de que las historias deseadas estén completas

### User Story Dependencies

- **US1 (P1)**: Sólo depende de T002 — independiente
- **US2 (P1)**: Sólo depende de T002 — independiente
- **US3 (P1)**: Sólo depende de T002 — independiente
- **US4 (P2)**: Sólo depende de T002 — independiente

### Within Each User Story

- La primera tarea de cada historia crea la clase de test (incluye el caso principal); las siguientes añaden casos al **mismo archivo**, por lo que son secuenciales entre sí (no `[P]` dentro de una misma historia).

### Parallel Opportunities

- Tras completar T002, las tareas iniciales de cada historia pueden lanzarse en paralelo (archivos distintos): **T003 (US1)**, **T007 (US2)**, **T010 (US3)**, **T013 (US4)**.
- En Polish, **T016** y **T017** son paralelas.

---

## Parallel Example: tras la Foundational (T002)

```bash
# Lanzar la primera tarea de cada historia en paralelo (archivos distintos):
Task: "T003 [US1] ListInvoicesApiTests.cs — listado 200 + paginación"
Task: "T007 [US2] InvoiceNotFoundApiTests.cs — detalle 200/404"
Task: "T010 [US3] TransitionInvoiceApiTests.cs — transición permitida 200"
Task: "T013 [US4] InvoiceStatsApiTests.cs — stats base vacía"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1 (Setup) y Phase 2 (Foundational: T002 — fábrica compartida)
2. Completar Phase 3 (US1 — listado)
3. **DETENER y VALIDAR**: ejecutar `dotnet test --filter "FullyQualifiedName~ListInvoicesApiTests"` y confirmar verde
4. Demo del contrato de listado cubierto de extremo a extremo

### Incremental Delivery

1. Setup + Foundational → fábrica lista
2. US1 (listado) → validar → MVP
3. US2 (404) → validar
4. US3 (transición) → validar
5. US4 (stats + aislamiento) → validar
6. Polish (determinismo + formato)

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes
- La etiqueta [Story] mapea cada tarea a su historia para trazabilidad (ver `contracts/api-test-matrix.md`)
- No se modifica código de producción: sólo se añaden tests
- No se requiere MongoDB: la suite usa dobles en memoria (research.md D2)
- Sin `Skip`/`[Ignore]` (Principio IV); CI falla si cualquier test falla
- Commit tras cada tarea o grupo lógico
