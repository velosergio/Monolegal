/**
 * El backend serializa `InvoiceStatus` como cadena en minúsculas (nombre del
 * miembro del enum), p. ej. `PrimerRecordatorio → "primerrecordatorio"`.
 * Ver backend/Api/Endpoints/Invoices/InvoiceStatusApi.cs (JsonStringEnumConverter global).
 */
export type KnownInvoiceStatus =
  | 'draft'
  | 'pending'
  | 'pagado'
  | 'overdue'
  | 'cancelled'
  | 'primerrecordatorio'
  | 'segundorecordatorio'
  | 'desactivado'

/**
 * Estado tal como llega de la API. Se admiten estados desconocidos (futuros) sin
 * romper el tipado: se renderizan con una etiqueta neutra.
 */
export type InvoiceStatus = KnownInvoiceStatus | (string & {})

/**
 * Estados que pueden filtrarse en el panel (conjunto válido expuesto por la API).
 */
export const FILTERABLE_STATUSES = [
  'pending',
  'primerrecordatorio',
  'segundorecordatorio',
  'desactivado',
  'pagado',
] as const satisfies readonly KnownInvoiceStatus[]

/**
 * Estados terminales: una factura en alguno de estos estados ya no admite pago manual.
 */
export const TERMINAL_STATUSES: ReadonlySet<string> = new Set<KnownInvoiceStatus>([
  'pagado',
  'desactivado',
  'cancelled',
])

/**
 * Etiqueta legible en español por estado.
 */
export const INVOICE_STATUS_LABELS: Record<KnownInvoiceStatus, string> = {
  draft: 'Borrador',
  pending: 'Pendiente',
  pagado: 'Pagado',
  overdue: 'Vencida',
  cancelled: 'Cancelada',
  primerrecordatorio: '1er Recordatorio',
  segundorecordatorio: '2do Recordatorio',
  desactivado: 'Desactivado',
}

/**
 * Devuelve la etiqueta legible de un estado, con respaldo al valor en bruto si
 * el estado no está mapeado (compatibilidad futura).
 */
export function statusLabel(status: InvoiceStatus): string {
  return INVOICE_STATUS_LABELS[status as KnownInvoiceStatus] ?? status
}

/**
 * Forma de una factura tal como la devuelve la API del backend (spec 014).
 */
export interface Invoice {
  id: string
  clientId: string
  amount: number
  status: InvoiceStatus
  createdAt: string // ISO-8601 UTC
  lastStatusTransitionAt: string // ISO-8601 UTC — "Última Acción"
}

/**
 * Respuesta paginada del listado de facturas.
 */
export interface PagedInvoices {
  data: Invoice[]
  total: number
  pageSize: number
}
