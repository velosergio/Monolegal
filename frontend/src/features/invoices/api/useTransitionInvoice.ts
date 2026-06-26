import { useMutation, useQueryClient } from '@tanstack/react-query'
import type { InvoiceStatus } from '../types'
import { transitionInvoice } from './transitionInvoice'
import { invoiceDetailKey } from './useInvoiceDetail'

/** Variables de la mutación de cambio de estado. */
export interface TransitionInvoiceVariables {
  id: string
  newStatus: InvoiceStatus
}

/**
 * Mutación de cambio de estado de una factura (spec 015, US3).
 *
 * Al tener éxito fija el detalle devuelto en la caché e invalida de forma dirigida
 * (research D5) las tres claves afectadas para mantener coherentes el modal, el
 * listado y el dashboard sin recargar la página.
 */
export function useTransitionInvoice() {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, newStatus }: TransitionInvoiceVariables) => transitionInvoice(id, newStatus),
    onSuccess: (updated, { id }) => {
      queryClient.setQueryData(invoiceDetailKey(id), updated)
      queryClient.invalidateQueries({ queryKey: invoiceDetailKey(id) })
      queryClient.invalidateQueries({ queryKey: ['invoices'] })
      queryClient.invalidateQueries({ queryKey: ['invoice-stats'] })
    },
  })
}
