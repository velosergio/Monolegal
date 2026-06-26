import type { EmailSettings, EmailSettingsInput, ValidateCredentialsResult } from '../types'
import { readErrorMessage } from './readErrorMessage'

const BASE = '/api/settings/email'

/** Carga la configuración de email vía `GET /api/settings/email`. */
export async function getEmailSettings(signal?: AbortSignal): Promise<EmailSettings> {
  const response = await fetch(BASE, { signal })
  if (!response.ok) {
    throw new Error(await readErrorMessage(response, 'No se pudo cargar la configuración de email'))
  }
  return (await response.json()) as EmailSettings
}

/** Persiste la configuración no secreta vía `PUT /api/settings/email`. */
export async function updateEmailSettings(input: EmailSettingsInput): Promise<void> {
  const response = await fetch(BASE, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(input),
  })
  if (!response.ok) {
    throw new Error(await readErrorMessage(response, 'No se pudo guardar la configuración'))
  }
}

/** Valida la credencial del proveedor (activo o indicado) sin enviar correo. */
export async function validateEmailCredentials(
  provider?: EmailSettings['activeProvider']
): Promise<ValidateCredentialsResult> {
  const response = await fetch(`${BASE}/validate`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(provider ? { provider } : {}),
  })
  if (!response.ok) {
    throw new Error(await readErrorMessage(response, 'No se pudo validar la credencial'))
  }
  return (await response.json()) as ValidateCredentialsResult
}
