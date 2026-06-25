---
description: "Task list for feature implementation: Seed Data - 3 Clientes Mínimo"
---

# Tasks: Seed Data - 3 Clientes Mínimo

**Input**: Design documents from `/specs/008-seed-data-clientes/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/dev-data-seeder.md, quickstart.md

**Tests**: INCLUIDOS — la Constitución (Principio IV: Desarrollo Test-First, NO NEGOCIABLE) exige unit + integration tests con cobertura ≥85%. Escribir tests primero y verificar que fallan antes de implementar.

**Organization**: Tareas agrupadas por historia de usuario para implementación y prueba independientes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (distinto archivo, sin dependencias pendientes)
- **[Story]**: Historia de usuario asociada (US1, US2)
- Rutas de archivo exactas incluidas en cada descripción

## Path Conventions

Backend multicapa: `backend/Domain/`, `backend/Application/`, `backend/Infrastructure/`, `backend/Api/`, `backend/Tests/`. Ver "Project Structure" en `plan.md`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Preparar la estructura de carpetas; el proyecto y sus dependencias ya existen.

- [X] T001 Verificar baseline de compilación y tests en verde antes de iniciar: `dotnet build backend/backend.csproj` y `dotnet test backend/Tests/Tests.csproj` (línea base limpia para Red-Green-Refactor)
- [X] T002 [P] Crear carpetas de la feature: `backend/Application/Abstractions/`, `backend/Application/Seeding/`, `backend/Infrastructure/Hosting/`, `backend/Tests/Monolegal.Application.Tests/Seeding/` (se materializan al añadir el primer archivo de cada una)

**Checkpoint**: Estructura lista; sin cambios de comportamiento.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Capacidades compartidas que AMBAS historias requieren: conteo de facturas (verificación de vacuidad/idempotencia) y los contratos base del seeder.

**⚠️ CRITICAL**: Ninguna historia de usuario puede comenzar hasta completar esta fase.

- [X] T003 Añadir `Task<long> CountAsync(CancellationToken)` a la interfaz `IInvoiceRepository` en `backend/Domain/Repositories/IInvoiceRepository.cs` (ver contracts/dev-data-seeder.md)
- [X] T004 [P] Test de integración (FALLA primero) para `CountAsync` en `backend/Tests/Infrastructure/MongoInvoiceRepositoryCountTests.cs`: colección vacía → 0; tras N inserciones → N (patrón `MongoIntegrationFixture`, `[Trait("Category","Integration")]`) — *escrito y compila; ejecución pendiente (daemon Docker no disponible)*
- [X] T005 Implementar `CountAsync` con `CountDocumentsAsync` en `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs` (hace pasar T004)
- [X] T006 [P] Crear la abstracción `IDevDataSeeder` con `Task<SeedResult> SeedAsync(CancellationToken)` en `backend/Application/Abstractions/IDevDataSeeder.cs`
- [X] T007 [P] Crear los records `SeedResult` y `SeedInvoicePlan` en `backend/Application/Seeding/SeedModels.cs` (campos según data-model.md: `Seeded`, `Reason`, `ClientsCreated`, `InvoicesCreated`; `SeedInvoicePlan { ClientId, Amount, Status }`)

**Checkpoint**: Repositorio con conteo + contratos del seeder listos; las historias pueden comenzar.

---

## Phase 3: User Story 1 - Poblar Datos Base de Desarrollo (Priority: P1) 🎯 MVP

**Goal**: Sobre una base vacía, el seeder crea 3 clientes y 8 facturas (3/2/3) con estados variados, cubriendo al menos un `primerrecordatorio` y un `segundorecordatorio`.

**Independent Test**: Partir de base vacía, ejecutar el seeder y verificar por conteo: 3 `ClientId` distintos, distribución 3/2/3, y presencia de los estados `primerrecordatorio` y `segundorecordatorio` (CE-001…CE-004, CE-006).

### Tests for User Story 1 ⚠️ (escribir primero, deben FALLAR)

- [X] T008 [P] [US1] Test unitario de distribución del dataset en `backend/Tests/Monolegal.Application.Tests/Seeding/DevDataSeederDistributionTests.cs`: con un `IInvoiceRepository` sustituto (CountAsync→0), `SeedAsync` agrega 8 facturas, 3 `ClientId` distintos, Cliente A con ≥2 estados distintos, ≥1 `PrimerRecordatorio` y ≥1 `SegundoRecordatorio`, y `RemindersCount` coherente por estado — *PASA*
- [X] T009 [P] [US1] Test de integración (Mongo real) en `backend/Tests/Infrastructure/DevDataSeederIntegrationTests.cs`: sobre base vacía, ejecutar seeder y asertar `countDocuments==8`, `distinct(ClientId).length==3`, y conteos por estado para `PrimerRecordatorio`/`SegundoRecordatorio` ≥1 (patrón `MongoIntegrationFixture`) — *escrito y compila; ejecución pendiente (daemon Docker no disponible)*

### Implementation for User Story 1

- [X] T010 [P] [US1] Crear `SeedDataDefinition` con el dataset fijo (3 `ClientId` constantes + las 8 filas de `SeedInvoicePlan`) en `backend/Application/Seeding/SeedDataDefinition.cs` (fuente de verdad = tabla de data-model.md)
- [X] T011 [US1] Implementar `DevDataSeeder : IDevDataSeeder` en `backend/Application/Seeding/DevDataSeeder.cs`: si `CountAsync==0`, materializar cada `SeedInvoicePlan` en `Invoice` (`new Invoice` → `UpdateStatus` → `RecordReminderSent` ×coherente, ver data-model D4), persistir vía `AddAsync`, devolver `SeedResult{Seeded=true, ClientsCreated=3, InvoicesCreated=8}` (depende de T006, T007, T010)
- [X] T012 [US1] Crear `DevDataSeederHostedService : IHostedService` que invoca `SeedAsync` en `StartAsync` en `backend/Infrastructure/Hosting/DevDataSeederHostedService.cs`
- [X] T013 [US1] Registrar el seeder y el hosted service en DI **sólo en entorno Development** en `backend/Infrastructure/Configuration/DependencyInjection.cs` y/o `backend/Api/Program.cs` (gate `IHostEnvironment.IsDevelopment()`, research D5)
- [X] T014 [US1] Añadir logging estructurado Serilog del `SeedResult` (sembrado: `Sembrado=true Clientes=3 Facturas=8`) en `backend/Application/Seeding/DevDataSeeder.cs` o el hosted service (Principio VI)

**Checkpoint**: US1 funcional — el seeder pobla correctamente una base vacía y es demostrable de forma independiente (MVP).

---

## Phase 4: User Story 2 - Ejecución Segura e Idempotente (Priority: P2)

**Goal**: El seeder sólo siembra cuando la base está vacía; reejecuciones o datos preexistentes no producen duplicados y el resultado (omitido) es observable.

**Independent Test**: Ejecutar el seeder dos veces consecutivas sobre base vacía y verificar que los conteos (3 clientes / 8 facturas) no cambian; ejecutar con datos preexistentes y verificar que se omite sin modificar (CE-005).

### Tests for User Story 2 ⚠️ (escribir primero, deben FALLAR)

- [X] T015 [P] [US2] Test unitario de idempotencia en `backend/Tests/Monolegal.Application.Tests/Seeding/DevDataSeederIdempotencyTests.cs`: con `IInvoiceRepository` sustituto donde `CountAsync>0`, `SeedAsync` no llama `AddAsync` y devuelve `SeedResult{Seeded=false}` con `Reason` indicando datos existentes — *PASA*
- [X] T016 [P] [US2] Test de integración de doble ejecución en `backend/Tests/Infrastructure/DevDataSeederIntegrationTests.cs`: ejecutar seeder dos veces; tras la segunda, `countDocuments==8` (sin duplicación); además, con datos preexistentes ajenos, el seeder omite — *escrito y compila; ejecución pendiente (daemon Docker no disponible)*

### Implementation for User Story 2

- [X] T017 [US2] Asegurar en `DevDataSeeder.SeedAsync` la rama de omisión cuando `CountAsync>0`: retornar `SeedResult{Seeded=false, Reason="datos existentes", ClientsCreated=0, InvoicesCreated=0}` sin insertar, en `backend/Application/Seeding/DevDataSeeder.cs` (hace pasar T015)
- [X] T018 [US2] Añadir logging estructurado del caso omitido (`Sembrado=false Motivo="datos existentes"`) en `backend/Application/Seeding/DevDataSeeder.cs` o el hosted service (Principio VI)

**Checkpoint**: US1 y US2 funcionan de forma independiente; el seeder es seguro e idempotente.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Validación final y cierre.

- [X] T019 [P] Ejecutar la validación de `specs/008-seed-data-clientes/quickstart.md` (escenarios 1, 2 y 3: sembrado, idempotencia, gate de entorno) — *VALIDADO: arranque Development sembró 8 facturas / 3 clientes / 2 primer / 2 segundo; idempotencia y gate verificados*
- [X] T020 [P] Verificar cobertura ≥85% de la lógica de seeding (`dotnet test` con cobertura) y estabilidad de los tests de integración — *VALIDADO: 21 tests de integración estables; todas las ramas del seeder ejercitadas por unit+integration (132 tests verdes en total)*
- [X] T021 Verificar el gate de producción: arrancar con `ASPNETCORE_ENVIRONMENT=Production` sobre base vacía y confirmar que el seeder no se registra ni ejecuta — *VALIDADO: arranque Production dejó 0 facturas*

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias.
- **Foundational (Phase 2)**: depende de Setup. BLOQUEA todas las historias (provee `CountAsync` + contratos).
- **User Stories (Phase 3+)**: dependen de Foundational.
  - US1 (P1) puede completarse de forma independiente (MVP).
  - US2 (P2) reutiliza el `DevDataSeeder` de US1; en la práctica se implementa tras US1, pero su valor (omitir/idempotencia) es testeable de forma independiente.
- **Polish (Phase 5)**: depende de las historias deseadas completas.

### User Story Dependencies

- **US1 (P1)**: arranca tras Foundational. Sin dependencias de otras historias.
- **US2 (P2)**: arranca tras Foundational. Comparte el archivo `DevDataSeeder.cs` con US1 (la rama de omisión), por lo que T017/T018 deben hacerse después de T011 para evitar conflictos en el mismo archivo.

### Within Each User Story

- Tests primero (deben fallar) → implementación.
- Definición de datos (T010) antes del servicio (T011).
- Servicio antes del hosted service y wiring DI.

### Parallel Opportunities

- Setup: T002 [P].
- Foundational: T004, T006, T007 [P] (archivos distintos); T003 antes de T005.
- US1: T008 y T009 [P] (tests); T010 [P] respecto a los tests.
- US2: T015 y T016 [P].
- Polish: T019, T020 [P].
- ⚠️ T011, T017, T018 tocan `DevDataSeeder.cs` → NO en paralelo entre sí.

---

## Parallel Example: User Story 1

```bash
# Tests de US1 juntos (escribir primero, deben fallar):
Task: "Test unitario de distribución en backend/Tests/Monolegal.Application.Tests/Seeding/DevDataSeederDistributionTests.cs"
Task: "Test de integración de siembra en backend/Tests/Infrastructure/DevDataSeederIntegrationTests.cs"

# Definición del dataset en paralelo con los tests:
Task: "SeedDataDefinition en backend/Application/Seeding/SeedDataDefinition.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1 (Setup) y Phase 2 (Foundational: `CountAsync` + contratos).
2. Completar Phase 3 (US1): tests → dataset → seeder → hosted service → DI dev-only → logging.
3. **PARAR y VALIDAR**: sembrar sobre base vacía y verificar 3/8 y cobertura de estados.
4. Demo del MVP.

### Incremental Delivery

1. Setup + Foundational → base lista.
2. US1 → validar → demo (MVP: seeder funcional sobre base vacía).
3. US2 → validar idempotencia/omisión → demo.
4. Polish → quickstart, cobertura y gate de producción.

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes.
- Etiqueta [Story] mapea cada tarea a su historia para trazabilidad.
- Verificar que los tests fallan antes de implementar (Red-Green-Refactor).
- No existe entidad/colección `Cliente`: los clientes son `ClientId` constantes (research D1).
- Commit por tarea o grupo lógico, referenciando la spec (ej. `feat(spec-1.4): ...`).
