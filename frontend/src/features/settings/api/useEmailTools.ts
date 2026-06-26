import { useMutation } from '@tanstack/react-query'
import { resendFailedNotifications, sanitizeStuckNotifications } from './emailTools'

/** Mutación de reenvío masivo de notificaciones fallidas. */
export function useResendFailed() {
  return useMutation({ mutationFn: () => resendFailedNotifications() })
}

/** Mutación de saneamiento de notificaciones atascadas. */
export function useSanitizeStuck() {
  return useMutation({ mutationFn: () => sanitizeStuckNotifications() })
}
