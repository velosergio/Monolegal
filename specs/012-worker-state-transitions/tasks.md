---
description: "Task list — Worker: Transiciones de Estado"
---

# Tasks: Worker — Transiciones de Estado

**Input**: Documentos de diseño en `specs/012-worker-state-transitions/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/invoice-transitions-worker.md, quickstart.md

**Tests**: INCLUIDOS — la Constitución (IV. Test-First, NO NEGOCIABLE) obliga a escribir las pruebas primero y verlas fallar antes de implementar.

**Contexto importante**: La mayor parte del worker **ya existe** (creado con la spec 006): `InvoiceTransitionsWorker`, `InvoiceTransitionService`, `IInvoiceRepository.GetTransitionableAsync` + `MongoInvoiceRepository`, registro en DI y el endpoint `POST /api/workers/trigger-transitions`. Estas tareas **cierran las brechas** frente a los requisitos 012 (intervalo configurable, aislamiento de errores por factura, conteo de errores) y añaden pruebas directas del worker. Lo ya cumplido solo se verifica.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: Historia de usuario a la que pertenece (US1, US2, US3)

## Path Conventions

- Backend por capas: `backend/Domain`, `backend/Application`, `backend/Infrastructure`, `backend/Api`
- Pruebas: `backend/Tests/Monolegal.Application.Tests/`

---

## Phase 1: Setup (Infraestructura compartida)

**Purpose**: Preparar el terreno para poder probar el worker real.

- [X] T001 Establecer línea base verde ejecutando `dotnet test` en `backend/` y registrar el estado actual (todas las pruebas existentes deben pasar antes de modificar nada).

---

## Phase 2: Foundational (Prerrequisitos bloqueantes)

**Purpose**: Habilitar el acceso de pruebas a la lógica interna del worker. BLOQUEA a todas las historias porque sus pruebas ejercitan `RunCycleAsync`.

**⚠️ CRITICAL**: Ninguna historia puede comenzar hasta completar esta fase.

- [X] T002 Crear `backend/Infrastructure/AssemblyInfo.cs` con `[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Tests")]` para exponer `internal Task RunCycleAsync(...)` al ensamblado de pruebas `Tests`.
- [X] T003 Crear el archivo de pruebas `backend/Tests/Monolegal.Application.Tests/InvoiceTransitionsWorkerCycleTests.cs` con el andamiaje base (clase, `[Trait("Category","Worker")]`, helpers para construir el worker con `Options.Create(...)` y fakes en memoria reutilizando `InMemoryInvoiceRepository`/`InMemorySystemSettingsRepository`). Sin casos aún.

**Checkpoint**: El worker real es testeable desde el ensamblado `Tests`.

---

## Phase 3: User Story 1 — Ejecución Automática de Transiciones por Tiempo (Priority: P1) 🎯 MVP

**Goal**: Garantizar que un ciclo del worker aplica las transiciones a las facturas elegibles, ignora terminales y **continúa el lote aunque una factura falle**.

**Independent Test**: Ejecutar `RunCycleAsync` sobre facturas en distintos estados/tiempos y verificar transiciones correctas, terminales intactas y aislamiento de errores por factura.

### Tests for User Story 1 (escribir primero, deben FALLAR) ⚠️

- [X] T004 [P] [US1] En `InvoiceTransitionsWorkerCycleTests.cs`: test `RunCycle_FacturasElegibles_TransicionanAlSiguienteEstado` (Pending→Primer, Primer→Segundo, Segundo→Desactivado) usando `now` y umbrales configurados (C-01).
- [X] T005 [P] [US1] En `InvoiceTransitionsWorkerCycleTests.cs`: test `RunCycle_FacturasTerminales_NoCambian` (Pagado/Desactivado no se evalúan ni cambian) (C-02).
- [X] T006 [P] [US1] En `InvoiceTransitionsWorkerCycleTests.cs`: test `RunCycle_FacturaQueFalla_NoAbortaElLote` con un repositorio fake cuyo `UpdateAsync` lanza para una factura concreta; el resto del lote debe transicionar y el ciclo terminar sin propagar la excepción (C-03, SC-004).

### Implementation for User Story 1

- [X] T007 [US1] Crear el fake `ThrowingInvoiceRepository` (o ampliar el `InMemoryInvoiceRepository` de pruebas) en `backend/Tests/Monolegal.Application.Tests/` para simular fallo de persistencia en una factura específica (requerido por T006).
- [X] T008 [US1] Modificar `backend/Infrastructure/Workers/InvoiceTransitionsWorker.cs` (`RunCycleAsync`): envolver el procesamiento de **cada factura** en `try/catch`; ante excepción, registrar `LogError` con `InvoiceId` y continuar con la siguiente (aislamiento por factura). Conservar el `try/catch` de ciclo como red de seguridad.
- [X] T009 [US1] Alinear el endpoint `backend/Api/Endpoints/Workers/TriggerTransitions.cs` para aislar también los errores por factura (mismo comportamiento que el worker), de modo que el disparo manual no aborte el lote ante un fallo individual.

**Checkpoint**: US1 funcional y verificable de forma independiente (transiciones + aislamiento de errores).

---

## Phase 4: User Story 2 — Programación Periódica Configurable (Priority: P2)

**Goal**: Externalizar el intervalo de ejecución a configuración con un default razonable, sin recompilar.

**Independent Test**: Configurar el intervalo por variable de entorno/sección y verificar que el worker usa el valor efectivo (y el default cuando no se configura).

### Tests for User Story 2 (escribir primero, deben FALLAR) ⚠️

- [X] T010 [P] [US2] Crear `backend/Tests/Monolegal.Application.Tests/InvoiceTransitionsWorkerOptionsTests.cs`: test del default (`IntervalMinutes == 60`, `RunOnStartup == true`) cuando no hay configuración.
- [X] T011 [P] [US2] En `InvoiceTransitionsWorkerOptionsTests.cs`: test de enlace desde configuración (sección `InvoiceTransitionsWorker:IntervalMinutes` y variable `InvoiceTransitionsWorker__IntervalMinutes`) que produce el valor configurado (C-08, SC-005).
- [X] T012 [P] [US2] En `InvoiceTransitionsWorkerOptionsTests.cs`: test de validación (valor `<= 0` o inválido cae al default y se considera advertencia).

### Implementation for User Story 2

- [X] T013 [P] [US2] Crear `backend/Infrastructure/Workers/InvoiceTransitionsWorkerOptions.cs` con `IntervalMinutes` (default 60) y `RunOnStartup` (default true), más un método/propiedad que resuelva el `TimeSpan` efectivo y normalice valores inválidos al default.
- [X] T014 [US2] Modificar `backend/Infrastructure/Workers/InvoiceTransitionsWorker.cs`: inyectar `IOptions<InvoiceTransitionsWorkerOptions>`, reemplazar la constante `RunInterval` por el intervalo de las opciones, registrar en el log de arranque el intervalo efectivo y respetar `RunOnStartup`.
- [X] T015 [US2] Modificar `backend/Infrastructure/Configuration/DependencyInjection.cs`: enlazar `InvoiceTransitionsWorkerOptions` desde `IConfiguration` (`services.Configure<>(configuration.GetSection("InvoiceTransitionsWorker"))`) antes de registrar el hosted service.

**Checkpoint**: US1 + US2 funcionan de forma independiente (intervalo configurable activo).

---

## Phase 5: User Story 3 — Trazabilidad de Cada Ejecución (Priority: P3)

**Goal**: Cada ejecución produce un resumen estructurado consultable que incluye el conteo de errores, incluso sin transiciones.

**Independent Test**: Ejecutar un ciclo (con y sin candidatos, con y sin errores) y verificar el resumen `Timestamp/Evaluated/Transitioned/Errors/DurationMs`.

### Tests for User Story 3 (escribir primero, deben FALLAR) ⚠️

- [X] T016 [P] [US3] En `InvoiceTransitionsWorkerCycleTests.cs`: test `RunCycle_RepositorioVacio_ResumenEnCeros` (`Evaluated=0`, `Transitioned=0`, `Errors=0`) (C-06).
- [X] T017 [P] [US3] En `InvoiceTransitionsWorkerCycleTests.cs`: test que verifica que tras un ciclo con un fallo aislado el conteo de errores refleja `Errors=1` y las transiciones exitosas se cuentan aparte (C-04). Usar un sink de Serilog en memoria o exponer el resultado del ciclo para aserción.

### Implementation for User Story 3

- [X] T018 [US3] Modificar `backend/Infrastructure/Workers/InvoiceTransitionsWorker.cs` (`RunCycleAsync`): añadir el contador `errors` y emitir el resumen de fin de ciclo con `Timestamp`, `Evaluated`, `Transitioned`, `Errors` y `DurationMs` (FR-008). Si hace falta para aserción, devolver un `CycleResult` interno.
- [X] T019 [US3] Verificar/ajustar el log por transición en el worker (`InvoiceId`, estado anterior, estado nuevo) para confirmar C-05; ya existe, solo confirmar formato estructurado.
- [X] T020 [US3] Alinear el resumen del endpoint `backend/Api/Endpoints/Workers/TriggerTransitions.cs` para incluir también `errors` en la respuesta JSON y en el log, manteniendo consistencia con C-04.

**Checkpoint**: Las tres historias funcionan de forma independiente.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Cierre, consistencia y validación final.

- [X] T021 [P] Ejecutar `dotnet format` en `backend/` y resolver advertencias de estilo introducidas.
- [X] T022 Ejecutar la suite completa `dotnet test` en `backend/` y confirmar verde, incluyendo cobertura del worker (objetivo ≥85% en la lógica nueva).
- [X] T023 [P] Ejecutar la validación de `specs/012-worker-state-transitions/quickstart.md` (tests, endpoint de disparo y prueba del intervalo por variable de entorno).
- [X] T024 [P] Actualizar `roadmap.md` (Spec 3.2): marcar como cubiertos los criterios de "ejecución cada X minutos configurable", "transiciones automáticas" y "registro en logs (Serilog)".

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias.
- **Foundational (Phase 2)**: depende de Setup. BLOQUEA todas las historias (habilita pruebas del worker real).
- **User Stories (Phase 3-5)**: dependen de Foundational. Tras Foundational pueden abordarse en paralelo, pero todas editan `InvoiceTransitionsWorker.cs` → ver nota de conflicto abajo.
- **Polish (Phase 6)**: depende de las historias deseadas completadas.

### User Story Dependencies

- **US1 (P1)**: independiente; entrega el MVP (transiciones + aislamiento de errores).
- **US2 (P2)**: independiente de US1 en lógica (intervalo), pero T014 edita el mismo worker que T008.
- **US3 (P3)**: independiente; T018 edita el mismo worker que T008/T014.

### ⚠️ Nota de conflicto de archivo

`backend/Infrastructure/Workers/InvoiceTransitionsWorker.cs` es editado por **T008 (US1)**, **T014 (US2)** y **T018 (US3)**. Estas tres tareas **NO** son `[P]` entre sí: deben hacerse secuencialmente (recomendado en orden US1 → US2 → US3) o consolidarse en una única edición coordinada. Lo mismo aplica a `TriggerTransitions.cs` (T009 y T020).

### Within Each User Story

- Las pruebas se escriben y **fallan** antes de implementar.
- Opciones/fakes antes de la edición del worker.
- Historia completa antes de pasar a la siguiente prioridad.

### Parallel Opportunities

- T004, T005, T006 (pruebas US1) pueden escribirse en paralelo entre sí.
- T010, T011, T012 (pruebas US2) en paralelo entre sí.
- T016, T017 (pruebas US3) en paralelo entre sí.
- T013 (clase de opciones, archivo nuevo) puede hacerse en paralelo a las pruebas de US1.
- En Polish, T021/T023/T024 son paralelizables.

---

## Parallel Example: User Story 1

```text
# Escribir primero las pruebas de US1 (archivos/casos independientes):
Task: "T004 test transiciones elegibles en InvoiceTransitionsWorkerCycleTests.cs"
Task: "T005 test terminales no cambian en InvoiceTransitionsWorkerCycleTests.cs"
Task: "T006 test factura que falla no aborta el lote en InvoiceTransitionsWorkerCycleTests.cs"
```

---

## Implementation Strategy

### MVP First (solo US1)

1. Phase 1: Setup (T001)
2. Phase 2: Foundational (T002-T003) — CRÍTICO
3. Phase 3: US1 (T004-T009)
4. **DETENER y VALIDAR**: el worker transiciona y aísla errores por factura.
5. Demo/deploy si está listo.

### Incremental Delivery

1. Setup + Foundational → base lista
2. US1 → MVP (transiciones robustas)
3. US2 → intervalo configurable
4. US3 → trazabilidad con conteo de errores
5. Polish → formato, cobertura, quickstart, roadmap

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes.
- Las tres ediciones del worker (T008/T014/T018) comparten archivo: secuenciar.
- Verificar que las pruebas fallan antes de implementar (Red-Green-Refactor).
- Documentación y comentarios de requisitos en español (Constitución III).
- Commit por tarea o grupo lógico, referenciando la spec (p. ej. `feat(spec-3.2): intervalo configurable del worker`).
