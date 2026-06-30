---
description: "Lista de tareas para la implementación de la feature Test Runner Unificado"
---

# Tasks: Test Runner Unificado

**Input**: Documentos de diseño en `/specs/024-test-runner-unificado/`

**Prerequisites**: plan.md (requerido), spec.md (historias de usuario), research.md, data-model.md, contracts/, quickstart.md

**Tests**: Esta feature **es** el ejecutor de pruebas; no se añaden pruebas automatizadas a las suites existentes (FR-011). Su propia corrección se valida mediante los escenarios del quickstart (V1–V6), incluidos como tareas de validación dentro de cada historia.

**Organization**: Tareas agrupadas por historia de usuario para permitir implementación y validación independientes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede correr en paralelo (archivo distinto, sin dependencias pendientes)
- **[Story]**: Historia de usuario a la que pertenece (US1, US2, US3)
- Se incluyen rutas de archivo exactas (relativas a la raíz del repo)

## Path Conventions

- Monorepo web. El tooling de orquestación vive en la **raíz**: `package.json` y `scripts/test-all.mjs`.
- Suites existentes invocadas (SIN cambios): `backend/`, `worker/`, `frontend/`.
- Workflow CI opcional: `.github/workflows/test-all.yml`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Crear el punto de entrada raíz y el esqueleto del orquestador.

- [X] T001 Crear `package.json` en la raíz del repo: `"private": true`, sin dependencias, con `"scripts": { "test:all": "node scripts/test-all.mjs", "test": "node scripts/test-all.mjs" }` y `"type": "module"` (research D1/D2).
- [X] T002 Crear el esqueleto del orquestador en `scripts/test-all.mjs`: módulo ESM de Node usando solo `node:child_process` y `node:process`, sin dependencias externas; estructura base (función `main()` invocada al final, sin lógica de suites aún).

**Checkpoint**: `npm run test:all` ejecuta el esqueleto sin error (aunque todavía no corra suites).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Definir las suites y el mecanismo de ejecución de un proceso hijo que todas las historias necesitan.

**⚠️ CRITICAL**: Ninguna historia puede completarse hasta terminar esta fase.

- [X] T003 En `scripts/test-all.mjs`, definir la lista declarativa `SUITES` con las cuatro `SuiteDefinition` (data-model.md): `backend` → `dotnet test` en `backend/`; `worker` → `dotnet test` en `worker/`; `frontend` → `npm run test:run` en `frontend/`; `e2e` → `npm run test:e2e` en `frontend/`.
- [X] T004 En `scripts/test-all.mjs`, implementar el helper `runSuite(def)` que lanza el proceso hijo en su `cwd`, mide `durationMs`, captura el `exitCode` y, ante fallo de spawn (binario ausente/error), devuelve un `SuiteResult` con `status: FAIL` y `error` (FR-010, data-model "regla de derivación").

**Checkpoint**: existen las definiciones de las 4 suites y un helper capaz de ejecutar una suite y devolver su `SuiteResult`.

---

## Phase 3: User Story 1 - Ejecutar todas las suites con un solo comando apto para CI (Priority: P1) 🎯 MVP

**Goal**: Un único comando ejecuta las cuatro suites y devuelve un código de salida agregado (0 solo si todas pasan; ≠0 si alguna falla).

**Independent Test**: Ejecutar `npm run test:all` en un entorno con las suites disponibles y verificar que corre las cuatro y que el código de salida es 0 si todas pasan y ≠0 si alguna falla (quickstart V1–V4).

### Implementation for User Story 1

- [X] T005 [US1] En `scripts/test-all.mjs`, implementar el bucle **secuencial** que recorre `SUITES` y ejecuta cada una con `runSuite()`, acumulando los `SuiteResult` en un arreglo `results`; **no** fail-fast: continúa aunque una suite falle (FR-009, research D4).
- [X] T006 [US1] En `scripts/test-all.mjs`, construir el `RunReport` (`allPassed = results.every(PASS)`) y terminar con `process.exit(allPassed ? 0 : 1)` — código de salida agregado coherente (FR-003, FR-004, FR-006).
- [X] T007 [US1] Validar la historia con los escenarios del quickstart V1 (todas en verde → exit 0), V2 (una falla → exit ≠0) y V3 (no fail-fast: todas se ejecutan), comprobando el código de salida en bash y PowerShell.

**Checkpoint**: El MVP funciona: un comando corre las 4 suites y el exit code agregado es correcto y apto para CI.

---

## Phase 4: User Story 2 - Reporte consolidado del resultado por suite (Priority: P2)

**Goal**: Al final de la corrida se imprime un resumen con PASS/FAIL por suite (backend, worker, frontend, e2e) y un veredicto global coherente con el código de salida.

**Independent Test**: Tras una corrida con al menos una suite fallando, el resumen final lista las cuatro suites con su veredicto individual y coincide con el resultado real (quickstart V2).

### Implementation for User Story 2

- [X] T008 [US2] En `scripts/test-all.mjs`, imprimir antes de cada suite un encabezado (`[n/4] <label>`) y heredar el stdio del proceso hijo para hacer streaming en vivo de su salida (research D6, observabilidad — Principio VI).
- [X] T009 [US2] En `scripts/test-all.mjs`, imprimir al final el bloque resumen consolidado: una línea por suite con `PASS`/`FAIL` y `durationMs`, más la línea de veredicto global; el texto en español (FR-005, SC-004, Principio III). Garantizar coherencia resumen↔exit code (FR-006).
- [X] T010 [US2] Validar la historia con quickstart V2 (resumen marca la suite fallida como FAIL y el resto PASS) y confirmar que el veredicto global del resumen concuerda con el código de salida.

**Checkpoint**: La salida es accionable: resumen PASS/FAIL por suite + veredicto global, coherente con el exit code.

---

## Phase 5: User Story 3 - Ejecución multiplataforma (Windows/Linux) (Priority: P3)

**Goal**: El mismo comando funciona de forma equivalente en Windows y Linux, sin scripts por plataforma, y queda demostrado en CI.

**Independent Test**: Ejecutar `npm run test:all` en Windows y en Linux sobre el mismo estado y verificar que ejecuta las mismas suites con formato de resumen y semántica de exit code equivalentes (quickstart V5, V6).

### Implementation for User Story 3

- [X] T011 [US3] En `scripts/test-all.mjs`, asegurar el spawn multiplataforma del comando `npm` (en Windows el ejecutable es `npm.cmd`; usar `shell: true` o resolución condicional por `process.platform`) y verificar que `dotnet` se invoca igual en ambos SO; sin rutas ni separadores específicos de un SO (FR-007).
- [X] T012 [P] [US3] Crear `.github/workflows/test-all.yml`: configurar .NET (`global-json-file: global.json`) y Node (`.node-version`), servicio Mongo (patrón de `backend.yml`), `npm ci` en `frontend/`, `npx playwright install --with-deps`, levantar el backend en Development (:5155) y ejecutar `npm run test:all`; el job falla si el comando retorna ≠0 (research D8, CI Gate Principio IV).
- [X] T013 [US3] Validar la historia con quickstart V5 (paridad Windows/Linux) y V6 (no interactivo / apto para CI, sin TTY).

**Checkpoint**: El comando único es multiplataforma y está integrado en CI como gate.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Conveniencias opcionales y documentación, sin alterar el flujo por defecto (las 4 suites).

- [X] T014 [P] Añadir selección opcional de suites por argumentos/variable de entorno en `scripts/test-all.mjs` (`node scripts/test-all.mjs backend worker` o `SUITES=backend,worker`), manteniendo el default = las cuatro suites (research D9, contracts "Argumentos opcionales").
- [X] T015 [P] Documentar el comando único en `README.md` (sección de pruebas: `npm run test:all`, prerrequisitos, exit codes) enlazando al quickstart de la feature.
- [X] T016 Ejecutar la validación completa del quickstart (V1–V6) de extremo a extremo y confirmar SC-001…SC-007 (incluye SC-005 paridad y SC-007 sin cambios en producción).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias — empieza de inmediato. T002 depende de T001 (mismo flujo de creación del entry point).
- **Foundational (Phase 2)**: depende de Setup — **BLOQUEA** todas las historias.
- **User Stories (Phase 3+)**: dependen de Foundational. Por compartir un único archivo (`scripts/test-all.mjs`), las historias se implementan **en orden de prioridad** (P1 → P2 → P3) en lugar de en paralelo.
- **Polish (Phase 6)**: depende de las historias deseadas completas (T014/T015 pueden adelantarse por tocar archivos distintos).

### User Story Dependencies

- **US1 (P1)**: arranca tras Foundational. Sin dependencias de otras historias. Es el MVP.
- **US2 (P2)**: arranca tras Foundational. Mejora la salida de US1; mantiene el exit code de US1 intacto.
- **US3 (P3)**: arranca tras Foundational. Asegura paridad multiplataforma y CI; no cambia la semántica de US1/US2.

### Within Each User Story

- US1: T005 (bucle) → T006 (agregado/exit) → T007 (validación).
- US2: T008 (streaming) y T009 (resumen) → T010 (validación).
- US3: T011 (spawn multiplataforma) y T012 (CI, [P]) → T013 (validación).

### Parallel Opportunities

- La mayoría de tareas editan `scripts/test-all.mjs` → **no** son paralelizables entre sí.
- Tareas en archivos distintos sí son `[P]`: **T012** (`.github/workflows/test-all.yml`), **T014** (puede separarse), **T015** (`README.md`).

---

## Parallel Example: Phase 6 (Polish)

```bash
# Tareas en archivos distintos, ejecutables en paralelo:
Task: "Crear .github/workflows/test-all.yml"          # T012 (si aún no hecho)
Task: "Documentar npm run test:all en README.md"      # T015
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Completar Phase 1 (Setup) y Phase 2 (Foundational).
2. Completar Phase 3 (US1): comando único + exit code agregado.
3. **PARAR y VALIDAR**: quickstart V1–V4. Ya es usable como gate básico.

### Incremental Delivery

1. Setup + Foundational → base lista.
2. US1 → exit code agregado (MVP) → validar.
3. US2 → reporte consolidado legible → validar.
4. US3 → paridad multiplataforma + CI → validar.
5. Polish → selección opcional de suites + docs.

---

## Notes

- `[P]` = archivos distintos, sin dependencias. La concentración de lógica en `scripts/test-all.mjs` limita el paralelismo dentro de las historias (es esperado en una utilidad de un solo archivo).
- No se añaden ni modifican pruebas de las suites ni código de producción (FR-011 / SC-007).
- Validar cada historia con su escenario de quickstart antes de pasar a la siguiente prioridad.
- Una suite no ejecutable cuenta como FAIL, nunca como PASS (FR-010).

## Notas de implementación (decisiones no obvias)

- **Spawn multiplataforma**: en Windows los lanzadores `npm`/`npx` son scripts `.cmd` que Node ya no permite ejecutar sin shell (`EINVAL`, endurecimiento CVE-2024-27980). Se usa `shell: true` pasando la **línea de comando completa como una sola cadena** (no un arreglo `args`) para evitar además el aviso de deprecación `DEP0190`. `dotnet` se invoca igual en ambos SO.
- **stdin ignorado** (`stdio: ['ignore', 'inherit', 'inherit']`): heredar stdin provocaba que la suite de frontend (Vitest) **terminara antes de tiempo con código ≠ 0** en ejecución no interactiva (se observó FAIL a ~30–74s frente al PASS real a ~90s). Ignorar stdin reproduce el comportamiento no interactivo correcto (FR-008) manteniendo el streaming de stdout/stderr (FR-005). Verificado: 61 archivos / 191 pruebas → PASS, exit 0.
