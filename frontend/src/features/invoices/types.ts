/**
 * El backend serializa `InvoiceStatus` como cadena en minúsculas (nombre del
 * miembro del enum), p. ej. `PrimerRecordatorio → "primerrecordatorio"`.
 * Ver backend/Api/Endpoints/Invoices/InvoiceStatusApi.cs (JsonStringEnumConverter global).
 */
export type KnownInvoiceStatus =
  | 'pending'
  | 'pagado'
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
])

/**
 * Etiqueta legible en español por estado (conjunto activo; los estados legacy se retiraron, spec 015).
 */
export const INVOICE_STATUS_LABELS: Record<KnownInvoiceStatus, string> = {
  pending: 'Pendiente',
  pagado: 'Pagado',
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

/**
 * Origen de un cambio de estado (spec 015). Se admite un valor desconocido sin romper el tipado.
 */
export type StatusChangeSource = 'automatic' | 'manual' | (string & {})

/**
 * Un evento del historial de cambios de estado de una factura.
 */
export interface StatusChange {
  from: InvoiceStatus
  to: InvoiceStatus
  at: string // ISO-8601 UTC
  source: StatusChangeSource
}

/**
 * Detalle completo de una factura (spec 015), extendido con el historial de cambios de
 * estado y los estados destino válidos provistos por el backend.
 */
export interface InvoiceDetail {
  id: string
  clientId: string
  amount: number
  status: InvoiceStatus
  createdAt: string // ISO-8601 UTC
  updatedAt: string // ISO-8601 UTC
  remindersCount: number
  lastReminderSentAt: string | null // ISO-8601 UTC
  lastStatusTransitionAt: string // ISO-8601 UTC
  statusHistory: StatusChange[]
  allowedTransitions: InvoiceStatus[]
}
