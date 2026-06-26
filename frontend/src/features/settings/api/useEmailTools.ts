import { useMutation, useQueryClient } from '@tanstack/react-query'
import { resendFailedNotifications, sanitizeStuckNotifications } from './emailTools'

/** Mutación de reenvío masivo de notificaciones fallidas; refresca facturas y estadísticas. */
export function useResendFailed() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => resendFailedNotifications(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices'] })
      queryClient.invalidateQueries({ queryKey: ['invoice'] })
      queryClient.invalidateQueries({ queryKey: ['invoice-stats'] })
    },
  })
}

/** Mutación de saneamiento de notificaciones atascadas; refresca facturas y estadísticas. */
export function useSanitizeStuck() {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: () => sanitizeStuckNotifications(),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoices'] })
      queryClient.invalidateQueries({ queryKey: ['invoice'] })
      queryClient.invalidateQueries({ queryKey: ['invoice-stats'] })
    },
  })
}
