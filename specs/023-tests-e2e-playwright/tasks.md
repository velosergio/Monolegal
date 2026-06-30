---
description: "Task list for feature implementation"
---

# Tasks: Tests E2E con Playwright

**Input**: Design documents from `/specs/023-tests-e2e-playwright/`

**Prerequisites**: plan.md (requerido), spec.md (requerido), research.md, data-model.md, contracts/e2e-flow-matrix.md, quickstart.md

**Tests**: Esta feature **es** la suite E2E (Principio IV — "E2E Tests: jornadas críticas del usuario"). Las tareas de implementación consisten en escribir configuración, fixtures y specs de Playwright; no hay código de producción que añadir ni modificar.

**Organization**: Las tareas se agrupan por historia de usuario (US1–US3). US1 (lista + filtro) es el MVP que verifica el punto de entrada de la jornada crítica. El backend se consume como caja negra (Development, con seeder idempotente y endpoint de flush ya existentes).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivo distinto, sin dependencias pendientes)
- **[Story]**: Historia de usuario a la que pertenece (US1–US3)
- Rutas de archivo exactas incluidas en cada descripción

## Path Conventions

- Proyecto frontend: `frontend/`
- Suite E2E (NUEVO): `frontend/e2e/`
- Configuración Playwright (NUEVO): `frontend/playwright.config.ts`
- Fixtures/helpers (NUEVO): `frontend/e2e/fixtures/`, `frontend/e2e/pages/`
- Backend caja negra (sin cambios): `http://localhost:5155` (Development) + `POST /api/settings/maintenance/flush-database`
- Ejecución: `npm run test:e2e` desde `frontend/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Instalar Playwright y crear la estructura de la suite E2E sin tocar código de producción

- [X] T001 Añadir `@playwright/test` como devDependency en `frontend/package.json` (`npm install -D @playwright/test`) e instalar navegadores (`npx playwright install`)
- [X] T002 Añadir el script `"test:e2e": "playwright test"` a `frontend/package.json` (scripts)
- [X] T003 Crear `frontend/playwright.config.ts`: `testDir: 'e2e'`, `baseURL: 'http://localhost:5173'`, `webServer` que levante el frontend (preview sobre build, con fallback a `dev`, `reuseExistingServer: true`), proyecto Chromium, y artefactos de diagnóstico (`trace: 'on-first-retry'`, `screenshot: 'only-on-failure'`, `video: 'retain-on-failure'`, reporters `list` + `html`) según research.md D2/D8
- [X] T004 [P] Excluir `frontend/e2e/**` de la config de Vitest (`frontend/vitest.config.ts`) para que Vitest no intente ejecutar los specs de Playwright (research.md D7)
- [X] T005 [P] Crear la carpeta `frontend/e2e/` con subcarpetas `fixtures/` y `pages/`, y añadir un `.gitignore`/config para artefactos de Playwright (`frontend/playwright-report/`, `frontend/test-results/`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Fixtures y helpers compartidos por todas las historias (reset de datos + localizadores accesibles). BLOQUEA las fases de historias.

**⚠️ CRITICAL**: Ninguna historia de usuario puede comenzar hasta completar esta fase

- [X] T006 Crear `frontend/e2e/fixtures/reset-data.ts`: helper `resetData(request)` que hace `POST http://localhost:5155/api/settings/maintenance/flush-database` y valida la respuesta `{ seeded: true, clientsCreated: 3, invoicesCreated: 8 }` (research.md D3, data-model.md §3)
- [X] T007 Crear `frontend/e2e/fixtures/test.ts`: extender `test` de `@playwright/test` con el fixture `resetData` y exponer `expect`; documentar el patrón de aislamiento (specs que mutan estado resetean en `beforeEach`/serial) según research.md D4
- [X] T008 [P] Crear `frontend/e2e/pages/invoices.page.ts`: page object con localizadores accesibles estables (filtro `getByLabel('Filtrar por estado')`, filas/badges de estado, botón `getByRole('button', { name: /Ver detalle de la factura de/ })`, select `getByLabel('Nuevo estado')`, botón `getByRole('button', { name: 'Cambiar Estado' })`) según contracts/e2e-flow-matrix.md y research.md D5
- [X] T009 [P] Crear `frontend/e2e/pages/dashboard.page.ts`: page object para el dashboard con lectura de conteos por estado (helper que devuelve un mapa estado→conteo para comparar por **delta**) y localizadores `getByText('Total de facturas')` y `getByLabel('Distribución de facturas por estado')` (research.md D6, data-model.md §3)

**Checkpoint**: Fixtures de reset y page objects listos — las historias de usuario pueden comenzar

---

## Phase 3: User Story 1 - Listar y filtrar facturas por estado (Priority: P1) 🎯 MVP

**Goal**: Verificar de extremo a extremo que la lista de facturas carga datos reales y que el filtro por estado devuelve únicamente las facturas correspondientes.

**Independent Test**: Ejecutar `npm run test:e2e -- invoices-list-filter.spec.ts` partiendo de datos sembrados: la vista carga facturas, filtrar por un estado deja solo ese estado y volver a "Todos los estados" restaura el listado.

### Implementation for User Story 1

- [X] T010 [US1] Crear `frontend/e2e/invoices-list-filter.spec.ts` con el caso 1.1 (carga de la lista): navegar a `/facturas`, afirmar el título "Facturas" y que la tabla tiene ≥1 fila, sin mensaje de error (contracts/e2e-flow-matrix.md F1, FR-002)
- [X] T011 [US1] Añadir el caso 1.2 (filtrar por estado): seleccionar "1er Recordatorio" en el filtro y afirmar que todas las filas visibles muestran ese badge (FR-003)
- [X] T012 [US1] Añadir el caso 1.3 (volver a todos): seleccionar "Todos los estados" y afirmar que se muestran facturas de múltiples estados (≥ las del caso 1.1) (FR-003)
- [X] T013 [US1] Añadir el caso 1.4 (sin resultados): como el seed pobla todos los estados filtrables, se ejercita el estado vacío del listado vía búsqueda de un cliente inexistente (mismo componente "No se encontraron facturas"), de forma determinista y sin mutar datos (FR-003, spec Edge Cases)
- [ ] T014 [US1] Ejecutar `npm run test:e2e -- invoices-list-filter.spec.ts` y confirmar PASS; usar `npx playwright show-report` si hay fallos para diagnosticar — ⚠️ PENDIENTE: requiere backend (`:5155`, Development) + MongoDB, no disponibles en el entorno actual. La config y los specs compilan (`playwright test --list` lista los 9 tests)

**Checkpoint**: El flujo "abrir lista + filtrar por estado" del roadmap queda verificado de extremo a extremo (SC-001). 

---

## Phase 4: User Story 2 - Realizar una transición manual de estado (Priority: P1)

**Goal**: Verificar que abrir el detalle de una factura no terminal, elegir un destino permitido y confirmar persiste el nuevo estado; y que un estado terminal no ofrece transición.

**Independent Test**: Ejecutar `npm run test:e2e -- manual-transition.spec.ts` tras reset+seed: la transición Pendiente→1er Recordatorio muestra confirmación y el nuevo estado; la factura `Pagado` no ofrece control.

### Implementation for User Story 2

- [X] T015 [US2] Crear `frontend/e2e/manual-transition.spec.ts` con `test.describe.serial` y `beforeEach` que invoca `resetData` (estado conocido, aislamiento — research.md D4)
- [X] T016 [US2] Añadir el caso 2.1 (destinos permitidos): abrir detalle de una factura `Pendiente`, abrir el select "Nuevo estado" y afirmar que las opciones son exactamente 1er Recordatorio y Pagado (FR-004, data-model.md §2)
- [X] T017 [US2] Añadir el caso 2.2 (aplicar transición no terminal): seleccionar "1er Recordatorio", pulsar "Cambiar Estado" y afirmar el toast "Estado actualizado a «1er Recordatorio»." y el nuevo estado en el detalle (FR-004, SC-006)
- [X] T018 [US2] Añadir el caso 2.4 (persistencia en lista): cerrar el modal, volver a la lista y afirmar que la factura figura con el estado actualizado (FR-004, SC-006)
- [X] T019 [US2] Añadir el caso 2.3 (estado terminal): abrir el detalle de la factura `Pagado` y afirmar que no hay control de transición y que se comunica que no admite cambios (FR-005)
- [ ] T020 [US2] Ejecutar `npm run test:e2e -- manual-transition.spec.ts` y confirmar PASS — ⚠️ PENDIENTE: requiere backend + MongoDB (no disponibles en el entorno actual)

**Checkpoint**: El flujo "transición manual" del roadmap queda verificado (incluido el caso terminal) (SC-001).

---

## Phase 5: User Story 3 - Ver el dashboard actualizado tras una transición (Priority: P2)

**Goal**: Verificar que, tras una transición, las métricas y la distribución por estado del dashboard reflejan el cambio (por delta), cerrando la jornada crítica.

**Independent Test**: Ejecutar `npm run test:e2e -- dashboard-updated.spec.ts` tras reset+seed: tras Pendiente→1er Recordatorio, el conteo de "Pendiente" baja 1 y el de "1er Recordatorio" sube 1; el total no cambia.

### Implementation for User Story 3

- [X] T021 [US3] Crear `frontend/e2e/dashboard-updated.spec.ts` con `test.describe.serial` y `beforeEach` que invoca `resetData` (research.md D4)
- [X] T022 [US3] Añadir el caso 3.1 (reflejo del cambio): leer la distribución por estado en `/` (page object dashboard), realizar la transición Pendiente→1er Recordatorio y, de vuelta en `/`, afirmar delta −1 en "Pendiente" y +1 en "1er Recordatorio" (FR-006, SC-006, research.md D6)
- [X] T023 [US3] Añadir el caso 3.2 (total coherente): afirmar que "Total de facturas" es igual antes y después de la transición (FR-006)
- [X] T024 [US3] Caso 3.3 (estado vacío): NO reproducible — el endpoint de flush siempre re-siembra (seeder idempotente), por lo que la BD nunca queda sin facturas. Documentado en el encabezado del spec `dashboard-updated.spec.ts` (omitido conscientemente, sin `.skip`); el estado vacío del dashboard ya está cubierto por pruebas de componente (Vitest, spec 022)
- [ ] T025 [US3] Ejecutar `npm run test:e2e -- dashboard-updated.spec.ts` y confirmar PASS — ⚠️ PENDIENTE: requiere backend + MongoDB (no disponibles en el entorno actual)

**Checkpoint**: El flujo "ver dashboard actualizado" del roadmap queda verificado; la jornada crítica completa está cubierta (SC-001).

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Calidad, determinismo y cierre

- [ ] T026 Ejecutar la suite completa `npm run test:e2e` y confirmar que todas las pruebas pasan con código de salida 0 (FR-009, SC-003) — ⚠️ PENDIENTE: requiere backend + MongoDB (no disponibles en el entorno actual)
- [ ] T027 Ejecutar `npm run test:e2e` por segunda vez consecutiva (con reset previo) y confirmar cero flakiness (SC-002); validar independencia de orden ejecutando un spec aislado (SC-004) — ⚠️ PENDIENTE: requiere backend + MongoDB (no disponibles en el entorno actual)
- [X] T028 [P] Ejecutar Biome (`biome check --write`) sobre `frontend/e2e/`, `frontend/playwright.config.ts` y `frontend/vitest.config.ts`: formateo aplicado (CRLF→LF) y `biome check` queda limpio (Principio V)
- [X] T029 [P] Verificado: no hay `.skip`/`.only` en los specs E2E (FR-012, Principio IV)
- [X] T030 Confirmado: esta feature solo añade `frontend/e2e/**`, `frontend/playwright.config.ts`, `frontend/package.json`/lockfile, `frontend/vitest.config.ts` y `.gitignore`; no se editó ningún `frontend/src/**` ni `backend/**`. (Nota: el working tree del repo ya tenía modificaciones preexistentes masivas ajenas a esta sesión —p. ej. normalización de fin de línea—, por lo que `git status` muestra muchos archivos `M` no causados por esta feature) (FR-010, SC-005)
- [ ] T031 Ejecutar la validación de [quickstart.md](./quickstart.md) de principio a fin (levantar backend, reset, `npm run test:e2e`) y confirmar resultados esperados — ⚠️ PENDIENTE: requiere backend + MongoDB (no disponibles en el entorno actual)
- [X] T032 Marcada la Spec 5.4 como implementada en `roadmap.md` (encabezado con referencia a la feature 023) y matriz de aceptación actualizada (Fase 5: 2/5)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sin dependencias — puede empezar de inmediato
- **Foundational (Phase 2)**: Depende de Setup; BLOQUEA todas las historias (fixtures + page objects)
- **User Stories (Phase 3–5)**: Dependen de Foundational
  - US1 (P1) es el MVP (solo lectura, no requiere reset dedicado)
  - US2 (P1) y US3 (P2) mutan estado: requieren `resetData` y ejecución serial
- **Polish (Phase 6)**: Tras completar las historias deseadas

### User Story Dependencies

- **US1 (P1)**: Independiente; solo lectura sobre datos sembrados
- **US2 (P1)**: Independiente; resetea su propio estado
- **US3 (P2)**: Independiente; resetea su propio estado y verifica agregados tras transición (reutiliza el patrón de transición de US2 pero en su propio spec)

### Within Each User Story

- El primer task de cada historia crea el archivo spec; los siguientes añaden casos al mismo archivo (secuenciales entre sí, mismo archivo)
- El último task de cada historia ejecuta y valida ese spec

### Parallel Opportunities

- Setup: T004 y T005 son [P] (archivos/áreas distintas)
- Foundational: T008 y T009 son [P] (page objects en archivos distintos); T006/T007 son secuenciales (T007 depende de T006)
- Entre historias: una vez completada la Phase 2, los specs de US1, US2 y US3 viven en archivos distintos y pueden escribirse por personas distintas; la **ejecución** de US2/US3 contra el backend compartido debe serializarse (BD única) — research.md D4

---

## Parallel Example: Foundational + arranque de historias

```bash
# Page objects en paralelo (archivos distintos):
Task: "frontend/e2e/pages/invoices.page.ts"
Task: "frontend/e2e/pages/dashboard.page.ts"

# Tras Phase 2, crear los specs base en paralelo (archivos distintos):
Task: "frontend/e2e/invoices-list-filter.spec.ts"
Task: "frontend/e2e/manual-transition.spec.ts"
Task: "frontend/e2e/dashboard-updated.spec.ts"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1: Setup (Playwright + config + estructura)
2. Phase 2: Foundational (reset fixture + page objects)
3. Phase 3: US1 (lista + filtro) → ejecutar y validar
4. **PARAR y VALIDAR**: el punto de entrada de la jornada crítica está cubierto

### Incremental Delivery

1. Setup + Foundational → infraestructura E2E lista
2. US1 → lista + filtro (MVP)
3. US2 → transición manual (+ caso terminal)
4. US3 → dashboard actualizado (cierra la jornada crítica)
5. Polish → determinismo, lint/format, no-cambios en producción, roadmap

### Parallel Team Strategy

1. El equipo completa Setup + Foundational juntos
2. Una vez listo Foundational:
   - Persona A: US1 (lista + filtro)
   - Persona B: US2 (transición manual)
   - Persona C: US3 (dashboard)
3. Los specs se escriben en paralelo; la ejecución contra el backend compartido se serializa (D4)

---

## Notes

- [P] = archivos distintos, sin dependencias
- El backend se trata como caja negra: no se modifica; se usa el seeder idempotente + endpoint de flush
- Determinismo (D6): aserciones del dashboard por **delta**, no por valores absolutos; localizadores por rol/etiqueta accesible (D5)
- Sin `.skip`/`.only`; sin cambios en código de producción (`frontend/src/**`, `backend/**`)
- Descripciones de prueba en español; nunca correr Playwright con actualización ciega de artefactos en CI
- La orquestación de un comando único backend+frontend+E2E pertenece a la Spec 5.5 (Test Runner Unificado), no a esta feature
