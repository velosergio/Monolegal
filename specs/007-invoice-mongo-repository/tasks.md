---
description: "Task list — Repositorio MongoDB de Facturas"
---

# Tasks: Repositorio MongoDB de Facturas

**Input**: Documentos de diseño en `/specs/007-invoice-mongo-repository/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/IInvoiceRepository.md, quickstart.md

**Tests**: INCLUIDOS Y OBLIGATORIOS. El Principio IV de la constitución exige tests de integración para contratos de repositorio (ciclo Red-Green-Refactor). El núcleo del trabajo de esta feature es precisamente esa suite de integración.

**Organization**: Tareas agrupadas por historia de usuario para implementación y verificación independientes.

## Contexto importante (estado del código)

La implementación productiva ya existe en el repositorio:
- `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs` — todas las operaciones del contrato
- `backend/Infrastructure/Persistence/MongoIndexBuilder.cs` — índices `Status_asc`, `ClientId_asc`, `LastStatusTransitionAt_asc`
- `backend/Infrastructure/Configuration/DependencyInjection.cs` — registro DI de `IInvoiceRepository → MongoInvoiceRepository`

Por tanto las tareas de "implementación" son tareas de **verificación contra la spec** (confirmar que el código satisface el contrato; ajustar solo si un test falla). El trabajo genuinamente nuevo son las suites de integración contra MongoDB real, hoy inexistentes (solo hay `InvoiceRepositoryContractTests` contra un fake en memoria).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede correr en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: Historia de usuario a la que pertenece (US1–US5)

## Path Conventions

Estructura de servicio web por capas. Código bajo `backend/`; tests bajo `backend/Tests/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Disponibilidad del entorno de integración

- [X] T001 Verificar servicio `mongo` de docker-compose en ejecución y variable `MONGODB_URI` definida según `quickstart.md` (prerrequisito de los tests de integración)
- [X] T002 [P] Confirmar que `backend/Tests/Tests.csproj` referencia `Infrastructure.csproj` (acceso a `MongoInvoiceRepository`/`MongoIndexBuilder`) y los paquetes `xunit` + `Shouldly` (ya presentes)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infraestructura de tests de integración compartida por todas las historias

**⚠️ CRITICAL**: Ninguna suite de integración (US1–US5) puede escribirse hasta completar esta fase

- [X] T003 Crear fixture de integración `MongoIntegrationFixture` en `backend/Tests/Infrastructure/Support/MongoIntegrationFixture.cs`: lee `MONGODB_URI` (o default dev), provisiona una **base de datos con nombre único** (sufijo GUID) para aislamiento, expone `IMongoDatabase`, una instancia de `MongoInvoiceRepository` apuntando a esa base, y elimina (drop) la base en `Dispose`/`DisposeAsync`. Marcar la clase con `[Trait("Category", "Integration")]`
- [X] T004 [P] Crear helper de datos `InvoiceTestFactory` en `backend/Tests/Infrastructure/Support/InvoiceTestFactory.cs` que construya `Invoice` válidas y permita fijar su `Status` vía `UpdateStatus(...)` para los escenarios de prueba

**Checkpoint**: Fixture y factory listos — las suites de integración por historia pueden empezar (en paralelo)

---

## Phase 3: User Story 1 - Consultar facturas por estado (Priority: P1) 🎯 MVP

**Goal**: `GetByStatusAsync(status)` devuelve exactamente las facturas en el estado dado (vacío si no hay)

**Independent Test**: Insertar facturas en estados mixtos y verificar que la consulta por un estado devuelve solo ese estado; consultar un estado sin coincidencias devuelve `[]`

### Tests for User Story 1 ⚠️ (escribir primero, deben FALLAR antes de verificar)

- [X] T005 [P] [US1] Suite de integración en `backend/Tests/Infrastructure/MongoInvoiceRepositoryStatusQueryTests.cs` usando `MongoIntegrationFixture`: (a) devuelve solo coincidencias de estado, (b) devuelve `[]` sin coincidencias, (c) devuelve múltiples cuando varias coinciden — cubre FR-001, FR-008, SC-001

### Implementation for User Story 1

- [X] T006 [US1] Verificar que `GetByStatusAsync` en `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs` satisface T005 (filtro `Find(x => x.Status == status)`); ajustar solo si algún caso falla

**Checkpoint**: US1 verificada de forma independiente contra MongoDB real

---

## Phase 4: User Story 2 - Consultar facturas por cliente (Priority: P1)

**Goal**: `GetByClientIdAsync(clientId)` devuelve exactamente las facturas del cliente dado (vacío si no hay)

**Independent Test**: Insertar facturas de varios clientes y verificar que la consulta por un `clientId` devuelve solo las de ese cliente

### Tests for User Story 2 ⚠️

- [X] T007 [P] [US2] Suite de integración en `backend/Tests/Infrastructure/MongoInvoiceRepositoryClientQueryTests.cs`: (a) devuelve solo facturas del `clientId`, (b) devuelve `[]` para cliente sin facturas — cubre FR-002, FR-008, SC-002

### Implementation for User Story 2

- [X] T008 [US2] Verificar que `GetByClientIdAsync` en `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs` satisface T007; ajustar solo si algún caso falla

**Checkpoint**: US1 y US2 verificadas de forma independiente

---

## Phase 5: User Story 3 - Cambiar el estado de una factura (Priority: P1)

**Goal**: `UpdateStatusAsync(id, newStatus)` actualiza solo `Status`/`UpdatedAt`/`LastStatusTransitionAt`; no-op si `id` no existe

**Independent Test**: Insertar una factura, cambiar su estado y verificar nuevo estado + marca temporal; un `id` inexistente no modifica documentos

### Tests for User Story 3 ⚠️

- [X] T009 [P] [US3] Suite de integración en `backend/Tests/Infrastructure/MongoInvoiceRepositoryStatusUpdateTests.cs`: (a) cambia el estado de la factura objetivo y actualiza `LastStatusTransitionAt`/`UpdatedAt`, (b) no altera otras facturas ni otros campos (`Amount`, `RemindersCount`), (c) no-op (0 documentos modificados) cuando el `id` no existe — cubre FR-003, FR-007, FR-009, SC-003, SC-004

### Implementation for User Story 3

- [X] T010 [US3] Verificar que `UpdateStatusAsync` en `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs` usa `UpdateOneAsync` con `Set` parcial (Status, UpdatedAt, LastStatusTransitionAt) y satisface T009; ajustar solo si algún caso falla

**Checkpoint**: US1–US3 verificadas de forma independiente

---

## Phase 6: User Story 4 - Crear una nueva factura (Priority: P1)

**Goal**: `AddAsync(invoice)` (semántica de inserción) persiste una factura recuperable después

**Independent Test**: Crear una factura y verificar que es recuperable por `Id`, por estado y por cliente

### Tests for User Story 4 ⚠️

- [X] T011 [P] [US4] Suite de integración en `backend/Tests/Infrastructure/MongoInvoiceRepositoryInsertTests.cs`: (a) tras `AddAsync`, la factura es recuperable por `GetByIdAsync`, (b) aparece en `GetByStatusAsync` y `GetByClientIdAsync` con sus valores — cubre FR-004, SC-007

### Implementation for User Story 4

- [X] T012 [US4] Verificar que `AddAsync` en `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs` usa `InsertOneAsync` y satisface T011; ajustar solo si algún caso falla

**Checkpoint**: Las 4 historias P1 (MVP funcional del repositorio) verificadas de forma independiente

---

## Phase 7: User Story 5 - Rendimiento de consultas frecuentes (Priority: P2)

**Goal**: Existen índices sobre `Status` y `ClientId`, creados de forma idempotente al arranque

**Independent Test**: Ejecutar `EnsureIndexesAsync` y verificar que la colección expone `Status_asc` y `ClientId_asc`; re-ejecutar no produce error

### Tests for User Story 5 ⚠️

- [X] T013 [P] [US5] Suite de integración en `backend/Tests/Infrastructure/MongoInvoiceRepositoryIndexTests.cs` usando `MongoIntegrationFixture`: (a) tras `MongoIndexBuilder.EnsureIndexesAsync`, `db.Invoices.getIndexes()` incluye `Status_asc` y `ClientId_asc`, (b) una segunda ejecución de `EnsureIndexesAsync` es idempotente (sin excepción, sin índices duplicados) — cubre FR-005, FR-006, FR-010, SC-006

### Implementation for User Story 5

- [X] T014 [US5] Verificar que `MongoIndexBuilder.EnsureIndexesAsync` en `backend/Infrastructure/Persistence/MongoIndexBuilder.cs` crea los índices `Status_asc`/`ClientId_asc` de forma idempotente y satisface T013; ajustar solo si algún caso falla

**Checkpoint**: Todas las historias (US1–US5) verificadas de forma independiente

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Validación integral y cierre

- [X] T015 Ejecutar la suite completa `cd backend && dotnet test` y confirmar que todas las categorías (unit, contrato, integración) están en verde, sin tests omitidos (`[Ignore]`/skip prohibidos)
- [X] T016 [P] Ejecutar las validaciones 1–4 de `quickstart.md` (contrato, integración, índices vía `mongosh`, suite completa) y confirmar resultados esperados
- [X] T017 [P] Documentar en `backend/README.md` (o ADR) el patrón de tests de integración del repositorio (base efímera por fixture vía `MONGODB_URI`, `[Trait("Category","Integration")]`)
- [X] T018 Marcar el checklist `specs/007-invoice-mongo-repository/checklists/requirements.md` y confirmar trazabilidad FR→tests

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias — puede empezar de inmediato
- **Foundational (Phase 2)**: depende de Setup — BLOQUEA todas las historias (fixture + factory)
- **User Stories (Phase 3–7)**: todas dependen de Phase 2; entre sí son independientes (archivos de test distintos)
- **Polish (Phase 8)**: depende de que las historias deseadas estén completas

### User Story Dependencies

- **US1 (P1)**, **US2 (P1)**, **US3 (P1)**, **US4 (P1)**, **US5 (P2)**: cada una arranca tras Phase 2; sin dependencias cruzadas (cada suite vive en su propio archivo y su propia base efímera)

### Within Each User Story

- El test de integración (T005/T007/T009/T011/T013) se escribe primero y debe FALLAR antes de la verificación de implementación
- La tarea de verificación confirma (o ajusta) el código existente para poner el test en verde

### Parallel Opportunities

- T002 (setup) en paralelo a T001
- T004 (factory) en paralelo a la finalización de T003 (archivos distintos; T003 es prerequisito de uso pero el archivo del factory no depende de él)
- Tras Phase 2: **T005, T007, T009, T011, T013 pueden escribirse en paralelo** (archivos de test distintos)
- En Polish: T016 y T017 en paralelo

---

## Parallel Example: Suites de integración (tras Phase 2)

```bash
# Lanzar en paralelo la escritura de las suites por historia (archivos distintos):
Task: "T005 [US1] MongoInvoiceRepositoryStatusQueryTests.cs"
Task: "T007 [US2] MongoInvoiceRepositoryClientQueryTests.cs"
Task: "T009 [US3] MongoInvoiceRepositoryStatusUpdateTests.cs"
Task: "T011 [US4] MongoInvoiceRepositoryInsertTests.cs"
Task: "T013 [US5] MongoInvoiceRepositoryIndexTests.cs"
```

---

## Implementation Strategy

### MVP First (Historias P1: US1–US4)

1. Completar Phase 1 (Setup) y Phase 2 (Foundational: fixture + factory)
2. Completar US1 → US4 (las cuatro operaciones del contrato sobre Mongo real)
3. **DETENERSE Y VALIDAR**: `dotnet test --filter "Category=Integration"` en verde
4. El repositorio queda completamente validado para crear, consultar (por estado y cliente) y transicionar estado

### Incremental Delivery

1. Setup + Foundational → base de integración lista
2. US1 (estado) → validar → el filtro central queda probado (MVP mínimo demostrable)
3. US2 (cliente), US3 (cambio de estado), US4 (creación) → añadir y validar cada una
4. US5 (índices/rendimiento) → validar cobertura de índices
5. Polish → suite completa + quickstart + docs

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes
- Toda suite de integración usa base de datos efímera por fixture → repetible, aislada, paralelizable
- Las tareas de "implementación" son verificaciones: el código productivo ya existe; solo se ajusta si un test lo demuestra necesario (evitar spec creep)
- Verificar que cada test falla antes de darlo por satisfecho (Red-Green-Refactor)
- Commit por tarea o grupo lógico; mensaje con referencia a la spec (ej: `test(spec-1.3): integración GetByStatusAsync`)
