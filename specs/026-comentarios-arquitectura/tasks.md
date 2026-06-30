---
description: "Task list — Spec 026: Comentarios de Código y Documentación de Arquitectura"
---

# Tasks: Comentarios de Código y Documentación de Arquitectura

**Input**: Design documents from `specs/026-comentarios-arquitectura/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: No se solicitan tareas de test. Es una feature de documentación/comentarios; la validación es por checklist y `quickstart.md`, y se exige que `dotnet build` permanezca verde tras añadir comentarios.

**Organization**: Tareas agrupadas por historia de usuario (US1–US4) para implementación y validación independientes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias).
- **[Story]**: US1, US2, US3, US4.
- Rutas de archivo exactas incluidas.

## Path Conventions

Aplicación web multi-proyecto existente: `backend/` (Domain/Application/Infrastructure/Api), `worker/`, `frontend/`, documentación en `docs/` y `README.md`. No se crean proyectos nuevos.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establecer línea base y confirmar el material existente sobre el que se documenta.

- [X] T001 Establecer línea base: ejecutar `dotnet build` del backend (`backend/`) y del worker (`worker/`) y confirmar que compilan en verde antes de añadir comentarios
- [X] T002 [P] Confirmar artefactos de documentación existentes (`README.md`, `docs/architecture.md`, `docs/adr/0001-*.md`, `docs/adr/0002-*.md`) y registrar qué se reutiliza vs. qué falta, según `research.md` D1

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Fijar la lista canónica de "clases clave" que define la cobertura de US2.

**⚠️ CRITICAL**: Bloquea US2 (los comentarios SOLID se miden contra esta lista).

- [X] T003 Fijar la lista canónica de "clases clave" a comentar (servicios de dominio/aplicación, notificadores, resolvers, validadores, repositorios, proveedores/factory de email, workers, seeder, mantenimiento), conforme a `research.md` D2, y registrarla como checklist de cobertura en la descripción del PR

**Checkpoint**: Lista de cobertura acordada — US1–US4 pueden comenzar.

---

## Phase 3: User Story 1 - Entender la Clean Architecture desde el README (Priority: P1) 🎯 MVP

**Goal**: README y `docs/architecture.md` explican las capas, la dirección de dependencias y aportan diagrama, de modo que una persona sin contexto entienda la arquitectura en < 15 min.

**Independent Test**: Abrir `README.md` y `docs/architecture.md` y verificar capas + responsabilidad única + dirección de dependencias (externas → internas) + confinamiento a Infrastructure + al menos un diagrama Mermaid.

### Implementation for User Story 1

- [X] T004 [P] [US1] Revisar y, si falta, ampliar la sección "Arquitectura" de `README.md`: descripción de cada capa (Domain, Application, Infrastructure, Api), Worker y Frontend con su responsabilidad única (FR-001)
- [X] T005 [US1] Verificar/ampliar `docs/architecture.md` para enunciar explícitamente la regla de dirección de dependencias y que los cambios tecnológicos quedan confinados a la capa Infrastructure (FR-002)
- [X] T006 [US1] Asegurar al menos un diagrama Mermaid de capas y relaciones de dependencia en `docs/architecture.md` (FR-003)
- [X] T007 [US1] Añadir enlaces cruzados desde `README.md` (y `docs/README.md`) hacia `docs/architecture.md`, `docs/dependency-injection.md` y `docs/adr/`

**Checkpoint**: US1 funcional e independientemente verificable (Escenario 1 de `quickstart.md`).

---

## Phase 4: User Story 2 - Verificar principios SOLID con comentarios en clase (Priority: P2)

**Goal**: Las clases clave de `backend/` y `worker/` declaran su(s) principio(s) SOLID mediante comentarios XML-doc según `contracts/convencion-comentarios-solid.md`.

**Independent Test**: `grep -rln "SOLID:" backend/ worker/ --include="*.cs"` cubre la lista canónica (T003); muestra de clases con principio + justificación concreta; `dotnet build` verde.

### Implementation for User Story 2

- [X] T008 [P] [US2] Comentario SOLID de clase en `backend/Domain/Services/InvoiceTransitionService.cs` (FR-004/FR-005)
- [X] T009 [P] [US2] Comentarios SOLID en `backend/Application/Services/AppService.cs` y `backend/Application/Services/InvoiceShipmentService.cs`
- [X] T010 [P] [US2] Comentario SOLID en `backend/Application/Notifications/InvoiceTransitionNotifier.cs`
- [X] T011 [P] [US2] Comentarios SOLID en los validadores de `backend/Application/Validation/*.cs` (ClientValidators, CreateInvoiceValidator, ListClientsQueryValidator, ListInvoicesQueryValidator, SendTestEmailValidator, ShipmentsQueryValidator, TransitionInvoiceRequestValidator, UpdateEmailSettingsValidator, UpdateEmailTemplateValidator, UpdateInvoiceValidator)
- [X] T012 [P] [US2] Comentario SOLID en `backend/Application/Seeding/DevDataSeeder.cs`
- [X] T013 [P] [US2] Comentarios SOLID en `backend/Infrastructure/Repositories/MongoClientRepository.cs`, `MongoInvoiceRepository.cs` y `MongoSystemSettingsRepository.cs` (énfasis DIP)
- [X] T014 [P] [US2] Comentarios SOLID en los servicios/proveedores de `backend/Infrastructure/Email/*.cs` (EmailAdminService, EmailCredentialStatusService, EmailProviderFactory, EmailTemplateProvider, NoOpEmailService, ResendEmailProvider, SettingsBackedEmailService, SmtpEmailProvider; excluir `EmailOptions.cs`)
- [X] T015 [P] [US2] Comentarios SOLID en `backend/Infrastructure/Clients/ClientRepositoryEmailResolver.cs` y `ConfiguredClientEmailResolver.cs`
- [X] T016 [P] [US2] Comentario SOLID en `backend/Infrastructure/Workers/InvoiceTransitionsWorker.cs` (excluir `InvoiceTransitionsWorkerOptions.cs`)
- [X] T017 [P] [US2] Comentario SOLID en `backend/Infrastructure/Maintenance/MaintenanceService.cs`
- [X] T018 [P] [US2] Comentario SOLID en `worker/Services/BackgroundWorker.cs`
- [X] T019 [US2] Verificar `dotnet build` verde tras los comentarios y que `grep "SOLID:"` en `backend/` + `worker/` cubre el 100% de la lista canónica de T003 (SC-002)

**Checkpoint**: US2 funcional e independientemente verificable (Escenario 2 de `quickstart.md`).

---

## Phase 5: User Story 3 - Mapear la Inyección de Dependencias (Priority: P2)

**Goal**: `docs/dependency-injection.md` lista cada abstracción → implementación → ciclo de vida, sincronizado con el registro real, según `contracts/esquema-mapa-di.md`.

**Independent Test**: Comparar 3–5 filas del documento contra `grep -nE "AddSingleton|AddScoped|AddTransient|AddHostedService"` en `DependencyInjection.cs`/`Program.cs`; correspondencia 1:1 sin entradas obsoletas.

### Implementation for User Story 3

- [X] T020 [US3] Crear `docs/dependency-injection.md` con introducción (DI centralizado) y la tabla `Abstracción | Implementación | Ciclo de vida | Registrado en`, según `contracts/esquema-mapa-di.md` (FR-006)
- [X] T021 [US3] Completar la tabla con todos los registros de `backend/Infrastructure/Configuration/DependencyInjection.cs` (repos, servicios de email, resolvers, notificador, seeder, mantenimiento, mongo client/db, hosted services) con su ciclo de vida real (FR-006/FR-007)
- [X] T022 [US3] Añadir al mapa los registros adicionales de `backend/Api/Program.cs`
- [X] T023 [US3] Añadir al mapa los registros del proyecto `worker/` (`worker/Program.cs`)
- [X] T024 [US3] Añadir la nota de mantenimiento (documento vivo, FR-012) y verificar sincronía 1:1 contra el registro real (SC-003)

**Checkpoint**: US3 funcional e independientemente verificable (Escenario 3 de `quickstart.md`).

---

## Phase 6: User Story 4 - Registrar decisiones importantes con ADRs (Priority: P3)

**Goal**: `docs/adr/` formalizado con plantilla e índice, y ADRs retroactivos de las decisiones no obvias vigentes (`research.md` D7), según `contracts/plantilla-adr.md`.

**Independent Test**: `docs/adr/README.md` y `docs/adr/0000-plantilla.md` existen; cada ADR sigue el formato (Estado, Fecha, Contexto, Decisión, Alternativas, Consecuencias); decisiones no obvias vigentes registradas.

### Implementation for User Story 4

- [X] T025 [P] [US4] Crear `docs/adr/0000-plantilla.md` desde `contracts/plantilla-adr.md` (FR-008/FR-010)
- [X] T026 [P] [US4] Crear `docs/adr/README.md` (índice) listando los ADRs existentes (0001, 0002) con número, título, estado y fecha
- [X] T027 [P] [US4] ADR retroactivo `docs/adr/0003-repositorios-singleton-mongodb.md`: ciclo de vida Singleton de los repositorios con el driver de MongoDB
- [X] T028 [P] [US4] ADR retroactivo `docs/adr/0004-seleccion-proveedor-email-runtime.md`: factory de proveedores de email + fallback `NoOp` en Dev/CI (spec 017)
- [X] T029 [P] [US4] ADR retroactivo `docs/adr/0005-migraciones-idempotentes-hostedservice.md`: migraciones idempotentes como `IHostedService` al arranque (specs 015/018)
- [X] T030 [P] [US4] ADR retroactivo `docs/adr/0006-worker-backgroundservice-estado-mongodb.md`: worker `BackgroundService` con estado en MongoDB (sin estado en memoria)
- [X] T031 [US4] Actualizar `docs/adr/README.md` con los nuevos ADRs (0003–0006) y sus estados; aplicar enlaces `Reemplaza`/`Reemplazado por` donde corresponda (FR-010)

**Checkpoint**: US4 funcional e independientemente verificable (Escenario 4 de `quickstart.md`).

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Validación final y consistencia.

- [X] T032 [P] Ejecutar la validación de `specs/026-comentarios-arquitectura/quickstart.md` (5 escenarios) y registrar resultados
- [X] T033 [P] Verificar que toda la documentación entregada (`README.md`, `docs/architecture.md`, `docs/dependency-injection.md`, `docs/adr/*`) y los comentarios están en español (FR-011/SC-006)
- [X] T034 Revisar enlaces cruzados entre `README.md`, `docs/architecture.md`, `docs/dependency-injection.md` y `docs/adr/` (sin enlaces rotos)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sin dependencias — inicia de inmediato.
- **Foundational (Phase 2)**: Depende de Setup. **Bloquea US2** (lista de cobertura).
- **User Stories (Phase 3–6)**: Dependen de Setup; US2 además depende de Foundational. US1, US3 y US4 son independientes entre sí y de US2.
- **Polish (Phase 7)**: Depende de las historias que se quieran entregar.

### User Story Dependencies

- **US1 (P1)**: Independiente. Solo requiere Setup. → **MVP**.
- **US2 (P2)**: Requiere Foundational (T003). Independiente de US1/US3/US4.
- **US3 (P2)**: Independiente. Solo requiere Setup.
- **US4 (P3)**: Independiente. Solo requiere Setup.

### Within Each User Story

- US1: T004 (README) independiente de T005/T006 (mismo archivo `architecture.md`, secuenciales entre sí); T007 al final.
- US2: T008–T018 tocan archivos distintos → todas paralelas; T019 (verificación) depende de todas.
- US3: T020 crea el archivo; T021–T024 editan el mismo `dependency-injection.md` → secuenciales.
- US4: T025–T030 crean archivos distintos → paralelas; T031 (índice) depende de T026–T030.

### Parallel Opportunities

- T002 en Setup es [P].
- US2 es altamente paralelizable: T008–T018 (11 tareas) en paralelo.
- US4: T025–T030 en paralelo.
- Una vez completado Setup, US1, US3 y US4 pueden avanzar en paralelo; US2 tras T003.
- US1 ⟂ US2 ⟂ US3 ⟂ US4 (distintos archivos/áreas).

---

## Parallel Example: User Story 2

```bash
# Lanzar en paralelo los comentarios SOLID por área (archivos distintos):
Task: "Comentario SOLID en backend/Domain/Services/InvoiceTransitionService.cs"
Task: "Comentarios SOLID en backend/Application/Services/*.cs"
Task: "Comentarios SOLID en backend/Application/Validation/*.cs"
Task: "Comentarios SOLID en backend/Infrastructure/Repositories/*.cs"
Task: "Comentarios SOLID en backend/Infrastructure/Email/*.cs"
Task: "Comentario SOLID en worker/Services/BackgroundWorker.cs"
# Después (no paralelo):
Task: "Verificar dotnet build verde + grep 'SOLID:' cubre la lista canónica"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1: Setup.
2. Completar Phase 3: US1 (Clean Architecture en README/architecture.md).
3. **PARAR y VALIDAR**: Escenario 1 de `quickstart.md`.
4. Entregar/demostrar si está listo.

### Incremental Delivery

1. Setup → base lista.
2. US1 → validar → entregar (MVP: documentación de arquitectura).
3. US3 (mapa DI) y US4 (ADRs) en paralelo → validar cada una.
4. US2 (comentarios SOLID, tras T003) → validar build verde → entregar.
5. Polish (Phase 7) → validación final.

### Parallel Team Strategy

Tras Setup + Foundational: Dev A → US1; Dev B → US2; Dev C → US3; Dev D → US4. Las historias integran de forma independiente (archivos distintos).

---

## Notes

- Tareas [P] = archivos distintos, sin dependencias.
- US2 no introduce código funcional: solo comentarios XML-doc; el `dotnet build` debe permanecer verde (T019).
- Mantener todos los documentos como "documentos vivos" (FR-012): actualizar el mapa DI y la arquitectura en el mismo PR que cambie la estructura.
- Toda la documentación y comentarios en español (FR-011).
- Commit tras cada tarea o grupo lógico; referenciar `spec-6.2` / spec 026.
