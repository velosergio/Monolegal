import { useMutation, useQueryClient } from '@tanstack/react-query'
import { cancelNotification, resendInvoice } from './shipmentMutations'

/** Invalida las consultas afectadas por una acción de envío (listado, facturas y estadísticas). */
function useInvalidateShipments() {
  const queryClient = useQueryClient()
  return () => {
    queryClient.invalidateQueries({ queryKey: ['shipments'] })
    queryClient.invalidateQueries({ queryKey: ['invoices'] })
    queryClient.invalidateQueries({ queryKey: ['invoice-stats'] })
  }
}

/**
 * Mutación de reenvío por factura (spec 019, US2). `variables` contiene el id de la factura, lo que
 * permite a la tabla mostrar el estado transitorio "reintentando" sólo en la fila en curso.
 */
export function useResendInvoice() {
  const invalidate = useInvalidateShipments()
  return useMutation({
    mutationFn: (id: string) => resendInvoice(id),
    onSuccess: invalidate,
  })
}

/** Mutación de cancelación de envío por factura (spec 019, US4). */
export function useCancelNotification() {
  const invalidate = useInvalidateShipments()
  return useMutation({
    mutationFn: (id: string) => cancelNotification(id),
    onSuccess: invalidate,
  })
}
