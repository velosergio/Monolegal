---
description: "Lista de tareas para Endpoints API de Facturas"
---

# Tasks: Endpoints API de Facturas

**Input**: Documentos de diseño en `specs/009-invoice-api-endpoints/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: OBLIGATORIOS. La Constitución (Principio IV — Test-First, NO NEGOCIABLE) exige tests escritos antes de la implementación, con ciclo Red-Green-Refactor y cobertura ≥85%.

**Organization**: Tareas agrupadas por historia de usuario para implementación y prueba independiente.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias).
- **[Story]**: Historia de usuario asociada (US1–US4).
- Cada tarea incluye la ruta de archivo exacta.

## Path Conventions

Proyecto web por capas en `backend/`: `Domain/`, `Application/`, `Infrastructure/`, `Api/`, `Tests/`.

---

## Phase 1: Setup (Infraestructura compartida)

**Purpose**: Preparación mínima (el proyecto ya existe).

- [x] T001 Crear la carpeta `backend/Application/Validation/` para los validadores FluentValidation y verificar la referencia a FluentValidation en `backend/Application/Application.csproj`

---

## Phase 2: Foundational (Prerrequisitos bloqueantes)

**Purpose**: Componentes compartidos por todos los endpoints (mapeo de estado, DTOs, serialización JSON, fake de tests).

**⚠️ CRITICAL**: Ninguna historia de usuario puede comenzar hasta completar esta fase.

- [x] T002 [P] Crear `InvoiceStatusApi` (mapeo `InvoiceStatus` ↔ cadena en minúscula, parseo case-insensitive y conjunto de estados válidos `pending`/`primerrecordatorio`/`segundorecordatorio`/`desactivado`/`pagado`) en `backend/Api/Endpoints/Invoices/InvoiceStatusApi.cs` (research.md D1)
- [x] T003 [P] Crear los DTOs de transporte (`InvoiceListItemDto`, `PagedResponse<T>`, `InvoiceDetailDto`, `TransitionRequest`, `InvoiceStatsDto`) en `backend/Api/Endpoints/Invoices/InvoiceDtos.cs` (data-model.md)
- [x] T004 Configurar `JsonStringEnumConverter` global con política a minúsculas vía `ConfigureHttpJsonOptions` en `backend/Api/Program.cs` (research.md D1)
- [x] T005 [P] Crear el fake compartido `InMemoryInvoiceRepository` (implementa la interfaz actual de `IInvoiceRepository`) en `backend/Tests/Infrastructure/Support/InMemoryInvoiceRepository.cs` y refactorizar `backend/Tests/Monolegal.Application.Tests/Endpoints/PayInvoiceTests.cs` para reutilizarlo
- [x] T006 [US-N/A] Test unitario del mapeo de `InvoiceStatusApi` (enum↔cadena en ambos sentidos y rechazo de cadenas inválidas) en `backend/Tests/Monolegal.Application.Tests/Endpoints/InvoiceStatusApiTests.cs` (depende de T002)

**Checkpoint**: Fundación lista — las historias de usuario pueden comenzar.

---

## Phase 3: User Story 1 — Listar facturas con filtro y paginación (Priority: P1) 🎯 MVP

**Goal**: `GET /api/invoices` devuelve una página de facturas (filtrable por estado), con `total` y `pageSize`, ordenada por `createdAt` desc.

**Independent Test**: Insertar facturas en estados mixtos y verificar página, total filtrado, orden desc, defaults, rechazo de parámetros inválidos y tope `pageSize=50`.

### Tests for User Story 1 ⚠️ (escribir primero, deben FALLAR)

- [x] T007 [US1] Tests del listado en `backend/Tests/Monolegal.Application.Tests/Endpoints/ListInvoicesTests.cs`: filtro por estado, paginación (página fuera de rango → `data` vacío + `total` real), orden `createdAt` desc, defaults (`page=1`/`pageSize=10`), inválidos → `400`, `pageSize>50` → `400`

### Implementation for User Story 1

- [x] T008 [US1] Añadir `GetPagedAsync(InvoiceStatus? status, int page, int pageSize, CancellationToken)` a `backend/Domain/Repositories/IInvoiceRepository.cs` y actualizar el fake compartido `InMemoryInvoiceRepository` (orden `CreatedAt` desc en memoria)
- [x] T009 [US1] Implementar `GetPagedAsync` en `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs` (filtro opcional por `Status`, orden `CreatedAt` desc, `Skip/Limit`, `Total` = `CountDocumentsAsync(filtro)`)
- [x] T010 [P] [US1] Crear `ListInvoicesQueryValidator` (FluentValidation: `page≥1`, `1≤pageSize≤50`, `status` válido si presente; defaults solo en ausencia) en `backend/Application/Validation/ListInvoicesQueryValidator.cs`
- [x] T011 [US1] Implementar el endpoint `GET /api/invoices` (`MapListInvoices`) con validación, mapeo a `PagedResponse<InvoiceListItemDto>` y logging Serilog en `backend/Api/Endpoints/Invoices/ListInvoices.cs`
- [x] T012 [US1] Registrar `app.MapListInvoices()` en `backend/Api/Program.cs`

**Checkpoint**: US1 funcional y testeable de forma independiente.

---

## Phase 4: User Story 2 — Consultar el detalle de una factura (Priority: P1)

**Goal**: `GET /api/invoices/{id}` devuelve el objeto completo (`200`) o `404` si no existe / id inválido.

**Independent Test**: Insertar una factura y verificar `200` con objeto completo; id inexistente o con formato inválido → `404`.

### Tests for User Story 2 ⚠️ (escribir primero, deben FALLAR)

- [x] T013 [US2] Tests del detalle en `backend/Tests/Monolegal.Application.Tests/Endpoints/GetInvoiceByIdTests.cs`: existente → `200` (objeto completo), inexistente → `404`, id con formato inválido → `404`

### Implementation for User Story 2

- [x] T014 [US2] Implementar el endpoint `GET /api/invoices/{id}` (`MapGetInvoiceById`) usando `GetByIdAsync`, mapeo a `InvoiceDetailDto` y logging en `backend/Api/Endpoints/Invoices/GetInvoiceById.cs`
- [x] T015 [US2] Registrar `app.MapGetInvoiceById()` en `backend/Api/Program.cs`

**Checkpoint**: US1 y US2 funcionan de forma independiente.

---

## Phase 5: User Story 3 — Transicionar el estado de una factura (Priority: P1)

**Goal**: `POST /api/invoices/transition/{id}` aplica una transición válida (`200`, persistida), rechaza la no permitida (`400`, sin cambios), `newStatus` inválido (`400`) e id inexistente/ inválido (`404`).

**Independent Test**: Probar transición permitida (persiste), no permitida (sin cambios → `400`), inexistente (`404`) y `newStatus` inválido (`400`).

### Tests for User Story 3 ⚠️ (escribir primero, deben FALLAR)

- [x] T016 [P] [US3] Tests de dominio de `ApplyManualTransition` (matriz de transiciones permitidas/ rechazadas) en `backend/Tests/Monolegal.Domain.Tests/Entities/InvoiceTransitionServiceManualTests.cs`
- [x] T017 [US3] Tests del endpoint de transición en `backend/Tests/Monolegal.Application.Tests/Endpoints/TransitionInvoiceTests.cs`: permitida → `200` (persiste), no permitida → `400` (sin cambios), inexistente → `404`, `newStatus` inválido/ ausente → `400`

### Implementation for User Story 3

- [x] T018 [US3] Añadir `ApplyManualTransition(Invoice, InvoiceStatus)` a `backend/Domain/Services/InvoiceTransitionService.cs` con la matriz de transiciones (research.md D4; delega en `ApplyPayment` para `Pagado`; lanza `InvalidOperationException` en transición no permitida)
- [x] T019 [P] [US3] Crear `TransitionInvoiceRequestValidator` (FluentValidation: `newStatus` requerido y válido) en `backend/Application/Validation/TransitionInvoiceRequestValidator.cs`
- [x] T020 [US3] Implementar el endpoint `POST /api/invoices/transition/{id}` (`MapTransitionInvoice`) con `GetByIdAsync` → `404`, validación → `400`, `ApplyManualTransition` + `UpdateAsync` → `200`, captura de `InvalidOperationException` → `400`, y logging en `backend/Api/Endpoints/Invoices/TransitionInvoice.cs`
- [x] T021 [US3] Registrar `app.MapTransitionInvoice()` en `backend/Api/Program.cs`

**Checkpoint**: US1, US2 y US3 funcionan de forma independiente.

---

## Phase 6: User Story 4 — Obtener estadísticas para el dashboard (Priority: P2)

**Goal**: `GET /api/invoices/stats` devuelve `totalInvoices`, `byStatus` y `byClient`, con la invariante `Σ(byStatus) == totalInvoices`.

**Independent Test**: Insertar facturas conocidas de varios estados/clientes y verificar los agregados; sin facturas → `200` con `0` y mapas vacíos.

### Tests for User Story 4 ⚠️ (escribir primero, deben FALLAR)

- [x] T022 [US4] Tests de estadísticas en `backend/Tests/Monolegal.Application.Tests/Endpoints/GetInvoiceStatsTests.cs`: `total`, `byStatus`, `byClient`, invariante `Σ(byStatus)==total`, y caso sin facturas → `total=0` con mapas vacíos

### Implementation for User Story 4

- [x] T023 [US4] Añadir `CountByStatusAsync` y `CountByClientAsync` a `backend/Domain/Repositories/IInvoiceRepository.cs` y actualizar el fake compartido `InMemoryInvoiceRepository`
- [x] T024 [US4] Implementar `CountByStatusAsync`/`CountByClientAsync` con agregación `$group` en `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs` (research.md D3)
- [x] T025 [US4] Implementar el endpoint `GET /api/invoices/stats` (`MapGetInvoiceStats`) componiendo `InvoiceStatsDto` (usando `CountAsync` para el total) y logging en `backend/Api/Endpoints/Invoices/GetInvoiceStats.cs`
- [x] T026 [US4] Registrar `app.MapGetInvoiceStats()` en `backend/Api/Program.cs`

**Checkpoint**: las cuatro historias funcionan de forma independiente.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [x] T027 [P] Verificar/ajustar `backend/Tests/Monolegal.Application.Tests/Endpoints/PayInvoiceTests.cs` ante el cambio de serialización de `status` a cadena (research.md D1)
- [x] T028 [P] Tests de integración Mongo (opcionales) para `GetPagedAsync` y `CountBy*Async` con `MongoIntegrationFixture` en `backend/Tests/Infrastructure/`
- [x] T029 Ejecutar `dotnet test` en `backend/` y confirmar todas las suites en verde con cobertura ≥85% — ✅ 164 tests verdes (106 Tests.csproj + 58 Domain.Tests), 0 fallos. Tests de integración Mongo (Category=Integration) se ejecutan en CI con Mongo.
- [~] T030 `dotnet format` verificado sobre los archivos nuevos (cumplen estilo; única advertencia en `UpdateInvoiceTransitions.cs` preexistente, fuera de alcance). **Pendiente**: validación manual de `quickstart.md` con servidor en vivo + MongoDB (no disponible en este entorno; ejecutar en entorno de desarrollo).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias.
- **Foundational (Phase 2)**: depende de Setup. BLOQUEA todas las historias.
- **User Stories (Phase 3–6)**: dependen de Foundational. US1–US3 son P1; US4 es P2.
- **Polish (Phase 7)**: depende de las historias deseadas completadas.

### User Story Dependencies

- **US1, US2, US3 (P1)**: independientes entre sí tras Foundational. US2 y US3 usan métodos de repositorio ya existentes (`GetByIdAsync`, `UpdateAsync`); US1 añade `GetPagedAsync`; US4 añade `CountBy*Async`.
- **US4 (P2)**: independiente; añade sus propios métodos de agregación.

### Archivo compartido (serializa tareas)

- `backend/Api/Program.cs` lo tocan T004, T012, T015, T021, T026 → **no** ejecutar en paralelo entre sí.
- `backend/Domain/Repositories/IInvoiceRepository.cs` lo tocan T008 y T023 → secuenciar.
- `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs` lo tocan T009 y T024 → secuenciar.

### Within Each User Story

- Tests primero (deben fallar) → dominio/repositorio → validador → endpoint → registro.

### Parallel Opportunities

- Foundational: T002, T003 y T005 en paralelo (archivos distintos).
- US1: T010 (validador) en paralelo con T008/T009 (repositorio).
- US3: T016 (test dominio) y T019 (validador) en paralelo con el resto.
- Polish: T027 y T028 en paralelo.

---

## Parallel Example: Foundational (Phase 2)

```bash
# Lanzar en paralelo (archivos distintos):
Task: "Crear InvoiceStatusApi en backend/Api/Endpoints/Invoices/InvoiceStatusApi.cs"
Task: "Crear DTOs en backend/Api/Endpoints/Invoices/InvoiceDtos.cs"
Task: "Crear fake InMemoryInvoiceRepository en backend/Tests/Infrastructure/Support/"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 (Setup) → Phase 2 (Foundational).
2. Phase 3 (US1 — listado).
3. **STOP & VALIDATE**: probar `GET /api/invoices` de forma independiente.

### Incremental Delivery

1. Foundation lista.
2. US1 (listado) → validar → demo (MVP).
3. US2 (detalle) → validar.
4. US3 (transición) → validar.
5. US4 (estadísticas) → validar.
6. Polish (cobertura, quickstart, formato).

---

## Notes

- [P] = archivos distintos sin dependencias.
- Verificar que los tests fallan antes de implementar (Red-Green-Refactor, Principio IV).
- Confirmar cobertura ≥85% antes de cerrar (CI gate).
- Commit por tarea o grupo lógico, referenciando la spec (ej. `feat(spec-2.1): ...`).
- Evitar romper la independencia entre historias y los conflictos en `Program.cs`.
