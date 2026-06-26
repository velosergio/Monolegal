import type { NotificationType, SendTestEmailResult } from '../types'
import { readErrorMessage } from './readErrorMessage'

/** Variables del envío de prueba. */
export interface SendTestEmailVariables {
  to: string
  templateType: NotificationType
}

/** Envía un correo de prueba vía `POST /api/settings/email/test`. */
export async function sendTestEmail(
  variables: SendTestEmailVariables
): Promise<SendTestEmailResult> {
  const response = await fetch('/api/settings/email/test', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(variables),
  })
  if (!response.ok) {
    throw new Error(await readErrorMessage(response, 'No se pudo enviar el correo de prueba'))
  }
  return (await response.json()) as SendTestEmailResult
}
