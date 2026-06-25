/**
 * Espejo del enum InvoiceStatus definido en el backend:
 * backend/Domain/Enums/InvoiceStatus.cs
 *
 * Solo se incluyen los valores relevantes para el flujo de facturas;
 * cualquier estado no listado aquí se considera desconocido.
 */
export enum InvoiceStatus {
  Draft = 0,
  Pending = 1,
  Pagado = 2,
  Overdue = 3,
  Cancelled = 4,
  PrimerRecordatorio = 10,
  SegundoRecordatorio = 11,
  Desactivado = 12,
}

/**
 * Estados terminales: una factura en alguno de estos estados ya no puede
 * recibir un pago manual, por lo que el botón "Pagar" no se muestra.
 */
export const TERMINAL_STATUSES: ReadonlySet<InvoiceStatus> = new Set([
  InvoiceStatus.Pagado,
  InvoiceStatus.Desactivado,
  InvoiceStatus.Cancelled,
])

/**
 * Representación legible en español de cada estado de factura.
 */
export const INVOICE_STATUS_LABELS: Record<InvoiceStatus, string> = {
  [InvoiceStatus.Draft]: 'Borrador',
  [InvoiceStatus.Pending]: 'Pendiente',
  [InvoiceStatus.Pagado]: 'Pagado',
  [InvoiceStatus.Overdue]: 'Vencida',
  [InvoiceStatus.Cancelled]: 'Cancelada',
  [InvoiceStatus.PrimerRecordatorio]: '1er Recordatorio',
  [InvoiceStatus.SegundoRecordatorio]: '2do Recordatorio',
  [InvoiceStatus.Desactivado]: 'Desactivado',
}

/**
 * Forma de una factura tal como la devuelve la API del backend.
 * El campo `status` es el valor numérico del enum `InvoiceStatus`.
 */
export interface Invoice {
  id: string
  clientId: string
  amount: number
  status: InvoiceStatus
  lastStatusTransitionAt: string // ISO-8601 UTC
}
