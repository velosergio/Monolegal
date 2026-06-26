import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import type { EmailProvider, EmailSettingsInput } from '../types'
import { getEmailSettings, updateEmailSettings, validateEmailCredentials } from './emailSettings'

/** Clave de caché de la configuración de email. */
export const emailSettingsKey = ['settings', 'email'] as const

/** Hook de lectura de la configuración de email. */
export function useEmailSettings() {
  return useQuery({
    queryKey: emailSettingsKey,
    queryFn: ({ signal }) => getEmailSettings(signal),
  })
}

/** Mutación para persistir la configuración de email; invalida la caché al éxito. */
export function useUpdateEmailSettings() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (input: EmailSettingsInput) => updateEmailSettings(input),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: emailSettingsKey })
    },
  })
}

/** Mutación para validar la credencial del proveedor (sin enviar correo). */
export function useValidateEmailCredentials() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (provider?: EmailProvider) => validateEmailCredentials(provider),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: emailSettingsKey })
    },
  })
}
