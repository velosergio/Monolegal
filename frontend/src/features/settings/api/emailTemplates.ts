import type { EmailTemplatesResponse, NotificationType, TemplatePreview } from '../types'
import { readErrorMessage } from './readErrorMessage'

const BASE = '/api/settings/email/templates'

/** Cuerpo de edición/vista previa de una plantilla. */
export interface TemplateContent {
  subject: string
  body: string
}

/** Lista las plantillas efectivas y el catálogo de variables admitidas. */
export async function getEmailTemplates(signal?: AbortSignal): Promise<EmailTemplatesResponse> {
  const response = await fetch(BASE, { signal })
  if (!response.ok) {
    throw new Error(await readErrorMessage(response, 'No se pudieron cargar las plantillas'))
  }
  return (await response.json()) as EmailTemplatesResponse
}

/** Actualiza la plantilla del tipo indicado. */
export async function updateEmailTemplate(
  type: NotificationType,
  content: TemplateContent
): Promise<void> {
  const response = await fetch(`${BASE}/${type}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(content),
  })
  if (!response.ok) {
    throw new Error(await readErrorMessage(response, 'No se pudo guardar la plantilla'))
  }
}

/** Restablece la plantilla del tipo indicado a su contenido por defecto. */
export async function resetEmailTemplate(type: NotificationType): Promise<void> {
  const response = await fetch(`${BASE}/${type}/reset`, { method: 'POST' })
  if (!response.ok) {
    throw new Error(await readErrorMessage(response, 'No se pudo restablecer la plantilla'))
  }
}

/** Renderiza una plantilla con datos de ejemplo (vista previa server-side). */
export async function previewEmailTemplate(
  type: NotificationType,
  content: TemplateContent
): Promise<TemplatePreview> {
  const response = await fetch(`${BASE}/${type}/preview`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(content),
  })
  if (!response.ok) {
    throw new Error(await readErrorMessage(response, 'No se pudo generar la vista previa'))
  }
  return (await response.json()) as TemplatePreview
}
