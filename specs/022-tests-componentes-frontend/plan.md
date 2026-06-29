# Implementation Plan: Tests de Componentes Frontend

**Branch**: `022-tests-componentes-frontend` | **Date**: 2026-06-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/022-tests-componentes-frontend/spec.md`

## Summary

Cerrar la **Spec 5.3 (Frontend Component Tests)** del roadmap añadiendo el único criterio que aún no se cumple —**snapshot tests para UI crítica**— y completando la cobertura de **render/estructura** de los componentes presentacionales que hoy no tienen prueba dedicada. La suite actual (48 archivos, 161 casos en verde) ya cubre render, interacciones simuladas (click/select) y manejadores asíncronos con TanStack Query, pero no existe ningún snapshot en el proyecto.

El enfoque: añadir pruebas con Vitest + Testing Library para un conjunto acotado de componentes **presentacionales deterministas** (insignias de estado, tarjeta de métrica, estados vacíos, esqueletos de carga, pie del sidebar). Cada uno recibe (a) aserciones legibles de render/estructura/variantes y (b) un snapshot determinista del marcado. Las fuentes de indeterminismo (fecha del sistema en `Footer`, animación de Motion, preferencia de movimiento) se neutralizan dentro de la prueba. No se modifica código de producción: solo se añaden archivos de test y de snapshot. Se cierra con un inventario de trazabilidad criterio→prueba y la suite completa en verde.

## Technical Context

**Language/Version**: TypeScript 6 (strict) / React 19 / Node 22+

**Primary Dependencies**: Vitest 4.1, `@testing-library/react` 16.3, `@testing-library/user-event` 14.6, `@testing-library/jest-dom` 6.9, jsdom 29. Serializador de snapshots integrado en Vitest (`toMatchSnapshot`).

**Storage**: N/A. Pruebas puras en memoria (jsdom); sin red ni base de datos.

**Testing**: Vitest + Testing Library; ejecución vía `npm run test:run` (`vitest run`) desde `frontend/`. Setup global compartido en `frontend/tests/setup.ts` (polyfills de Radix, Motion `matchMedia`, `localStorage` en memoria). Cobertura con `@vitest/coverage-v8`.

**Target Platform**: Frontend React 19 + Vite, ejecutado en jsdom para pruebas (Windows/Linux).

**Project Type**: Web application (frontend React + backend ASP.NET). Esta feature es exclusivamente frontend.

**Performance Goals**: La suite del frontend se ejecuta en decenas de segundos en máquina de desarrollo; los nuevos tests son ligeros (render presentacional sin E/S). Objetivo: que las nuevas pruebas no añadan tiempo perceptible (cada archivo < 1 s).

**Constraints**: Snapshots **deterministas** (sin fechas/aleatoriedad/animación variable); sin pruebas omitidas (`.skip`/`.only`) conforme al Principio IV; sin tocar código de producción; documentación y descripciones de test en español; respetar convenciones existentes (alias `@/`, setup global, Testing Library con roles accesibles).

**Scale/Scope**: ~8–10 componentes presentacionales bajo prueba; ~15–22 casos nuevos (render + variantes + snapshot) repartidos por componente/feature. Sin nuevos proyectos ni cambios de configuración.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación |
|-----------|------------|
| **I. Arquitectura Limpia** | ✅ Las pruebas viven en la capa de tests del frontend y consumen componentes presentacionales por su contrato público (props). No introducen lógica de negocio ni acoplan a internals. |
| **II. SOLID** | ✅ Se prueban componentes con responsabilidad única (presentación); las pruebas dependen del contrato de props, no de detalles internos. |
| **III. SDD** | ✅ Feature nace de spec GIVEN/WHEN/THEN (spec.md 022) derivada de la Spec 5.3 del roadmap; criterios trazables a casos de prueba (ver contracts/). Documentación en español. |
| **IV. Test-First** | ✅ La feature **es** cobertura de pruebas (Vitest frontend exigido por el Principio IV: ">85% cobertura, snapshots para UI crítica"). Sin skips; resultado consumible por CI. |
| **V. Frontend Calidad** | ✅ Refuerza la calidad del frontend: accesibilidad verificada por roles, variantes de tema implícitas en clases, snapshots como regresión. Biome debe seguir 100% compliant en los nuevos archivos. |
| **VI. Observable/Mantenible** | ✅ Snapshots y aserciones documentan el contrato visual de cada componente y sirven de regresión. No introducen acoplamiento oculto. |

**Resultado**: PASS. Sin violaciones que requieran justificación en Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/022-tests-componentes-frontend/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Fase 0: decisiones técnicas (snapshot vs aserción, determinismo)
├── data-model.md        # Fase 1: inventario de componentes bajo prueba y formas de props
├── quickstart.md        # Fase 1: cómo ejecutar y validar la suite y los snapshots
├── contracts/
│   └── component-test-matrix.md  # Fase 1: matriz componente → casos → tipo de aserción
├── checklists/
│   └── requirements.md  # Checklist de calidad de la spec (de /speckit-specify)
└── tasks.md             # Fase 2 (/speckit-tasks — NO creado aquí)
```

### Source Code (repository root)

```text
frontend/
├── src/
│   ├── components/layout/
│   │   └── Footer.tsx                       # Pie del sidebar (año actual → fecha fija en test)
│   └── features/
│       ├── invoices/components/
│       │   ├── StatusBadge.tsx              # Insignia de estado (conocido/desconocido)
│       │   ├── InvoicesEmptyState.tsx       # Estado vacío del listado
│       │   ├── InvoicesTableSkeleton.tsx    # (ya tiene test de render; se añade snapshot)
│       │   └── InvoiceDetailSkeleton.tsx    # Esqueleto del detalle
│       ├── dashboard/components/
│       │   ├── StatCard.tsx                 # Tarjeta de métrica
│       │   ├── DashboardEmptyState.tsx      # Estado vacío del dashboard
│       │   └── DashboardSkeleton.tsx        # Esqueleto del dashboard
│       └── shipments/components/
│           ├── ShipmentsEmptyState.tsx      # Estado vacío de envíos
│           └── ShipmentsTableSkeleton.tsx   # Esqueleto de envíos
└── tests/
    ├── setup.ts                             # Setup global compartido (sin cambios)
    └── components/snapshots/                # NUEVO: pruebas de snapshot de UI crítica
        ├── __snapshots__/                   # Snapshots generados (versionados)
        ├── StatusBadge.snapshot.test.tsx
        ├── StatCard.snapshot.test.tsx
        ├── EmptyStates.snapshot.test.tsx
        ├── Skeletons.snapshot.test.tsx
        └── Footer.snapshot.test.tsx
```

**Structure Decision**: Web application; el trabajo es exclusivamente frontend. Las pruebas de snapshot se agrupan bajo `frontend/tests/components/snapshots/` (carpeta nueva) para mantener separada la regresión por snapshot de las pruebas de comportamiento existentes, y para que los archivos `__snapshots__/` queden localizados. Las pruebas de render/estructura de componentes que carecen de cobertura se ubican junto a la convención existente (`frontend/tests/features/<feature>/...`). No se crean proyectos nuevos ni se modifica la configuración de Vitest, los componentes de producción ni el setup global.

## Complexity Tracking

> No aplica. La Constitution Check pasa sin violaciones; no se introducen patrones adicionales, proyectos nuevos ni cambios de configuración.
