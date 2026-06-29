---
description: "Task list for feature implementation"
---

# Tasks: Tests de Componentes Frontend

**Input**: Design documents from `/specs/022-tests-componentes-frontend/`

**Prerequisites**: plan.md (requerido), spec.md (requerido), research.md, data-model.md, contracts/component-test-matrix.md, quickstart.md

**Tests**: Esta feature **es** una suite de pruebas de componentes (Principio IV). Las tareas de implementación consisten en escribir tests de Vitest + Testing Library (render/estructura y snapshot); no hay código de producción que añadir.

**Organization**: Las tareas se agrupan por historia de usuario (US1–US3). US1 (snapshots de UI crítica) es el MVP que cierra el criterio pendiente de la Spec 5.3. Cada componente/archivo es independiente para habilitar paralelismo.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivo distinto, sin dependencias pendientes)
- **[Story]**: Historia de usuario a la que pertenece (US1–US3)
- Rutas de archivo exactas incluidas en cada descripción

## Path Conventions

- Proyecto frontend: `frontend/`
- Setup global compartido (sin cambios): `frontend/tests/setup.ts`
- Pruebas de snapshot (NUEVO): `frontend/tests/components/snapshots/`
- Snapshots generados (versionados): `frontend/tests/components/snapshots/__snapshots__/`
- Componentes bajo prueba (sin cambios): `frontend/src/features/**/components/`, `frontend/src/components/layout/`
- Ejecución: `npm run test:run` desde `frontend/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Preparar la estructura de la suite de snapshots sin tocar código de producción

- [X] T001 Crear la carpeta `frontend/tests/components/snapshots/` y verificar línea base verde ejecutando `npm run test:run` desde `frontend/` (48 archivos / 161 casos en verde antes de añadir nada)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: No hay infraestructura nueva que construir; el setup global (`frontend/tests/setup.ts`) ya provee los polyfills (Radix, Motion `matchMedia`, `localStorage`) y no se modifica.

- [X] T002 Confirmar que `frontend/tests/setup.ts` cubre los polyfills necesarios para los componentes presentacionales (no requiere cambios) y documentar en el PR que el setup se reutiliza tal cual

**Checkpoint**: Infraestructura lista — las historias de usuario pueden comenzar

---

## Phase 3: User Story 1 - Regresión por snapshot de la UI crítica (Priority: P1) 🎯 MVP

**Goal**: Establecer pruebas de snapshot deterministas para los componentes presentacionales críticos, cerrando el único criterio pendiente de la Spec 5.3.

**Independent Test**: Ejecutar `npm run test:run -- tests/components/snapshots`: la primera corrida genera los snapshots en `__snapshots__/`; una segunda corrida sin cambios pasa sin diferencias; alterar el marcado de un componente hace fallar su snapshot.

### Implementation for User Story 1

- [X] T003 [P] [US1] Crear `frontend/tests/components/snapshots/StatusBadge.snapshot.test.tsx` con snapshot de `StatusBadge` para un estado conocido (`pagado`) y para un estado desconocido (rama neutra)
- [X] T004 [P] [US1] Crear `frontend/tests/components/snapshots/ShipmentStatusBadge.snapshot.test.tsx` con snapshot de `ShipmentStatusBadge` para un estado conocido (`sent`)
- [X] T005 [P] [US1] Crear `frontend/tests/components/snapshots/StatCard.snapshot.test.tsx` con snapshot de `StatCard` en variante con ícono y sin ícono
- [X] T006 [P] [US1] Crear `frontend/tests/components/snapshots/EmptyStates.snapshot.test.tsx` con snapshots de `InvoicesEmptyState`, `DashboardEmptyState` y `ShipmentsEmptyState` (ambas ramas `filtered`)
- [X] T007 [P] [US1] Crear `frontend/tests/components/snapshots/Skeletons.snapshot.test.tsx` con snapshots de `InvoicesTableSkeleton` (`rows={2}`), `InvoiceDetailSkeleton`, `DashboardSkeleton` y `ShipmentsTableSkeleton` (`rows={2}`)
- [X] T008 [US1] Crear `frontend/tests/components/snapshots/Footer.snapshot.test.tsx` con snapshot de `Footer` expandido y colapsado, fijando la fecha del sistema con `vi.setSystemTime(new Date('2026-01-01T00:00:00Z'))` y restaurándola con `vi.useRealTimers()` (FR-002, determinismo)
- [X] T009 [US1] Ejecutar `npm run test:run -- tests/components/snapshots` para generar los snapshots, verificar que se crean en `frontend/tests/components/snapshots/__snapshots__/` y re-ejecutar para confirmar PASS sin diferencias

**Checkpoint**: La UI crítica tiene regresión por snapshot determinista; el criterio "Snapshot tests para UI crítica" de la Spec 5.3 queda cubierto.

---

## Phase 4: User Story 2 - Cobertura de render/estructura de componentes presentacionales sin pruebas (Priority: P2)

**Goal**: Que los componentes presentacionales sin prueba dedicada verifiquen render sin errores y contenido/estructura accesible con aserciones legibles (no solo snapshot).

**Independent Test**: Ejecutar los archivos de render nuevos: cada componente renderiza con props representativas y se afirma su contenido/rol/variante.

### Implementation for User Story 2

- [X] T010 [P] [US2] Crear `frontend/tests/features/invoices/StatusBadge.test.tsx`: estado conocido muestra etiqueta legible; estado desconocido muestra valor en bruto con estilo neutro
- [X] T011 [P] [US2] Crear `frontend/tests/features/dashboard/StatCard.test.tsx`: muestra `label` y `value`; el ícono opcional es decorativo (`aria-hidden="true"`)
- [X] T012 [P] [US2] Crear `frontend/tests/features/dashboard/DashboardEmptyState.test.tsx`: título "No hay facturas todavía" + descripción
- [X] T013 [P] [US2] Crear `frontend/tests/features/dashboard/DashboardSkeleton.test.tsx`: rol `status` con etiqueta "Cargando estadísticas"
- [X] T014 [P] [US2] Crear `frontend/tests/features/invoices/InvoiceDetailSkeleton.test.tsx`: `aria-hidden="true"` y estructura de 6 grupos de campos
- [X] T015 [P] [US2] Crear `frontend/tests/features/layout/Footer.test.tsx`: expandido muestra producto/versión/año (fecha fija); colapsado muestra solo la versión `v0.1.0`
- [X] T016 [P] [US2] Crear `frontend/tests/features/shipments/ShipmentsEmptyState.render.test.tsx`: `filtered=true` → "No se encontraron envíos"; `filtered=false` → "Aún no hay envíos"

**Checkpoint**: 0 componentes presentacionales críticos sin prueba dedicada (SC-002).

---

## Phase 5: User Story 3 - Verificación consolidada de los cuatro criterios (Priority: P3)

**Goal**: Confirmar que los cuatro criterios de la Spec 5.3 quedan respaldados y la suite completa pasa en verde, con trazabilidad documentada.

**Independent Test**: `npm run test:run` en verde + revisión del inventario criterio→prueba.

### Implementation for User Story 3

- [X] T017 [US3] Ejecutar `npm run test:run` (suite completa) y confirmar que todos los archivos pasan, incluidos los nuevos de snapshot y render
- [X] T018 [US3] Ejecutar `npm run test:run` por segunda vez consecutiva y confirmar cero diferencias de snapshot (SC-003, sin intermitencias)
- [X] T019 [US3] Verificar el inventario de trazabilidad de [contracts/component-test-matrix.md](./contracts/component-test-matrix.md): cada criterio del roadmap (render, interacciones, async, snapshots) tiene al menos una prueba

**Checkpoint**: Los cuatro criterios de la Spec 5.3 están cubiertos con evidencia.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Calidad y cierre

- [X] T020 [P] Ejecutar `npm run lint` y `npm run format` y corregir cualquier hallazgo de Biome en los archivos nuevos (Principio V)
- [X] T021 Confirmar que no se modificó ningún archivo de `frontend/src/**` de producción (`git status` solo muestra archivos de test y `__snapshots__/`) (FR-006, SC-005)
- [X] T022 Marcar la Spec 5.3 como implementada en `roadmap.md` (criterios ✅ y referencia a la feature 022)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: Sin dependencias — puede empezar de inmediato
- **Foundational (Phase 2)**: Trivial (reutiliza setup existente); no bloquea de forma real
- **User Stories (Phase 3+)**: Pueden comenzar tras Phase 1
  - US1 (P1) es el MVP y cierra el criterio pendiente
  - US2 (P2) y US3 (P3) complementan; US3 depende de que existan US1+US2 para la verificación consolidada
- **Polish (Phase 6)**: Tras completar las historias deseadas

### User Story Dependencies

- **US1 (P1)**: Independiente; entrega valor por sí sola (snapshots)
- **US2 (P2)**: Independiente de US1 (archivos distintos)
- **US3 (P3)**: Verificación; se ejecuta tras US1 y US2

### Within Each User Story

- Los archivos de test marcados [P] son independientes (archivos distintos) y pueden escribirse en paralelo
- T009 (generar snapshots) se ejecuta después de crear los archivos de snapshot de US1

### Parallel Opportunities

- US1: T003–T007 son [P] (archivos distintos); T008 toca un archivo propio pero requiere fijar timers
- US2: T010–T016 son todos [P] (archivos distintos)

---

## Parallel Example: User Story 1

```bash
# Escribir en paralelo los archivos de snapshot (archivos distintos):
Task: "StatusBadge.snapshot.test.tsx"
Task: "StatCard.snapshot.test.tsx"
Task: "EmptyStates.snapshot.test.tsx"
Task: "Skeletons.snapshot.test.tsx"
# Luego generar y validar snapshots:
npm run test:run -- tests/components/snapshots
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1: Setup (carpeta + línea base verde)
2. Phase 3: US1 (snapshots de UI crítica) → genera y valida snapshots
3. **PARAR y VALIDAR**: criterio de snapshot de la Spec 5.3 cubierto

### Incremental Delivery

1. US1 → snapshots (MVP, cierra el criterio pendiente)
2. US2 → render/estructura de componentes sin cobertura
3. US3 → verificación consolidada + trazabilidad
4. Polish → lint/format, confirmación de no-cambios en producción, roadmap

---

## Notes

- [P] = archivos distintos, sin dependencias
- Snapshots deterministas: fijar fecha en `Footer`; `rows` fijo en esqueletos; sin Motion en los componentes elegidos
- Sin `.skip`/`.only`; sin cambios en código de producción
- Descripciones de test en español; alias `@/`; reutilizar `tests/setup.ts`
- Nunca actualizar snapshots con `-u` en CI; revisar diffs en PR
