import type { KnownInvoiceStatus } from '@/features/invoices/types'

/**
 * Clase de color del trazo (segmento del donut) por estado, con variante oscura,
 * coherente con `STATUS_CLASSES` de `StatusBadge` para que el mismo estado tenga
 * el mismo color en todo el panel.
 */
export const STATUS_CHART_CLASSES: Record<KnownInvoiceStatus, string> = {
  pending: 'stroke-amber-400 dark:stroke-amber-300',
  primerrecordatorio: 'stroke-blue-500 dark:stroke-blue-300',
  segundorecordatorio: 'stroke-orange-500 dark:stroke-orange-300',
  desactivado: 'stroke-zinc-400 dark:stroke-zinc-500',
  pagado: 'stroke-lime-500 dark:stroke-lime-300',
}

/** Color neutro para estados no mapeados (compatibilidad futura). */
export const UNKNOWN_STATUS_CHART_CLASS = 'stroke-muted-foreground'

/** Devuelve la clase de color del segmento para un estado dado. */
export function statusChartClass(status: string): string {
  return STATUS_CHART_CLASSES[status as KnownInvoiceStatus] ?? UNKNOWN_STATUS_CHART_CLASS
}
