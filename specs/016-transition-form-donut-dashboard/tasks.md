---
description: "Lista de tareas para la implementación de la feature 016"
---

# Tasks: Formulario de Transición Manual de Estado, Dashboard como Inicio y Gráfico Donut por Estado

**Input**: Documentos de diseño en `specs/016-transition-form-donut-dashboard/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ui-contracts.md, quickstart.md

**Tests**: INCLUIDOS. La Constitución (IV. Test-First, NO NEGOCIABLE) exige escribir las pruebas primero (Red-Green-Refactor) y ≥85% de cobertura. Todas las pruebas usan Vitest + Testing Library.

**Animaciones (Motion)**: por petición explícita, esta feature **añade animaciones con Motion** en cada superficie nueva o editada (entrada/salida de toasts, barrido del donut, entrada escalonada del dashboard), **respetando siempre `prefers-reduced-motion`** vía `useReducedMotion()` y los helpers de `lib/motion.ts`.

**Organización**: tareas agrupadas por historia de usuario para implementación y prueba independientes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: Historia de usuario a la que pertenece (US1, US2, US3)

## Path Conventions

- SPA por feature: código en `frontend/src/...`, pruebas co-ubicadas en `frontend/tests/...`. **Sin cambios de backend.**

---

## Phase 1: Setup (Infraestructura compartida de animación)

**Purpose**: Base de animaciones Motion reutilizable por toasts, donut y dashboard. Sin dependencias de runtime nuevas.

- [X] T001 [P] Extender `frontend/src/lib/motion.ts` con variantes Motion nuevas y sus transiciones reduce-motion-safe: `toastInOut` (entrada/salida de toasts), `donutSweep` (barrido del anillo vía `pathLength`/`stroke-dashoffset`) y `staggerContainer` + reuso de `fadeInUp` (entrada escalonada del dashboard). Todas deben colapsar a instantáneo cuando `useReducedMotion()` es `true` (reusar `REDUCED_TRANSITION`/`motionTransition`).
- [X] T002 [P] Añadir prueba de las nuevas variantes en `frontend/tests/a11y/reduced-motion.test.tsx`: verificar que con movimiento reducido las transiciones de `toastInOut`/`donutSweep`/`staggerContainer` resuelven a duración 0 (no animan), y que sin reducción definen duración > 0.

---

## Phase 2: Foundational (Sistema de *toast* — prerrequisito bloqueante de US1)

**⚠️ CRITICAL**: El sistema de toast es infraestructura transversal montada en la raíz de la app. Debe completarse antes de US1 (que lo consume). No bloquea US2/US3, pero el montaje del provider toca `App.tsx` (coordinar con US2).

**Tests (escribir primero, deben FALLAR)** ⚠️

- [X] T003 [P] Crear `frontend/tests/components/feedback/Toast.test.tsx`: el `success` se anuncia en región `role="status"`/`aria-live="polite"` y auto-cierra (~4s, con timers falsos); el `error` se anuncia en `role="alert"`/`aria-live="assertive"` y **persiste** hasta cierre manual; botón de cierre con `aria-label` operable por teclado; con `prefers-reduced-motion` aparece/desaparece sin animación pero sigue anunciándose; `useToast` fuera de `ToastProvider` lanza error.

**Implementación**

- [X] T004 [P] Crear tipos y contexto del toast (`ToastVariant`, `ToastMessage`, `ToastApi`) y el `ToastProvider` (cola + `show`/`dismiss` + programación de auto-cierre del éxito) en `frontend/src/components/feedback/ToastProvider.tsx` (ver data-model §1).
- [X] T005 [P] Crear el hook `useToast(): ToastApi` (`success`/`error`/`dismiss`; lanza si se usa fuera del provider) en `frontend/src/components/feedback/useToast.ts` (depende de T004).
- [X] T006 [US-shared] Crear `ToastViewport` con **Motion + `AnimatePresence`** (variante `toastInOut`, `useReducedMotion`), regiones `aria-live` separadas para éxito/error, botón de cierre accesible y posición fija sin desbordamiento (móvil/escritorio) en `frontend/src/components/feedback/ToastViewport.tsx` (depende de T001, T004).
- [X] T007 Montar `<ToastProvider>` envolviendo las rutas en `frontend/src/App.tsx` y renderizar `<ToastViewport/>` una sola vez en `frontend/src/components/layout/AppShell.tsx` (depende de T004, T006).

**Checkpoint**: Sistema de toast accesible y animado disponible vía `useToast`.

---

## Phase 3: User Story 1 - Formulario de transición manual con toast (Priority: P1) 🎯 MVP

**Goal**: Cambiar el estado de una factura mediante un formulario con validación de cliente, confirmación, toast de éxito/error y actualización coherente de modal + listado, conservando el error inline persistente.

**Independent Test**: Abrir el modal de una factura con transiciones válidas; confirmar sin selección (validación, sin fetch); seleccionar y confirmar (toast éxito + estado/historial/listado actualizados); forzar rechazo (toast error + inline persistente, sin cambio de estado); factura terminal (formulario oculto/deshabilitado).

**Tests (escribir primero, deben FALLAR)** ⚠️

- [X] T008 [P] [US1] Actualizar `frontend/tests/features/invoices/ChangeStatusControl.test.tsx`: (a) submit sin selección → muestra mensaje de validación y **no** llama a la mutación/fetch; (b) durante `isPending` el select y el botón se deshabilitan y se evita el doble envío; (c) éxito → se invoca `toast.success` y se limpia la selección; (d) error → se invoca `toast.error` con el motivo y **persiste** el mensaje inline (`role="alert"`); (e) `allowedTransitions` vacío → no se renderiza el formulario.

**Implementación**

- [X] T009 [US1] Envolver el control en `<form onSubmit>` y añadir **validación de cliente** (estado `validationError`, mensaje "Selecciona un estado destino.", asociado al `Select` vía `aria-describedby`; `preventDefault` sin selección) en `frontend/src/features/invoices/components/ChangeStatusControl.tsx`.
- [X] T010 [US1] Integrar `useToast`: en `onSuccess` → `toast.success("Estado actualizado a «<etiqueta>».")` + limpiar selección/validación; en `onError` → `toast.error(<motivo> || genérico)` conservando el mensaje inline persistente existente, en `frontend/src/features/invoices/components/ChangeStatusControl.tsx` (depende de T009 y del sistema de toast).
- [X] T011 [P] [US1] Añadir microinteracción Motion al confirmar (p. ej. transición sutil del bloque de control / feedback de "ocupado") con `useReducedMotion` en `frontend/src/features/invoices/components/ChangeStatusControl.tsx`, sin introducir saltos de layout (FR-024/FR-025).

**Checkpoint**: MVP — el cambio de estado manual funciona con validación, toasts y coherencia de datos.

---

## Phase 4: User Story 2 - El dashboard es la pantalla de inicio (Priority: P2)

**Goal**: La ruta raíz `"/"` muestra el dashboard; `/dashboard` se elimina; rutas desconocidas → `"/"`; la navegación resalta "Dashboard" en la raíz. Entrada del dashboard animada con Motion.

**Independent Test**: Abrir `"/"` → dashboard; "Dashboard" activo solo en `"/"`; `/dashboard` y rutas desconocidas → `"/"`; Facturas/Configuración intactas.

**Tests (escribir primero, deben FALLAR)** ⚠️

- [X] T012 [P] [US2] Actualizar `frontend/tests/App.test.tsx`: `"/"` renderiza el Dashboard (no el listado); navegar a `/dashboard` (eliminada) y a una ruta inexistente redirige a `"/"`; `/facturas` y `/configuracion` siguen accesibles.
- [X] T013 [P] [US2] Actualizar `frontend/tests/components/layout/Navigation.dashboard.test.tsx`: el ítem "Dashboard" apunta a `"/"` y queda activo en `"/"` pero **no** en `/facturas` ni `/configuracion`.

**Implementación**

- [X] T014 [US2] Editar `frontend/src/App.tsx`: `"/"` → `DashboardPage` (lazy, fallback `DashboardSkeleton`); **eliminar** `<Route path="/dashboard">`; `<Route path="*">` → `Navigate to="/"` (depende de T007 para no perder el `ToastProvider`).
- [X] T015 [P] [US2] Editar `frontend/src/components/layout/navigation.ts`: el ítem "Dashboard" pasa a `to: '/'`.
- [X] T016 [P] [US2] Editar `frontend/src/components/layout/Sidebar.tsx`: el `NavLink` con `to === '/'` usa `end` para el resaltado activo exacto.
- [X] T017 [US2] Añadir **entrada escalonada con Motion** al contenido del dashboard (tarjetas + gráficos) usando `staggerContainer`/`fadeInUp` y `useReducedMotion` en `frontend/src/features/dashboard/components/DashboardPage.tsx` (depende de T001).

**Checkpoint**: El inicio es el dashboard, con navegación coherente y entrada animada.

---

## Phase 5: User Story 3 - Distribución por estado como gráfico de dona con colores (Priority: P2)

**Goal**: Reemplazar el gráfico por estado por un **donut** SVG + Motion con un color por estado coherente con `StatusBadge`, total en el centro, leyenda accesible y casos vacío/único/desconocido.

**Independent Test**: En el dashboard con datos, la distribución por estado es un donut con segmento+color por estado, total en el centro, leyenda color↔estado↔valor; vacío → centro `0`; un solo estado → anillo completo; entrada animada y reduce-motion-safe.

**Tests (escribir primero, deben FALLAR)** ⚠️

- [X] T018 [P] [US3] Actualizar `frontend/tests/features/dashboard/Charts.test.tsx`: la distribución por estado renderiza un donut con un segmento por estado y color coherente; el centro muestra el total con etiqueta "Total"; la leyenda asocia color↔estado↔valor (assert por texto/valor); `total === 0` → centro `0` y sin gráfico roto; un único estado → anillo completo; estado desconocido → color neutro; el gráfico por cliente sigue siendo de barras.

**Implementación**

- [X] T019 [P] [US3] Crear el mapa de colores `STATUS_CHART_CLASSES` + `statusChartClass()` (coherente con `STATUS_CLASSES` de `StatusBadge`, con dark mode y neutro para desconocidos) en `frontend/src/features/dashboard/statusChartColors.ts` (ver data-model §3).
- [X] T020 [US3] Crear `DonutChart` (SVG `role="img"`; pista + segmentos por `stroke-dasharray`; **barrido con Motion** `donutSweep` + `useReducedMotion`; total + `centerLabel` en el centro; leyenda `<ul>` accesible; casos vacío/único/desconocido) en `frontend/src/features/dashboard/components/DonutChart.tsx` (depende de T001).
- [X] T021 [US3] Editar `frontend/src/features/dashboard/components/StatusDistributionChart.tsx` para delegar en `DonutChart` (ariaLabel "Distribución de facturas por estado", `centerLabel="Total"`) en lugar de `BarChart` (depende de T020).
- [X] T022 [US3] Editar `frontend/src/features/dashboard/components/DashboardPage.tsx` para enriquecer `statusData` con `color: statusChartClass(status)` y pasar `total = stats.totalInvoices` al gráfico por estado (depende de T019, T021).

**Checkpoint**: El dashboard muestra la distribución por estado como donut con colores, total central y leyenda accesible.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Accesibilidad, calidad y verificación final.

- [X] T023 [P] Ampliar `frontend/tests/a11y/keyboard-navigation.test.tsx`: operar el formulario de transición, cerrar toasts y navegar al inicio solo con teclado (foco visible).
- [X] T024 [P] Verificar en `frontend/package.json` que **no** se añadieron dependencias de runtime (toast y donut in-house).
- [X] T025 Ejecutar `npm run lint` (Biome) en `frontend/` y corregir el 100% de hallazgos.
- [X] T026 Ejecutar `npm run build` (`tsc -b` strict + `vite build`) en `frontend/` y resolver cualquier error de tipos.
- [X] T027 Ejecutar `npm run doctor` (React Doctor) en `frontend/` y alcanzar **100/100 honesto** (sin supresiones; FR-021/SC-010). **Estado**: **100/100 en `--scope changed`** (sin errores ni *warnings*). Se completó la migración de Motion a `LazyMotion` + `m` (patrón `features={domAnimation}`) en los 5 componentes que aún usaban `import { motion }` directo: `ChangeStatusControl`, `ToastViewport`, `BarChart`, `DonutChart` y `DashboardPage` — eliminando por completo la familia `use-lazy-motion` (5 → 0 *warnings*; full-scope subió 83 → 87). Lint (Biome) limpio, `tsc -b` + `vite build` OK y 112/112 pruebas verdes. Sin supresiones aplicadas. (Nota: los *warnings* full-scope restantes son hallazgos preexistentes ajenos a esta feature: `prefer-tag-over-role`, `rerender-lazy-ref-init`, `unused-export`, `js-combine-iterations`, `no-react19-deprecated-apis`.)
- [ ] T028 Ejecutar la validación manual de `specs/016-transition-form-donut-dashboard/quickstart.md` (escenarios A–D) en claro/oscuro y móvil/escritorio. **Pendiente**: validación manual humana en navegador (claro/oscuro, móvil/escritorio). Cobertura automatizada equivalente verde (112 pruebas).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias — empieza de inmediato. T001 habilita las animaciones de las fases siguientes.
- **Foundational (Phase 2)**: depende de T001 (variantes Motion). Bloquea US1.
- **US1 (Phase 3)**: depende de Phase 2 (sistema de toast).
- **US2 (Phase 4)**: depende de Phase 2 solo por el orden de edición de `App.tsx` (T007 antes que T014); funcionalmente independiente de US1/US3.
- **US3 (Phase 5)**: depende de T001 (variante `donutSweep`); independiente de US1/US2.
- **Polish (Phase 6)**: depende de que estén completas las historias deseadas.

### User Story Dependencies

- **US1 (P1)**: requiere el sistema de toast (Phase 2). MVP.
- **US2 (P2)**: independiente de US1/US3 (coordinar edición de `App.tsx`).
- **US3 (P2)**: independiente de US1/US2.

### Within Each User Story

- Las pruebas se escriben y FALLAN antes de implementar.
- Tipos/colores antes de componentes; componentes antes de su integración en `DashboardPage`/`App`.

### Parallel Opportunities

- T001 y T002 en paralelo (Setup).
- En Phase 2: T003 (test) puede escribirse en paralelo a T004/T005; T006 tras T004; T007 al final.
- US2 y US3 pueden desarrollarse en paralelo tras Phase 1 (distintos archivos), coordinando solo `DashboardPage.tsx` (T017 vs T022) y `App.tsx`.
- Tareas marcadas [P] dentro de una historia tocan archivos distintos.

---

## Parallel Example: User Story 3

```bash
# Primero la prueba (debe fallar):
Task: "Actualizar Charts.test.tsx con asserts del donut (segmentos, colores, centro, leyenda, vacío/único/desconocido)"

# Luego, en paralelo (archivos distintos):
Task: "Crear statusChartColors.ts (T019)"
Task: "Crear DonutChart.tsx con Motion donutSweep (T020)"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 (Setup: variantes Motion) → Phase 2 (Foundational: sistema de toast) → Phase 3 (US1).
2. **STOP y VALIDAR**: cambio de estado con validación + toasts + coherencia.
3. Demo del MVP.

### Incremental Delivery

1. Setup + Foundational → base lista (toasts + animaciones).
2. US1 (P1) → validar → demo (MVP).
3. US2 (P2) → inicio = dashboard, navegación + entrada animada → validar → demo.
4. US3 (P2) → donut por estado con colores → validar → demo.
5. Polish → a11y, React Doctor 100/100, build, quickstart.

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes.
- Todas las animaciones nuevas usan Motion y **respetan `prefers-reduced-motion`** (FR-025/SC-008).
- Sin cambios de backend ni de contrato de API; sin dependencias de runtime nuevas.
- Verificar que las pruebas fallan antes de implementar; commit por tarea o grupo lógico.
- Mantener TypeScript strict (sin `any`) y Biome 100% compliant.
