import type { ResendFailedResult, SanitizeResult } from '../types'
import { readErrorMessage } from './readErrorMessage'

const BASE = '/api/settings/email/tools'

/** Reenvía las notificaciones de las facturas con último resultado fallido. */
export async function resendFailedNotifications(): Promise<ResendFailedResult> {
  const response = await fetch(`${BASE}/resend-failed`, { method: 'POST' })
  if (!response.ok) {
    throw new Error(
      await readErrorMessage(response, 'No se pudo reenviar las notificaciones fallidas')
    )
  }
  return (await response.json()) as ResendFailedResult
}

/** Sanea las notificaciones atascadas (marca None → Failed). Requiere confirmación previa. */
export async function sanitizeStuckNotifications(): Promise<SanitizeResult> {
  const response = await fetch(`${BASE}/sanitize`, { method: 'POST' })
  if (!response.ok) {
    throw new Error(
      await readErrorMessage(response, 'No se pudo sanear las notificaciones atascadas')
    )
  }
  return (await response.json()) as SanitizeResult
}
