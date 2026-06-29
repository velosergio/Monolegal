import type { InvoiceStatus } from '../invoices/types'

/**
 * Estado de envío de la notificación de una factura (spec 019). Los cuatro primeros los devuelve la
 * API (derivados del resultado de la última notificación); `retrying` es un estado **transitorio**
 * que sólo existe en el cliente mientras una mutación de reenvío está en curso.
 */
export type ServerSendStatus = 'pending' | 'sent' | 'failed' | 'skipped'
export type SendStatus = ServerSendStatus | 'retrying'

/** Estados de envío filtrables (los que expone la API). */
export const FILTERABLE_SEND_STATUSES = [
  'pending',
  'sent',
  'failed',
  'skipped',
] as const satisfies readonly ServerSendStatus[]

/** Etiqueta legible en español por estado de envío. */
export const SEND_STATUS_LABELS: Record<SendStatus, string> = {
  pending: 'Pendiente',
  sent: 'Enviado',
  failed: 'Fallido',
  skipped: 'Omitido',
  retrying: 'Reintentando',
}

/** Devuelve la etiqueta legible de un estado de envío, con respaldo al valor en bruto. */
export function sendStatusLabel(status: SendStatus): string {
  return SEND_STATUS_LABELS[status] ?? status
}

/**
 * Ítem de envío tal como lo devuelve `GET /api/invoices/shipments` (spec 019).
 */
export interface Shipment {
  id: string
  clientId: string
  /** Nombre legible del cliente (respaldo al clientId si no se resolvió). */
  clientName: string
  /** Correo de destino resuelto; `null` si no es resoluble. */
  clientEmail: string | null
  /** Estado de la factura (contexto). */
  status: InvoiceStatus
  /** Estado de envío derivado del resultado de la última notificación. */
  sendStatus: ServerSendStatus
  /** Fecha/hora del último intento (ISO-8601 UTC) o `null` si pendiente. */
  lastAttemptAt: string | null
  /** Reintentos del aviso vigente. */
  retryCount: number
  /** Motivo del último error (sólo cuando `sendStatus === 'failed'`). */
  lastError: string | null
}

/** Respuesta paginada del listado de envíos. */
export interface PagedShipments {
  data: Shipment[]
  total: number
  pageSize: number
}
