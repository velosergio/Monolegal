# Fase 0 — Research: Tests de Componentes Frontend

**Feature**: 022-tests-componentes-frontend | **Fecha**: 2026-06-29

Este documento consolida las decisiones técnicas para cerrar la Spec 5.3. No quedan marcadores `NEEDS CLARIFICATION`: el stack de testing ya está fijado por la constitución y operativo en el repositorio.

## Estado actual de la suite (línea base)

- **Decisión**: Partir de la suite existente sin duplicar pruebas de render/interacción/async.
- **Evidencia**: `npm run test:run` → **48 archivos, 161 casos en verde** (2026-06-29). Existen tests de interacción (click/select) y de async con TanStack Query mockeado.
- **Hueco detectado**: `grep` de `toMatchSnapshot`/`toMatchInlineSnapshot` y de `__snapshots__/` → **0 resultados**. No hay ninguna prueba de snapshot. Varios componentes presentacionales (`StatusBadge`, `StatCard`, `LastRefreshIndicator`, `InvoiceDetailSkeleton`, `DashboardSkeleton`, `DashboardEmptyState`, `ShipmentsEmptyState`, `ShipmentsTableSkeleton`, `Footer`) no tienen prueba dedicada.
- **Conclusión**: El trabajo neto es (1) snapshots de UI crítica y (2) render/estructura de los componentes presentacionales sin cobertura.

## Decisión 1 — Mecanismo de snapshot

- **Decisión**: Usar el serializador de snapshots integrado de Vitest (`expect(...).toMatchSnapshot()`), almacenando los archivos en `__snapshots__/` versionados.
- **Rationale**: Ya viene con Vitest (sin nuevas dependencias). El snapshot serializa el DOM renderizado por Testing Library (`container.firstChild` o `asFragment()`), detectando regresiones de estructura, clases y textos. Coherente con el Principio IV (Vitest frontend).
- **Alternativas consideradas**:
  - *Inline snapshots* (`toMatchInlineSnapshot`): cómodos para fragmentos pequeños pero ensucian el archivo de test y dificultan revisar diffs grandes; se reservan, si acaso, para casos triviales.
  - *Snapshot de imagen / regresión visual* (Playwright/Storybook): fuera de alcance (Spec 5.4 cubre E2E); requiere navegador real y no aplica a componentes presentacionales aislados.

## Decisión 2 — Qué se considera "UI crítica" para snapshot

- **Decisión**: Limitar los snapshots a **componentes presentacionales deterministas** con dependencias acotadas: insignias de estado (`StatusBadge`, `ShipmentStatusBadge`), tarjeta de métrica (`StatCard`), estados vacíos (`InvoicesEmptyState`, `DashboardEmptyState`, `ShipmentsEmptyState`), esqueletos de carga (`InvoicesTableSkeleton`, `InvoiceDetailSkeleton`, `DashboardSkeleton`, `ShipmentsTableSkeleton`) y el pie (`Footer`).
- **Rationale**: Estos componentes definen la identidad visual y los estados base; su marcado es estable, sin lógica asíncrona ni estado interno complejo. Un snapshot aquí es señal de regresión, no fuente de ruido.
- **Alternativas consideradas**: Snapshot de páginas compuestas (`InvoicesPage`, `DashboardPage`): rechazado por ser frágil (dependen de providers, datos, animación y orden de render) y porque su comportamiento ya está cubierto por pruebas de interacción/async.

## Decisión 3 — Determinismo de los snapshots

- **Decisión**: Neutralizar toda fuente de indeterminismo dentro de la prueba:
  - `Footer` usa `new Date().getFullYear()` → **fijar la fecha del sistema** con `vi.setSystemTime(new Date('2026-01-01T00:00:00Z'))` (y `vi.useRealTimers()` al final) antes de tomar el snapshot.
  - Animación de Motion → el setup global ya stubea `matchMedia` (sin preferencia de movimiento reducido); para snapshots de componentes con `motion`, preferir variantes sin animación o fijar `prefers-reduced-motion: reduce`. Los componentes elegidos para snapshot **no** usan Motion (son presentacionales puros).
  - Sin `Math.random`, IDs autogenerados ni timestamps en los componentes seleccionados (verificado por lectura de cada componente).
- **Rationale**: Evita fallos intermitentes (SC-003). Un snapshot que cambia por la fecha o por un layout animado no aporta valor de regresión.
- **Alternativas consideradas**: Aceptar snapshots no deterministas y normalizar con serializers personalizados: añade complejidad innecesaria frente a fijar la fuente directamente.

## Decisión 4 — Snapshot + aserción legible (no solo snapshot)

- **Decisión**: Cada componente recibe **ambos**: aserciones legibles (roles/textos/variantes con Testing Library) y un snapshot. El snapshot no sustituye a las aserciones de comportamiento.
- **Rationale**: FR-004. El snapshot detecta regresiones amplias pero es opaco; las aserciones documentan la intención (qué etiqueta muestra cada estado, qué prop oculta el ícono, etc.) y fallan con mensajes claros.
- **Alternativas consideradas**: Solo snapshot → rechazado (oculta la intención, fomenta updates a ciegas). Solo aserciones → no cumple el criterio de snapshot de la Spec 5.3.

## Decisión 5 — Ubicación y convenciones

- **Decisión**: Snapshots en carpeta nueva `frontend/tests/components/snapshots/` con sus `__snapshots__/`. Pruebas de render/estructura nuevas junto a la convención existente por feature (`frontend/tests/features/<feature>/...`). Seguir convenciones: alias `@/`, `render`/`screen` de Testing Library, `describe`/`it` en español, reutilizar `tests/setup.ts`.
- **Rationale**: Separa la regresión por snapshot (que genera artefactos `__snapshots__/`) del resto, y mantiene coherencia con la organización ya presente en el repo.
- **Alternativas consideradas**: Co-ubicar snapshots junto a cada componente en `src/` (como hacen los tests de `shipments`): inconsistente con la mayoría de la suite, que vive en `frontend/tests/`.

## Riesgos y mitigaciones

| Riesgo | Mitigación |
|--------|------------|
| Snapshots intermitentes por fecha/animación | Fijar `vi.setSystemTime`; elegir componentes sin Motion; setup global ya estabiliza `matchMedia`/`ResizeObserver`. |
| Updates de snapshot a ciegas en CI | Política: nunca `--update` en CI; los diffs se revisan en PR (FR + Assumptions de la spec). |
| Biome marca los nuevos archivos | Ejecutar `npm run lint` y `npm run format` sobre los nuevos archivos antes de cerrar. |
| Cambios legítimos de UI rompen snapshots | Comportamiento esperado: la diferencia obliga a actualizar conscientemente el snapshot. |
