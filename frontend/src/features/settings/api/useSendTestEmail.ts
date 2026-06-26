import { useMutation } from '@tanstack/react-query'
import { type SendTestEmailVariables, sendTestEmail } from './emailTest'

/** Mutación de envío de correo de prueba (acción puntual, sin caché). */
export function useSendTestEmail() {
  return useMutation({
    mutationFn: (variables: SendTestEmailVariables) => sendTestEmail(variables),
  })
}
