# Implementation Plan: Formulario de Transición Manual de Estado, Dashboard como Inicio y Gráfico Donut por Estado

**Branch**: `016-transition-form-donut-dashboard` | **Date**: 2026-06-26 | **Spec**: [spec.md](./spec.md)

**Input**: Especificación de funcionalidad desde `specs/016-transition-form-donut-dashboard/spec.md`

## Summary

Tres capacidades **enteramente de frontend** que se apoyan en el panel y los endpoints ya existentes (specs 014/015), sin cambios de backend ni de contrato de API:

1. **Formulario de transición manual de estado** (roadmap 4.5): se eleva el control de cambio de estado del modal (spec 015, `ChangeStatusControl`) al estándar 4.5 envolviéndolo en un `<form>` con **validación de cliente explícita** (no se envía sin destino seleccionado, con mensaje de validación), y se añade **retroalimentación tipo *toast*** de éxito/error. El error mantiene además el mensaje **inline persistente** ya existente. La coherencia de tabla + modal + dashboard ya la garantiza `useTransitionInvoice` vía invalidación dirigida (`['invoice',id]`, `['invoices']`, `['invoice-stats']`).
2. **Dashboard como pantalla de inicio**: la ruta raíz `"/"` renderiza `DashboardPage`; la ruta `/dashboard` se **elimina** (sin redirección como ruta válida); cualquier ruta desconocida redirige a `"/"`; la entrada de navegación "Dashboard" apunta a `"/"` y se resalta como activa en la raíz.
3. **Gráfico de dona (donut) por estado con colores**: la tarjeta "Facturas por estado" pasa de barras (`BarChart`) a un **donut SVG + Motion in-house** con un **color por estado** coherente con `StatusBadge`, **total en el centro**, leyenda accesible y estado vacío. La tarjeta "Facturas por cliente" conserva su `BarChart`.

**Enfoque técnico**: Reutilización máxima del stack ya montado (React 19 + Vite 8 + Tailwind v4 + shadcn/ui *new-york*, TanStack Query, Motion, react-router v7, Biome, Vitest). El **mecanismo de toast** no existe hoy y se construye **in-house** (contexto + región `aria-live` + Motion), coherente con la decisión de 015 de no añadir dependencias de runtime y con el presupuesto de bundle (<50KB gzip, Constitución). El donut también se construye in-house con SVG + Motion (sin librería de charting), reutilizando `ChartDatum.color` (ya previsto en `features/dashboard/types.ts`). No hay nuevas dependencias de runtime ni cambios de backend.

## Technical Context

**Language/Version**: TypeScript 6 (strict, sin `any`) sobre React 19 + Vite 8. No se toca el backend (.NET) en esta feature.

**Primary Dependencies**:
- Existentes (frontend): `react@19`, `react-dom@19`, `@tanstack/react-query@5`, `motion@12`, `react-router-dom@7`, `tailwindcss@4` (`@tailwindcss/vite`), `class-variance-authority`, `clsx`, `tailwind-merge`, `lucide-react`. Biome, Vitest + Testing Library.
- **Nuevas**: ninguna dependencia de runtime nueva. El sistema de *toast* y el gráfico de **dona** se construyen in-house (Motion ya disponible). No se añade `sonner` ni `@radix-ui/react-toast` (ver research D1).

**Storage**: N/A (frontend). No hay cambios de persistencia ni de esquema. Se reutilizan `GET /api/invoices/{id}`, `POST /api/invoices/transition/{id}` y `GET /api/invoices/stats` sin cambios de contrato.

**Testing**: Vitest + Testing Library (tests co-ubicados en `frontend/tests/...` reflejando `src`).
- Formulario de transición: validación (confirmar sin selección no envía, muestra mensaje), envío (estado ocupado, anti doble-envío), éxito (toast de éxito + actualización de estado/historial/listado), error (toast de error + mensaje inline persistente + sin cambio de estado), estado terminal (formulario oculto/deshabilitado).
- Toast: render en región `aria-live`, auto-cierre del éxito, persistencia/cierre manual, accesibilidad y `prefers-reduced-motion`.
- Dona: un segmento por estado con color coherente, total en el centro, leyenda accesible (color↔estado↔valor), animación de entrada, estado vacío (centro `0`), un solo estado (anillo completo), estado desconocido (color neutro).
- Ruteo: `"/"` renderiza el dashboard; `/dashboard` ya no es válida y redirige a `"/"`; ruta desconocida → `"/"`; navegación resalta "Dashboard" como activo en `"/"`; Facturas/Configuración intactas.

**Target Platform**: SPA servida por Vite/estáticos detrás del backend (proxy `/api`). Navegadores modernos; responsive móvil/escritorio. Modo claro/oscuro por clase `.dark` (`ThemeProvider`).

**Project Type**: Web (SPA frontend por feature). Esta feature toca **solo** `frontend/src` (y sus tests). Sin cambios en `backend/`.

**Performance Goals**: TTI < 2s y Lighthouse > 90 (Constitución V). Bundle principal < 50KB gzip → la ruta de inicio (dashboard) se mantiene *lazy*; el toast es ligero (sin dependencia nueva); el donut reutiliza Motion ya importado por el dashboard. Mutación con invalidación dirigida (sin recargar la página).

**Constraints**:
- TypeScript strict sin `any`; Biome 100% *compliant*; **React Doctor 100/100 honesto** (sin suprimir avisos — FR-021/SC-010).
- Accesibilidad WCAG A: foco visible, operable por teclado; *toast* anunciado por región `aria-live` (no roba foco); la información por color del donut cuenta también con etiqueta textual (no se depende solo del color); animaciones respetan `prefers-reduced-motion` (regla base en `index.css` + `useReducedMotion`).
- Dark mode *built-in* (colores del donut con variantes claro/oscuro vía utilidades `fill-*` de Tailwind, coherentes con `StatusBadge`). Documentación en español (Constitución III).

**Scale/Scope**: Conjunto activo de 5 estados (pendiente, 1er/2do recordatorio, desactivado, pagado). Alcance: roadmap 4.5 (formulario de transición) + cambio de inicio a dashboard + donut por estado. Fuera de alcance: cualquier cambio de backend, el gráfico por cliente (sigue en barras) y nuevas métricas del dashboard.

### Unknowns resueltos (ver research.md)

| Tema | Estado |
|------|--------|
| Mecanismo de *toast*: librería (`sonner`/Radix) vs. in-house | Resuelto → toast in-house (contexto + región `aria-live` + Motion); sin dependencia nueva (D1) |
| Geometría y accesibilidad del gráfico de **dona** (SVG vs. lib) | Resuelto → donut in-house con SVG + Motion; leyenda textual; total en el centro (D2) |
| Colores por estado para los segmentos del donut | Resuelto → mapa `STATUS_CHART_CLASSES` con utilidades `fill-*` (claro/oscuro) coherente con `StatusBadge`; neutro para desconocidos (D3) |
| Modelo de ruteo: `"/"` = dashboard, retiro de `/dashboard`, catch-all | Resuelto → `"/"` renderiza dashboard; `/dashboard` se elimina; `*` → `"/"`; nav apunta a `"/"` (D4) |
| Validación del formulario y combinación toast + error inline | Resuelto → `<form>` con validación de cliente; éxito por toast; error por toast + inline persistente (D5) |
| Auto-cierre del toast y antidoble envío | Resuelto → éxito auto-cierra (~4s) y es descartable; error persiste hasta descartar; botón ocupado durante la mutación (D6) |

## Constitution Check

*GATE: Debe pasar antes de Phase 0. Re-evaluado tras Phase 1.*

| Principio | Evaluación | Estado |
|-----------|------------|--------|
| I. Arquitectura Limpia | Frontend por feature: el formulario y los *toasts* de transición viven en `features/invoices`; el donut en `features/dashboard`; el *provider* de toasts es un componente transversal en `components/feedback`. El ruteo se ajusta en `App.tsx`/`navigation.ts`. Sin lógica de dominio en el frontend: los destinos válidos siguen viniendo del backend; el frontend no decide validez de transición. | ✅ PASS |
| II. SOLID | SRP: `ToastProvider`/`useToast` (notificación), `TransitionForm` (validación+envío), `DonutChart` (render del anillo), `StatusDonutChart` (mapeo estado→color), cada uno con una responsabilidad. OCP/DIP: los componentes dependen de tipos (`ChartDatum`, `ToastMessage`) y de hooks, no de implementaciones concretas; el donut reutiliza `ChartDatum.color`. | ✅ PASS |
| III. SDD (specs en español) | Spec 016 escrita, clarificada (4 respuestas) y validada; todos los artefactos de este plan en español. | ✅ PASS |
| IV. Test-First (≥85%) | Se escriben primero las pruebas (formulario, toast, donut, ruteo, a11y, reduce-motion) antes de implementar. | ✅ PASS |
| V. Frontend Producción | TS strict, Biome, **React Doctor 100/100 honesto**, WCAG A (toast `aria-live`, foco, teclado), dark mode *built-in* (colores del donut), TTI<2s / Lighthouse>90, responsive real, `prefers-reduced-motion`. | ✅ PASS (verificado al final con React Doctor) |
| VI. Observable y Mantenible | `ErrorBoundary` existente envuelve las rutas; estados de error legibles con reintento (dashboard) y mensaje inline persistente (formulario). Sin `console.*` en producción. | ✅ PASS |
| Stack tecnológico | shadcn/ui + Motion (toast y donut in-house) + TanStack Query (`useMutation`/`useQuery`) + react-router v7. Sin dependencias de runtime nuevas. Backend sin cambios. | ✅ PASS |
| Seguridad | Sin secretos en frontend; el cambio de estado reusa el endpoint validado (FluentValidation + matriz de dominio); el frontend nunca decide la validez por su cuenta. | ✅ PASS |
| Performance & Escalabilidad | Inicio (dashboard) *lazy*; toast ligero sin dep nueva; donut con Motion ya presente; invalidación dirigida tras la mutación; agregados de stats ya existentes. | ✅ PASS |

**Resultado del gate**: PASS. Ningún principio NO NEGOCIABLE se incumple. La feature es 100% frontend, sin propagar lógica de dominio (la validez de transición sigue en el backend) y sin añadir dependencias de runtime.

## Project Structure

### Documentation (this feature)

```text
specs/016-transition-form-donut-dashboard/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Phase 0 — decisiones técnicas (toast, donut, colores, ruteo, validación)
├── data-model.md        # Phase 1 — tipos/contratos frontend (ToastMessage, DonutDatum, mapa de colores)
├── quickstart.md        # Phase 1 — guía de validación end-to-end
├── contracts/
│   └── ui-contracts.md  # Contratos de UI: sistema de toast, formulario de transición, donut y ruteo
├── checklists/
│   └── requirements.md  # Checklist de calidad (ya existente)
└── tasks.md             # Phase 2 (/speckit-tasks — NO creado por /speckit-plan)
```

### Source Code (repository root)

```text
frontend/
├── src/
│   ├── components/
│   │   ├── ui/
│   │   │   └── ...                                  # (EXISTEN) button, select, card, badge, dialog, skeleton…
│   │   ├── layout/
│   │   │   ├── AppShell.tsx                          # (EDITADO) monta <ToastViewport/> (región aria-live) en el shell
│   │   │   └── navigation.ts                         # (EDITADO) ítem "Dashboard" → to: '/'
│   │   └── feedback/
│   │       ├── ErrorBoundary.tsx                     # (EXISTE)
│   │       ├── ToastProvider.tsx                     # (NUEVO) contexto + estado de toasts (in-house)
│   │       ├── ToastViewport.tsx                     # (NUEVO) región aria-live + render animado (Motion)
│   │       └── useToast.ts                           # (NUEVO) hook de acceso (toast.success/error)
│   ├── features/invoices/
│   │   └── components/
│   │       └── ChangeStatusControl.tsx               # (EDITADO) <form> + validación de cliente + toast éxito/error (conserva inline)
│   ├── features/dashboard/
│   │   ├── components/
│   │   │   ├── DonutChart.tsx                         # (NUEVO) anillo SVG + Motion + total en el centro + leyenda
│   │   │   ├── StatusDistributionChart.tsx           # (EDITADO) usa DonutChart con colores por estado (antes BarChart)
│   │   │   └── statusChartColors.ts                  # (NUEVO) mapa estado→clase fill-* (claro/oscuro), neutro p/ desconocido
│   │   └── types.ts                                  # (EXISTE; ChartDatum.color ya soportado)
│   ├── App.tsx                                        # (EDITADO) "/" → DashboardPage (lazy); retira "/dashboard"; "*" → "/"; envuelve con ToastProvider
│   └── index.css                                      # (EXISTE) regla base de prefers-reduced-motion
└── tests/                                             # Vitest + Testing Library (co-ubicados, reflejan src)
    ├── components/
    │   ├── feedback/Toast.test.tsx                    # (NUEVO) provider/viewport/useToast + aria-live + reduce-motion
    │   └── layout/Navigation.dashboard.test.tsx       # (EDITADO) "Dashboard" activo en "/"
    ├── features/invoices/ChangeStatusControl.test.tsx # (EDITADO) validación + toasts + inline + ocupado/terminal
    ├── features/dashboard/Charts.test.tsx             # (EDITADO) donut: segmentos, colores, centro, leyenda, vacío
    └── App.test.tsx                                   # (EDITADO) "/" = dashboard; /dashboard y desconocidas → "/"

backend/   (SIN CAMBIOS)
```

**Structure Decision**: Se mantiene la SPA por feature ya establecida. El **sistema de toast** es transversal (lo consumen varias features) y por eso vive en `components/feedback` con su *provider* montado en la raíz (`App.tsx`) y su *viewport* en `AppShell`. El **formulario de transición** extiende el componente existente de `features/invoices` (sin nuevos endpoints). El **donut** vive en `features/dashboard` reemplazando solo la representación por estado; el gráfico por cliente no cambia. El **ruteo** se ajusta en `App.tsx` y `navigation.ts`. No se crean carpetas ni capas nuevas en backend.

## Complexity Tracking

> Sin violaciones de la Constitución que requieran justificación. La feature es 100% de frontend, reutiliza los endpoints existentes y no añade dependencias de runtime. La única pieza realmente nueva (sistema de *toast*) se implementa in-house con una superficie mínima (provider + viewport + hook) para honrar el presupuesto de bundle y la coherencia con la decisión de 015 de no introducir librerías de runtime. El donut reutiliza el `ChartDatum` y Motion ya presentes. El cambio de ruteo retira una ruta (`/dashboard`) en lugar de añadir complejidad: reduce la superficie a una URL canónica de inicio.
