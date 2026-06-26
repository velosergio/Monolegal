import type { InvoiceDetail, InvoiceStatus } from '../types'
import { InvoiceNotFoundError } from './getInvoiceDetail'

/**
 * Ejecuta una transición manual de estado vía `POST /api/invoices/transition/{id}`.
 *
 * Devuelve el `InvoiceDetail` actualizado (con el historial extendido y los nuevos
 * `allowedTransitions`). Lanza {@link InvoiceNotFoundError} ante un 404 y un `Error`
 * con mensaje legible ante un 400/otros fallos, preservando el mensaje del backend
 * cuando está disponible.
 */
export async function transitionInvoice(
  id: string,
  newStatus: InvoiceStatus
): Promise<InvoiceDetail> {
  const response = await fetch(`/api/invoices/transition/${encodeURIComponent(id)}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ newStatus }),
  })

  if (response.status === 404) {
    throw new InvoiceNotFoundError(id)
  }

  if (!response.ok) {
    throw new Error(await readErrorMessage(response))
  }

  return (await response.json()) as InvoiceDetail
}

/** Extrae un mensaje de error legible del cuerpo (`{ error }` o ValidationProblem). */
async function readErrorMessage(response: Response): Promise<string> {
  const fallback = `No se pudo cambiar el estado de la factura (${response.status}).`
  try {
    const body = (await response.json()) as {
      error?: string
      errors?: Record<string, string[]>
    }
    if (typeof body?.error === 'string' && body.error.length > 0) {
      return body.error
    }
    if (body?.errors) {
      const first = Object.values(body.errors).flat()[0]
      if (typeof first === 'string' && first.length > 0) return first
    }
  } catch {
    // Cuerpo no-JSON: usar el mensaje de respaldo.
  }
  return fallback
}
