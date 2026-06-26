import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { NotificationType } from '../types'
import {
  getEmailTemplates,
  previewEmailTemplate,
  resetEmailTemplate,
  type TemplateContent,
  updateEmailTemplate,
} from './emailTemplates'

/** Clave de caché de las plantillas de email (privada del módulo). */
const emailTemplatesKey = ['settings', 'email', 'templates'] as const

/** Hook de lectura de las plantillas de email. */
export function useEmailTemplates() {
  return useQuery({
    queryKey: emailTemplatesKey,
    queryFn: ({ signal }) => getEmailTemplates(signal),
  })
}

/** Mutación de actualización de una plantilla; invalida la caché al éxito. */
export function useUpdateEmailTemplate() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ type, content }: { type: NotificationType; content: TemplateContent }) =>
      updateEmailTemplate(type, content),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: emailTemplatesKey })
    },
  })
}

/** Mutación de restablecimiento de una plantilla; invalida la caché al éxito. */
export function useResetEmailTemplate() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (type: NotificationType) => resetEmailTemplate(type),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: emailTemplatesKey })
    },
  })
}

/** Mutación de vista previa de una plantilla con datos de ejemplo. */
export function usePreviewEmailTemplate() {
  return useMutation({
    mutationFn: ({ type, content }: { type: NotificationType; content: TemplateContent }) =>
      previewEmailTemplate(type, content),
  })
}
