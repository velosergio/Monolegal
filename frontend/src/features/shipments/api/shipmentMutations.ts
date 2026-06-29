import type { Shipment } from '../types'

async function readErrorMessage(response: Response, fallback: string): Promise<string> {
  try {
    const body = (await response.json()) as { error?: string }
    if (typeof body?.error === 'string' && body.error.length > 0) return body.error
  } catch {
    // Cuerpo no-JSON.
  }
  return fallback
}

/** POST /api/invoices/{id}/resend — reenvía la notificación de una factura (spec 019, US2). */
export async function resendInvoice(id: string): Promise<Shipment> {
  const response = await fetch(`/api/invoices/${encodeURIComponent(id)}/resend`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
  })
  if (!response.ok) {
    throw new Error(
      await readErrorMessage(response, `No se pudo reenviar la notificación (${response.status}).`)
    )
  }
  return (await response.json()) as Shipment
}

/** POST /api/invoices/{id}/cancel-notification — marca como omitida una factura pendiente (spec 019, US4). */
export async function cancelNotification(id: string): Promise<Shipment> {
  const response = await fetch(`/api/invoices/${encodeURIComponent(id)}/cancel-notification`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
  })
  if (!response.ok) {
    throw new Error(
      await readErrorMessage(response, `No se pudo cancelar el envío (${response.status}).`)
    )
  }
  return (await response.json()) as Shipment
}
