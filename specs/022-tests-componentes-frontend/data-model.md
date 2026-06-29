# Fase 1 — Data Model: Tests de Componentes Frontend

**Feature**: 022-tests-componentes-frontend | **Fecha**: 2026-06-29

Esta feature no introduce entidades de dominio ni persistencia. El "modelo de datos" relevante es el **inventario de componentes bajo prueba** con la forma de sus props y las fuentes de indeterminismo a neutralizar.

## Componentes presentacionales bajo prueba

| Componente | Ubicación | Props | Ramas/variantes | Indeterminismo |
|------------|-----------|-------|-----------------|----------------|
| `StatusBadge` | `features/invoices/components/StatusBadge.tsx` | `status: InvoiceStatus` | estado conocido (5: pending, pagado, primerrecordatorio, segundorecordatorio, desactivado) vs desconocido (estilo neutro + valor en bruto) | ninguno |
| `ShipmentStatusBadge` | `features/shipments/components/ShipmentStatusBadge.tsx` | `status: SendStatus` | estado conocido (5: pending, sent, failed, skipped, retrying) vs desconocido | ninguno |
| `StatCard` | `features/dashboard/components/StatCard.tsx` | `label: string`, `value: number\|string`, `icon?: LucideIcon` | con ícono vs sin ícono; valor numérico vs string | ninguno |
| `InvoicesEmptyState` | `features/invoices/components/InvoicesEmptyState.tsx` | — | única | ninguno |
| `DashboardEmptyState` | `features/dashboard/components/DashboardEmptyState.tsx` | — | única | ninguno |
| `ShipmentsEmptyState` | `features/shipments/components/ShipmentsEmptyState.tsx` | `filtered: boolean` | `filtered=true` ("sin coincidencias") vs `filtered=false` ("aún no hay envíos") | ninguno |
| `InvoicesTableSkeleton` | `features/invoices/components/InvoicesTableSkeleton.tsx` | `rows?: number` | número de filas (default 10) | ninguno |
| `InvoiceDetailSkeleton` | `features/invoices/components/InvoiceDetailSkeleton.tsx` | — | única | ninguno |
| `DashboardSkeleton` | `features/dashboard/components/DashboardSkeleton.tsx` | — | única | ninguno |
| `ShipmentsTableSkeleton` | `features/shipments/components/ShipmentsTableSkeleton.tsx` | `rows?: number` | número de filas (default 10) | ninguno |
| `Footer` | `components/layout/Footer.tsx` | `collapsed?: boolean` | expandido (nombre + versión + año) vs colapsado (solo versión) | **`new Date().getFullYear()`** → fijar fecha |

> Para mantener snapshots estables, los esqueletos con prop `rows` se snapshotean con un valor **fijo y pequeño** (p. ej. `rows={2}`) y no con el default 10.

## Entidades conceptuales de la feature

- **Caso de prueba de render/estructura**: renderiza el componente con props representativas y afirma contenido visible, rol accesible y/o variante por prop. No genera artefactos persistentes.
- **Caso de prueba de snapshot**: renderiza el componente y compara su marcado serializado contra el snapshot almacenado en `__snapshots__/`. Genera/actualiza el archivo de snapshot (versionado en git).
- **Inventario de trazabilidad**: tabla criterio del roadmap → prueba(s) que lo respaldan (ver `quickstart.md`).

## Datos de prueba (fixtures mínimos)

- Estados de factura conocidos/desconocidos: literales de `InvoiceStatus` (incl. uno arbitrario no reconocido para la rama neutra).
- Estados de envío conocidos/desconocidos: literales de `SendStatus`.
- `StatCard`: `{ label: 'Total facturas', value: 128 }` y variante con `icon`.
- Fecha fija para `Footer`: `new Date('2026-01-01T00:00:00Z')` (año 2026 determinista).

No se requieren dobles de red, query client ni router para estos componentes presentacionales (dependencias acotadas a `@/components/ui/*` y `lucide-react`).
