# Data Model — Phase 1: tipos frontend y mapeos

**Feature**: `016-transition-form-donut-dashboard` | **Date**: 2026-06-26

Esta feature no introduce entidades de backend ni cambios de persistencia. El "modelo de datos" se limita a **tipos de UI** del frontend y a un **mapeo de presentación** (estado → color del gráfico). Se reutilizan los tipos existentes (`InvoiceDetail`, `InvoiceStatus`, `InvoiceStats`, `ChartDatum`).

---

## 1. Sistema de *toast* (`components/feedback`)

### `ToastVariant`

```ts
export type ToastVariant = 'success' | 'error'
```

### `ToastMessage`

| Campo | Tipo | Reglas |
|-------|------|--------|
| `id` | `string` | Identificador único (p. ej. `crypto.randomUUID()`). |
| `variant` | `ToastVariant` | `success` → región `aria-live="polite"`, auto-cierre ~4s. `error` → `aria-live="assertive"`, persiste hasta descarte. |
| `message` | `string` | Texto legible en español (≥1 carácter). |
| `createdAt` | `number` | Marca temporal (ms) para orden/expiración. |

```ts
export interface ToastMessage {
  id: string
  variant: ToastVariant
  message: string
  createdAt: number
}
```

### Contexto / API del hook

```ts
export interface ToastApi {
  success: (message: string) => void
  error: (message: string) => void
  dismiss: (id: string) => void
}
// useToast(): ToastApi   — lanza si se usa fuera de <ToastProvider>
```

**Reglas de comportamiento**:
- `success`: encola un toast `success`; programa auto-cierre (~4s); descartable manualmente.
- `error`: encola un toast `error`; **no** auto-cierra; descartable manualmente.
- Múltiples toasts se apilan en orden de `createdAt`; el *viewport* limita el apilamiento visible (p. ej. máx. 3, el resto en cola) — detalle de implementación, no contractual.

---

## 2. Gráfico de **dona** (`features/dashboard`)

### Reuso de `ChartDatum` (ya existente)

```ts
// features/dashboard/types.ts (SIN CAMBIOS)
export interface ChartDatum {
  label: string   // etiqueta legible del estado (INVOICE_STATUS_LABELS)
  value: number   // conteo de facturas en ese estado (≥0)
  color?: string  // clase de color (stroke-*) del segmento; ver §3
}
```

### Props de `DonutChart`

| Prop | Tipo | Reglas |
|------|------|--------|
| `data` | `ChartDatum[]` | Un elemento por estado presente; `value ≥ 0`. |
| `total` | `number` | Total a mostrar en el centro (= suma de `value`); `0` en vacío. |
| `ariaLabel` | `string` | Etiqueta accesible del gráfico en su conjunto. |
| `centerLabel` | `string` | Texto bajo el número central (por defecto `"Total"`). |

**Invariantes / casos**:
- `data` vacío o `total === 0` → estado vacío: anillo/pista sin segmentos y centro mostrando `0` (FR-016a/FR-019).
- Un único `value > 0` → un segmento que cubre el anillo completo (FR-019).
- La proporción de cada segmento = `value / total`; segmentos con `value === 0` no se dibujan (o longitud 0).
- Estado desconocido (sin color mapeado) → color neutro (§3).

---

## 3. Mapa de colores por estado (`features/dashboard/statusChartColors.ts`)

Coherente con `STATUS_CLASSES` de `StatusBadge` (mismo color por estado, con dark mode):

```ts
import type { KnownInvoiceStatus } from '@/features/invoices/types'

/** Clase de color del trazo (segmento del donut) por estado, con variante oscura. */
export const STATUS_CHART_CLASSES: Record<KnownInvoiceStatus, string> = {
  pending: 'stroke-amber-400 dark:stroke-amber-300',
  primerrecordatorio: 'stroke-blue-500 dark:stroke-blue-300',
  segundorecordatorio: 'stroke-orange-500 dark:stroke-orange-300',
  desactivado: 'stroke-zinc-400 dark:stroke-zinc-500',
  pagado: 'stroke-lime-500 dark:stroke-lime-300',
}

/** Color neutro para estados no mapeados (compatibilidad futura). */
export const UNKNOWN_STATUS_CHART_CLASS = 'stroke-muted-foreground'

export function statusChartClass(status: string): string {
  return STATUS_CHART_CLASSES[status as KnownInvoiceStatus] ?? UNKNOWN_STATUS_CHART_CLASS
}
```

> Los valores concretos de tono (`-400/-500`) se ajustarán en implementación para garantizar contraste WCAG A en claro y oscuro; el contrato es: **un color por estado, coherente con la insignia, con dark mode**.

### Derivación de los datos del donut (en `DashboardPage`/`StatusDistributionChart`)

```ts
const statusData: ChartDatum[] = FILTERABLE_STATUSES.map((status) => ({
  label: INVOICE_STATUS_LABELS[status],
  value: stats.byStatus[status] ?? 0,
  color: statusChartClass(status),
}))
const total = stats.totalInvoices // = suma de byStatus
```

---

## 4. Formulario de transición (`features/invoices/ChangeStatusControl`)

No introduce tipos nuevos; reutiliza:
- `InvoiceStatus`, `allowedTransitions: InvoiceStatus[]` (de `InvoiceDetail`).
- `TransitionInvoiceVariables { id, newStatus }` y `useTransitionInvoice()` (existentes).

**Estado de UI añadido**:

| Estado | Tipo | Propósito |
|--------|------|-----------|
| `selected` | `InvoiceStatus \| ''` | Destino elegido (ya existe). |
| `validationError` | `string \| null` | Mensaje de validación de cliente (nuevo): "Selecciona un estado destino." cuando se intenta enviar sin selección. |

**Transiciones de estado del formulario**:
- `submit` con `selected === ''` → `validationError` establecido, sin petición.
- `submit` con `selected` válido y no ocupado → `mutation.mutate(...)`.
- `onSuccess` → `toast.success`, limpiar `selected` y `validationError`.
- `onError` → `toast.error(motivo)`, mantener mensaje inline persistente (vía `mutation.isError`).
- `allowedTransitions.length === 0` → no se renderiza el formulario (mensaje de estado terminal, ya existente).

---

## 5. Ruteo (`App.tsx`, `navigation.ts`)

No es "data" pero define la forma de navegación afectada:

| Ruta | Antes | Después |
|------|-------|---------|
| `/` | `Navigate → /facturas` | `DashboardPage` (lazy) |
| `/dashboard` | `DashboardPage` | **eliminada** |
| `/facturas` | `InvoicesPage` | sin cambios |
| `/configuracion` | `SettingsPage` | sin cambios |
| `*` | `Navigate → /facturas` | `Navigate → /` |

`NAV_ITEMS`: el ítem "Dashboard" pasa de `to: '/dashboard'` a `to: '/'` y su `NavLink` usa coincidencia exacta (`end`) para el resaltado activo.
