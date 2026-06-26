import type { InvoiceStats } from '../types'

/**
 * Obtiene las estadísticas agregadas desde `GET /api/invoices/stats`.
 * Lanza un `Error` legible ante un fallo para que TanStack Query lo exponga.
 */
export async function getInvoiceStats(signal?: AbortSignal): Promise<InvoiceStats> {
  const response = await fetch('/api/invoices/stats', { signal })

  if (!response.ok) {
    throw new Error(`No se pudieron cargar las estadísticas (${response.status}).`)
  }

  return (await response.json()) as InvoiceStats
}
