# Research — Phase 0: Formulario de Transición Manual, Dashboard como Inicio y Donut por Estado

**Feature**: `016-transition-form-donut-dashboard` | **Date**: 2026-06-26

Esta feature es **100% frontend** y reutiliza endpoints existentes. Las decisiones siguientes resuelven los *unknowns* del plan. Formato: Decisión / Razón / Alternativas consideradas.

---

## D1 — Mecanismo de *toast* (notificación de éxito/error)

**Decisión**: Implementar un sistema de *toast* **in-house** con tres piezas mínimas en `components/feedback`:
- `ToastProvider` (contexto React con la cola de toasts y las acciones `show/dismiss`).
- `useToast` (hook que expone `toast.success(msg)` y `toast.error(msg)`).
- `ToastViewport` (región visual fija con `role="status"`/`aria-live="polite"` para éxito y `role="alert"`/`aria-live="assertive"` para error, animada con Motion y respetando `prefers-reduced-motion`).

El `ToastProvider` envuelve la app en `App.tsx`; el `ToastViewport` se monta una vez en `AppShell`.

**Razón**:
- **Sin dependencias de runtime nuevas**, coherente con la decisión de 015 (gráficos in-house) y con el presupuesto de bundle (<50KB gzip, Constitución V).
- Control total de accesibilidad (regiones `aria-live`, no robar foco) y de `prefers-reduced-motion`.
- Superficie de código pequeña y testeable; encaja con el sistema de componentes shadcn/ui (Tailwind, tokens de tema, dark mode).

**Alternativas consideradas**:
- **`sonner`** (toast recomendado por shadcn): excelente DX pero **añade dependencia de runtime** y va contra el patrón establecido de "sin deps nuevas"; descartada por bundle/consistencia.
- **`@radix-ui/react-toast`**: robusto y accesible, pero **no está instalado** (solo `react-dialog` y `react-select`); añadiría dependencia y complejidad de API (swipe, regiones) mayor que la necesidad real.

---

## D2 — Geometría y accesibilidad del gráfico de **dona** (donut)

**Decisión**: Construir `DonutChart` **in-house** con SVG + Motion. El anillo se dibuja con un `<circle>` de fondo (pista) y un `<circle>` por segmento usando `stroke`, `stroke-dasharray` y `stroke-dashoffset` (técnica de anillo por trazo), animando el barrido con Motion (`stroke-dashoffset` o `pathLength`) y respetando `useReducedMotion`. El **total** se renderiza como texto centrado (número grande + etiqueta "Total"). La accesibilidad recae en una **leyenda textual** (lista `color ↔ estado ↔ valor`), no en el SVG; el SVG es `role="img"` con `aria-label` resumido y los segmentos son `role="presentation"`.

**Razón**:
- Reutiliza Motion (ya importado por el dashboard) y el patrón in-house de 015; **sin librería de charting**.
- `stroke-dasharray` permite segmentos proporcionales sin cálculo de *paths* de arco complejos y anima limpio.
- La leyenda textual cubre WCAG A (no depender solo del color, SC-007) y facilita los tests (assert por texto/valor).

**Alternativas consideradas**:
- **Librería de charting** (recharts/visx/chart.js): bundle desproporcionado para un único donut; descartada por Constitución (bundle) y consistencia.
- **Segmentos con `<path>` de arcos (A command)**: más control visual (gaps, esquinas) pero más matemática y más superficie de error; el enfoque por `stroke` es suficiente para "color por estado + total".

---

## D3 — Colores por estado para los segmentos del donut

**Decisión**: Definir `statusChartColors.ts` en `features/dashboard` con un mapa `KnownInvoiceStatus → clase Tailwind fill/stroke` con variante clara y oscura, **alineado con `STATUS_CLASSES` de `StatusBadge`**:

| Estado | Color base (coherente con badge) |
|--------|----------------------------------|
| `pending` | ámbar (amber) |
| `primerrecordatorio` | azul (blue) |
| `segundorecordatorio` | naranja (orange) |
| `desactivado` | zinc (gris neutro) |
| `pagado` | lima (lime) |
| desconocido | `muted` (neutro) |

Los segmentos aplican la clase de color vía `className` (`stroke-*`/`fill-*`), de modo que el **dark mode** funcione con las variantes `dark:` de Tailwind, igual que las insignias.

**Razón**:
- **Coherencia visual** con las etiquetas de estado del listado y el modal (FR-016): el usuario asocia el mismo color al mismo estado en todo el panel.
- Usar utilidades Tailwind (`stroke-amber-500 dark:stroke-amber-400`) mantiene el dark mode *built-in* sin colores hardcodeados en JS.

**Alternativas consideradas**:
- **Colores hex/HSL en JS**: rompería el dark mode automático y duplicaría tokens; descartada.
- **Una sola variable `--primary` para todo** (como el `BarChart` actual): no distingue estados; el requisito pide **un color por estado**.

---

## D4 — Modelo de ruteo (`"/"` = dashboard; retiro de `/dashboard`; catch-all)

**Decisión** (alineada con clarificación 2026-06-26):
- `"/"` renderiza `DashboardPage` (lazy, con `DashboardSkeleton` de *fallback*).
- Se **elimina** la `<Route path="/dashboard">`.
- El catch-all `<Route path="*">` redirige a `"/"` (antes `/facturas`).
- En `navigation.ts`, el ítem "Dashboard" cambia `to: '/dashboard'` → `to: '/'`. Para el resaltado activo, usar coincidencia exacta (`end`) en el `NavLink` de `"/"` y así "Facturas"/"Configuración" no lo activen.

**Razón**:
- URL canónica única para el inicio; conserva un solo punto de verdad y evita dos rutas equivalentes.
- El catch-all a `"/"` es coherente con la nueva pantalla de inicio (FR-014a).

**Alternativas consideradas**:
- Mantener `/dashboard` como redirección a `"/"`: rechazada por la clarificación (eliminación completa).
- Mantener catch-all → `/facturas`: incoherente con el nuevo inicio.

**Riesgo/seguimiento**: el `NavLink` a `"/"` debe usar `end` para evitar que quede "activo" en todas las rutas hijas; cubierto por test de navegación.

---

## D5 — Validación del formulario y combinación toast + error inline

**Decisión**: Envolver el control en un `<form>` con `onSubmit`:
- **Validación de cliente**: si no hay destino seleccionado, `onSubmit` lo previene y muestra un mensaje de validación (texto asociado al control vía `aria-describedby`), **sin** realizar petición.
- **Éxito**: `toast.success("Estado actualizado a «…».")`; el `useTransitionInvoice` ya actualiza estado/historial/listado por invalidación.
- **Error**: `toast.error(motivo || genérico)` **y** se conserva el mensaje de error **inline persistente** ya existente (no desaparece como el toast).

**Razón**: Cumple FR-003/FR-005/FR-006/FR-006a. El toast da confirmación inmediata; el inline persistente preserva el motivo del fallo (accesibilidad/robustez, decisión de clarificación).

**Alternativas consideradas**:
- Validación nativa del navegador (`required` en select): menos control del mensaje en español y de la accesibilidad; se prefiere validación explícita controlada.
- Solo toast para error (sin inline): rechazada en clarificación.

---

## D6 — Auto-cierre del toast y prevención de doble envío

**Decisión**:
- **Éxito**: el toast se auto-cierra tras ~4s y es **descartable** manualmente (botón "cerrar" con `aria-label`).
- **Error**: el toast persiste hasta descarte manual (no auto-cierra), reforzando que el usuario lo vea; el motivo también queda inline.
- **Doble envío**: durante `mutation.isPending` el botón y el select se deshabilitan (ya implementado) y `onSubmit` ignora envíos mientras esté ocupado (FR-009).
- `prefers-reduced-motion`: la entrada/salida del toast usa `motionTransition(reduced)` (duración 0 si reducido); el contenido sigue siendo visible/anunciado.

**Razón**: Patrón estándar de toasts accesibles; balancea visibilidad (errores) y no-intrusión (éxitos). Cumple FR-009/FR-011/SC-008.

**Alternativas consideradas**:
- Auto-cierre también para errores: riesgo de que el usuario no lo lea; mitigado además por el inline, pero se prefiere persistencia del toast de error.
- Sin auto-cierre para éxito: ruido visual acumulado; descartada.

---

## Resumen de impacto

- **Sin cambios de backend** ni de contrato de API (reuso de detalle, transición y stats).
- **Sin dependencias de runtime nuevas** (toast y donut in-house).
- Áreas tocadas: `components/feedback` (toast), `components/layout` (viewport + nav), `features/invoices` (formulario), `features/dashboard` (donut), `App.tsx` (ruteo + provider).
- Todos los *unknowns* del plan quedan resueltos; no quedan marcadores `NEEDS CLARIFICATION`.
