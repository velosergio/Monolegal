import type { InvoiceDetail } from '../types'

/**
 * Error de "no encontrada" (HTTP 404) del detalle de factura, para que la UI distinga
 * el caso de factura inexistente de un fallo genérico.
 */
export class InvoiceNotFoundError extends Error {
  constructor(id: string) {
    super(`La factura ${id} no existe.`)
    this.name = 'InvoiceNotFoundError'
  }
}

/**
 * Obtiene el detalle completo de una factura desde `GET /api/invoices/{id}`.
 * Lanza {@link InvoiceNotFoundError} ante un 404 y un `Error` legible ante otros fallos,
 * para que TanStack Query los exponga como estado de error.
 */
export async function getInvoiceDetail(id: string, signal?: AbortSignal): Promise<InvoiceDetail> {
  const response = await fetch(`/api/invoices/${encodeURIComponent(id)}`, { signal })

  if (response.status === 404) {
    throw new InvoiceNotFoundError(id)
  }

  if (!response.ok) {
    throw new Error(`No se pudo cargar el detalle de la factura (${response.status}).`)
  }

  return (await response.json()) as InvoiceDetail
}
