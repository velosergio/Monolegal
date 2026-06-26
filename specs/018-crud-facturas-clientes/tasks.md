---
description: "Lista de tareas para la implementación de CRUD de Facturas y Clientes"
---

# Tasks: CRUD de Facturas y Clientes

**Input**: Documentos de diseño en `specs/018-crud-facturas-clientes/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUIDOS y obligatorios — la Constitución Monolegal exige Desarrollo Test-First (Principio IV, NO NEGOCIABLE), cobertura ≥85%. Cada historia escribe sus tests primero (Red), luego implementa (Green) y refactoriza.

**Organización**: Tareas agrupadas por historia de usuario para implementación y prueba independientes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (distinto archivo, sin dependencias pendientes)
- **[Story]**: Historia de usuario a la que pertenece (US1, US2)
- Cada tarea incluye la ruta exacta del archivo

## Path Conventions

- Backend: `backend/{Domain,Application,Infrastructure,Api,Tests}`
- Frontend: `frontend/src/features/{invoices,clients}`

---

## Phase 1: Setup (Infraestructura compartida)

**Purpose**: Preparar la estructura de directorios del feature (las dependencias del stack ya están instaladas).

- [X] T001 [P] Crear el directorio de endpoints `backend/Api/Endpoints/Clients/` y la estructura del feature frontend `frontend/src/features/clients/api/` y `frontend/src/features/clients/components/`
- [X] T002 [P] Verificar que los proyectos de test (`backend/Tests`, colocaciones Vitest en `frontend/src`) referencian los ensamblados/utilidades necesarios para las nuevas pruebas de contrato e integración

---

## Phase 2: Foundational (Prerrequisitos bloqueantes)

**Purpose**: Modelo de dominio, repositorios, índices, migración, resolver y seeder que AMBAS historias necesitan.

**⚠️ CRITICAL**: Ninguna historia (US1/US2) puede comenzar hasta completar esta fase.

### Tests (escribir primero, deben FALLAR)

- [X] T003 [P] Tests unitarios de dominio: derivación del monto (`Amount = Σ subtotales`), validez de `InvoiceItem` (cantidad/precio > 0) y bloqueo de edición en estado terminal en `backend/Tests/Monolegal.Domain.Tests/InvoiceItemsTests.cs`
- [X] T004 [P] Tests de contrato de repositorio: `IClientRepository` (alta/edición/borrado/paginación/búsqueda/unicidad email) y `IInvoiceRepository.DeleteAsync`/`CountByClientIdAsync` en `backend/Tests/Infrastructure/ClientRepositoryContractTests.cs` y `backend/Tests/Infrastructure/InvoiceRepositoryContractTests.cs`
- [X] T005 [P] Test de idempotencia de la migración de backfill de items/dueDate en `backend/Tests/Infrastructure/InvoiceItemsBackfillMigrationTests.cs`

### Dominio

- [X] T006 [P] Crear el value object `InvoiceItem` (Description, Quantity, UnitPrice, Subtotal derivado, inmutable) en `backend/Domain/Entities/InvoiceItem.cs`
- [X] T007 Ampliar la entidad `Invoice` con `Items` (lista embebida), `DueDate`, `Amount` derivado de los items, y métodos de edición que bloqueen en estado terminal (`Pagado`/`Desactivado`) en `backend/Domain/Entities/Invoice.cs` (depende de T006)
- [X] T008 [P] Crear la entidad `Client` (Id, Name, Email, Phone?, Address?, CreatedAt, UpdatedAt; email normalizado) en `backend/Domain/Entities/Client.cs`

### Contratos de repositorio

- [X] T009 Ampliar `IInvoiceRepository` con `DeleteAsync(id)` y `CountByClientIdAsync(clientId)` en `backend/Domain/Repositories/IInvoiceRepository.cs`
- [X] T010 [P] Crear `IClientRepository` (GetById, GetByEmail, GetPagedAsync, Add, Update, Delete, Count, DeleteAll) en `backend/Domain/Repositories/IClientRepository.cs`

### Infraestructura

- [X] T011 Implementar `MongoClientRepository` (colección `Clients`, búsqueda regex escapada por Name/Email, orden por Name) en `backend/Infrastructure/Repositories/MongoClientRepository.cs` (depende de T010)
- [X] T012 Ampliar `MongoInvoiceRepository` con `DeleteAsync` y `CountByClientIdAsync` en `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs` (depende de T009)
- [X] T013 Crear índice único `Email_unique` (case-insensitive) e índice `Name_asc` para `Clients` en `backend/Infrastructure/Persistence/` (extender `MongoIndexBuilder` o nuevo `ClientIndexBuilder`)
- [X] T014 Implementar `InvoiceItemsBackfillMigration` (HostedService idempotente: item sintético `{ "Concepto", 1, Amount }` + `DueDate = CreatedAt + 30d`) en `backend/Infrastructure/Hosting/InvoiceItemsBackfillMigration.cs` (depende de T007)
- [X] T015 Implementar `ClientRepositoryEmailResolver` (resuelve `ClientId → Email` desde `Clients`, con fallback a `ConfiguredClientEmailResolver`) en `backend/Infrastructure/Clients/ClientRepositoryEmailResolver.cs` (depende de T011)
- [X] T016 Extender el seeder para crear los 3 documentos `Client` (A/B/C con sus Id estables y emails) y poblar items/dueDate en las facturas sembradas en `backend/Application/Seeding/SeedDataDefinition.cs` y `backend/Application/Seeding/DevDataSeeder.cs` (depende de T007, T008, T011)

### Cableado e infraestructura de tests

- [X] T017 Registrar `IClientRepository`, el index builder de `Clients`, la migración de backfill y el `ClientRepositoryEmailResolver` (reemplazando el resolver actual como principal) en `backend/Infrastructure/Configuration/DependencyInjection.cs` (depende de T011, T013, T014, T015)
- [X] T018 Actualizar los repositorios fake/in-memory de test al nuevo contrato (`InMemoryInvoiceRepository`, `FakeInvoiceRepository` + nuevo `InMemoryClientRepository`) en `backend/Tests/Infrastructure/Support/` (depende de T009, T010)

**Checkpoint**: Dominio, persistencia, migración y seeder listos. Las historias US1 y US2 pueden comenzar (en paralelo si hay capacidad).

---

## Phase 3: User Story 1 - Gestión completa de Facturas (Priority: P1) 🎯 MVP

**Goal**: Crear, editar (con bloqueo en estado terminal) y eliminar facturas con items + vencimiento + monto derivado, con toasts y refresco de tabla/dashboard.

**Independent Test**: En `/facturas`, crear una factura con items (total calculado solo) → aparece en tabla y dashboard; editarla; eliminarla tras confirmación; verificar bloqueo de edición en una factura pagada. Usa los clientes sembrados (no requiere la UI de US2).

### Tests para US1 (escribir primero, deben FALLAR) ⚠️

- [X] T019 [P] [US1] Test de integración `POST /api/invoices` (cliente válido, items, monto derivado, cliente inexistente → 400) en `backend/Tests/Api/CreateInvoiceEndpointTests.cs`
- [X] T020 [P] [US1] Test de integración `PUT /api/invoices/{id}` (edición válida, recálculo de monto, bloqueo en estado terminal → 409, 404) en `backend/Tests/Api/UpdateInvoiceEndpointTests.cs`
- [X] T021 [P] [US1] Test de integración `DELETE /api/invoices/{id}` (borrado permanente en cualquier estado → 204, 404) en `backend/Tests/Api/DeleteInvoiceEndpointTests.cs`

### Implementación backend US1

- [X] T022 [US1] Ampliar `InvoiceDtos` con `InvoiceItemDto`, `dueDate` en los DTOs de salida y los records `CreateInvoiceRequest`/`UpdateInvoiceRequest` (sin `amount`, sin `status`) en `backend/Api/Endpoints/Invoices/InvoiceDtos.cs`
- [X] T023 [P] [US1] `CreateInvoiceValidator` (clientId no vacío; ≥1 item; cada item descripción/cantidad>0/precio>0; dueDate válida) en `backend/Application/Validation/CreateInvoiceValidator.cs`
- [X] T024 [P] [US1] `UpdateInvoiceValidator` (mismas reglas que Create) en `backend/Application/Validation/UpdateInvoiceValidator.cs`
- [X] T025 [US1] Endpoint `MapCreateInvoice` (valida cliente existente, deriva monto, status inicial Pending, 201 + Location, logging Serilog) en `backend/Api/Endpoints/Invoices/CreateInvoice.cs` (depende de T022, T023)
- [X] T026 [US1] Endpoint `MapUpdateInvoice` (404, bloqueo terminal → 409, valida cliente, recalcula monto, logging) en `backend/Api/Endpoints/Invoices/UpdateInvoice.cs` (depende de T022, T024)
- [X] T027 [US1] Endpoint `MapDeleteInvoice` (hard delete, 204/404, logging) en `backend/Api/Endpoints/Invoices/DeleteInvoice.cs` (depende de T012)
- [X] T028 [US1] Registrar `MapCreateInvoice`/`MapUpdateInvoice`/`MapDeleteInvoice` en `backend/Api/Program.cs` (depende de T025, T026, T027)

### Implementación frontend US1

- [X] T029 [P] [US1] Ampliar los tipos de factura con `items` y `dueDate` en `frontend/src/features/invoices/types.ts`
- [X] T030 [P] [US1] Funciones de API + hooks `useCreateInvoice`/`useUpdateInvoice`/`useDeleteInvoice` con invalidación dirigida (`['invoices']`, `['invoice-stats']`, detalle) en `frontend/src/features/invoices/api/`
- [X] T031 [US1] Componente `InvoiceItemsEditor` (añadir/quitar líneas, subtotal y total en vivo de solo lectura) en `frontend/src/features/invoices/components/InvoiceItemsEditor.tsx` (depende de T029)
- [X] T032 [US1] `InvoiceFormModal` (alta/edición, selector de cliente vía `/api/clients`, fecha de vencimiento, validación espejo, deshabilitado en estado terminal) en `frontend/src/features/invoices/components/InvoiceFormModal.tsx` (depende de T030, T031)
- [X] T033 [US1] `DeleteInvoiceDialog` (modal de confirmación) en `frontend/src/features/invoices/components/DeleteInvoiceDialog.tsx` (depende de T030)
- [X] T034 [US1] Cablear acciones crear/editar/eliminar + toasts de éxito/error (conservando datos del formulario ante error) en `InvoicesPage.tsx` y `InvoicesTable.tsx` de `frontend/src/features/invoices/components/` (depende de T032, T033)

### Tests de UI US1

- [X] T035 [P] [US1] Tests Vitest del `InvoiceItemsEditor`, `InvoiceFormModal` y hooks de mutación en `frontend/src/features/invoices/` (depende de T030, T031, T032)
- [ ] T036 [US1] E2E Playwright: jornada crear → editar → eliminar factura y verificación de bloqueo terminal en `frontend` (depende de T034)

**Checkpoint**: US1 (CRUD de facturas) completamente funcional y testeable de forma independiente. MVP entregable.

---

## Phase 4: User Story 2 - Gestión completa de Clientes (Priority: P2)

**Goal**: Listar (paginado + búsqueda), crear, editar y eliminar clientes (con guard de facturas asociadas), con toasts y refresco automático.

**Independent Test**: En `/clientes`, listar y buscar; crear un cliente con email único; editarlo; eliminar uno sin facturas; intentar eliminar uno con facturas (`seed-cliente-a`) y verificar el rechazo con mensaje.

### Tests para US2 (escribir primero, deben FALLAR) ⚠️

- [X] T037 [P] [US2] Test de integración `GET /api/clients` (paginación, búsqueda por nombre/email, validación de page/pageSize) en `backend/Tests/Api/ListClientsEndpointTests.cs`
- [X] T038 [P] [US2] Test de integración `POST`/`PUT /api/clients` (validación de nombre/email, unicidad de email → 400, edición excluye al propio cliente) en `backend/Tests/Api/ClientWriteEndpointTests.cs`
- [X] T039 [P] [US2] Test de integración `DELETE /api/clients/{id}` (204 sin facturas; 409 con facturas asociadas; 404) en `backend/Tests/Api/DeleteClientEndpointTests.cs`

### Implementación backend US2

- [X] T040 [P] [US2] `ClientDtos` (`ClientDto`, `CreateClientRequest`, `UpdateClientRequest`) en `backend/Api/Endpoints/Clients/ClientDtos.cs`
- [X] T041 [P] [US2] `ListClientsQueryValidator` (page≥1, pageSize 1..50, search≤100) en `backend/Application/Validation/ListClientsQueryValidator.cs`
- [X] T042 [P] [US2] `CreateClientValidator` y `UpdateClientValidator` (nombre no vacío, email formato + unicidad) en `backend/Application/Validation/`
- [X] T043 [US2] Endpoint `MapListClients` (paginado + búsqueda, `PagedResponse<ClientDto>`, logging) en `backend/Api/Endpoints/Clients/ListClients.cs` (depende de T040, T041)
- [X] T044 [US2] Endpoint `MapGetClientById` (200/404) en `backend/Api/Endpoints/Clients/GetClientById.cs` (depende de T040)
- [X] T045 [US2] Endpoint `MapCreateClient` (unicidad email, 201 + Location, logging) en `backend/Api/Endpoints/Clients/CreateClient.cs` (depende de T040, T042)
- [X] T046 [US2] Endpoint `MapUpdateClient` (404, unicidad excluyendo propio, logging) en `backend/Api/Endpoints/Clients/UpdateClient.cs` (depende de T040, T042)
- [X] T047 [US2] Endpoint `MapDeleteClient` (guard de facturas → 409, 204/404, logging) en `backend/Api/Endpoints/Clients/DeleteClient.cs` (depende de T040, T012)
- [X] T048 [US2] Registrar los endpoints de clientes en `backend/Api/Program.cs` (depende de T043, T044, T045, T046, T047)

### Implementación frontend US2

- [X] T049 [P] [US2] Tipos de cliente en `frontend/src/features/clients/types.ts`
- [X] T050 [P] [US2] Funciones de API + hooks `useClients` (con búsqueda/paginación), `useCreateClient`/`useUpdateClient`/`useDeleteClient` con invalidación de `['clients']` en `frontend/src/features/clients/api/`
- [X] T051 [US2] Componentes `ClientsTable`, `ClientsSearch`, `ClientsPagination` (incluye estado vacío) en `frontend/src/features/clients/components/` (depende de T049)
- [X] T052 [US2] `ClientFormModal` (alta/edición, validación espejo, nombre/email obligatorios, teléfono/dirección opcionales) en `frontend/src/features/clients/components/ClientFormModal.tsx` (depende de T050)
- [X] T053 [US2] `DeleteClientDialog` (confirmación + traducción del 409 a mensaje claro) en `frontend/src/features/clients/components/DeleteClientDialog.tsx` (depende de T050)
- [X] T054 [US2] `ClientsPage` ensamblando listado + búsqueda + paginación + acciones + toasts en `frontend/src/features/clients/components/ClientsPage.tsx` (depende de T051, T052, T053)
- [X] T055 [US2] Añadir la ruta lazy `/clientes` en `frontend/src/App.tsx` y el enlace de navegación en el sidebar (depende de T054)

### Tests de UI US2

- [X] T056 [P] [US2] Tests Vitest de componentes/hooks de clientes (formulario, listado, borrado con 409) en `frontend/src/features/clients/` (depende de T050, T052, T053)
- [ ] T057 [US2] E2E Playwright: listar/buscar/crear/editar/eliminar cliente, incluido borrado con facturas asociadas, en `frontend` (depende de T055)

**Checkpoint**: US1 y US2 funcionan de forma independiente. Funcionalidad completa.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Calidad, documentación y verificación final transversal.

- [X] T058 [P] Actualizar documentación de arquitectura (README) y registrar un ADR para "monto derivado de items" y "entidad Cliente + integridad referencial en aplicación" en `docs/` o `README`
- [X] T059 [P] Ejecutar Biome y React Doctor sobre `frontend` y resolver hasta 0 warnings (Constitución V)
- [X] T060 Verificar cobertura ≥85% en backend (xUnit) y frontend (Vitest) y publicar el reporte
- [ ] T061 Ejecutar los escenarios de validación de `quickstart.md` (facturas, clientes, migración) y confirmar resultados esperados
- [X] T062 [P] Verificar uso de índices (`Email_unique`, `Name_asc`, `ClientId`) y paginación forzada en las consultas de `Clients` (Constitución Performance)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sin dependencias — puede iniciar de inmediato.
- **Foundational (Phase 2)**: Depende de Setup — BLOQUEA todas las historias.
- **User Stories (Phase 3+)**: Dependen de Foundational. US1 y US2 pueden ir en paralelo tras la fase 2.
- **Polish (Phase 5)**: Depende de completar las historias deseadas.

### User Story Dependencies

- **US1 (P1)**: Inicia tras Foundational. Usa los clientes sembrados; no depende de la UI de US2.
- **US2 (P2)**: Inicia tras Foundational. Independiente de US1 (aunque el selector de cliente de US1 consume `GET /api/clients`, que US2 también expone; ambos se apoyan en la capa foundational, no entre sí).

### Within Each User Story

- Tests primero (deben fallar) → DTOs/modelos → validadores → endpoints → registro → frontend → tests de UI/E2E.

### Parallel Opportunities

- Setup: T001, T002 en paralelo.
- Foundational: tests T003–T005 en paralelo; T006 y T008 y T010 en paralelo (distintos archivos); T011/T012 tras sus contratos.
- US1: T019–T021 (tests) en paralelo; T023/T024 (validadores) en paralelo; T029/T030 (tipos/api) en paralelo.
- US2: T037–T039 (tests) en paralelo; T040/T041/T042 en paralelo; T049/T050 en paralelo.
- Con equipo: US1 y US2 por desarrolladores distintos tras la fase 2.

---

## Parallel Example: User Story 1

```bash
# Tests de US1 juntos (deben fallar primero):
Task: "Integración POST /api/invoices en backend/Tests/Api/CreateInvoiceEndpointTests.cs"
Task: "Integración PUT /api/invoices/{id} en backend/Tests/Api/UpdateInvoiceEndpointTests.cs"
Task: "Integración DELETE /api/invoices/{id} en backend/Tests/Api/DeleteInvoiceEndpointTests.cs"

# Validadores de US1 juntos:
Task: "CreateInvoiceValidator en backend/Application/Validation/CreateInvoiceValidator.cs"
Task: "UpdateInvoiceValidator en backend/Application/Validation/UpdateInvoiceValidator.cs"
```

---

## Implementation Strategy

### MVP First (solo US1)

1. Completar Phase 1: Setup.
2. Completar Phase 2: Foundational (CRÍTICO — bloquea todo).
3. Completar Phase 3: US1 (CRUD de facturas).
4. **DETENERSE y VALIDAR**: probar US1 de forma independiente con clientes sembrados.
5. Desplegar/demostrar si está listo (MVP).

### Incremental Delivery

1. Setup + Foundational → base lista (incluye entidad Cliente, migración, seeder).
2. US1 → probar → desplegar (MVP).
3. US2 → probar → desplegar.
4. Polish → calidad y verificación final.

### Parallel Team Strategy

Tras la fase 2: Desarrollador A toma US1; Desarrollador B toma US2. Integran de forma independiente apoyándose en la capa foundational compartida.

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes.
- La etiqueta [Story] mapea cada tarea a su historia para trazabilidad.
- Test-First obligatorio (Constitución IV): verificar que los tests fallan antes de implementar.
- Commit por tarea o grupo lógico; mensaje con referencia a la spec (ej.: "feat(spec-018): …").
- Evitar dependencias cruzadas entre US1 y US2 que rompan su independencia.

---

## Estado final (2026-06-26)

**59 / 62 tareas completadas.** Ver resumen en [README.md](./README.md).

Verificación:
- Backend: `dotnet build` 0 errores; **365 tests** (269 `Tests.csproj` incl. integración HTTP + Mongo, 96 `Domain.Tests`).
- Frontend: `tsc -b` 0 errores; Biome 0 issues; **148 tests** (`vitest`).

Pendientes (3):
- **T036, T057** — E2E Playwright: omitidos por decisión del usuario (requiere instalar `@playwright/test` + binarios de navegador).
- **T061** — validación manual de `quickstart.md` en navegador (las partes automatizables ya están cubiertas por las suites de tests).
