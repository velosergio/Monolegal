import { useQuery } from '@tanstack/react-query'
import { getInvoiceDetail } from './getInvoiceDetail'

/**
 * Clave de caché del detalle de una factura. Compartida por el modal y la mutación de
 * cambio de estado para invalidación dirigida.
 */
export function invoiceDetailKey(id: string) {
  return ['invoice', id] as const
}

/**
 * Hook de estado de servidor para el detalle de una factura. Solo se ejecuta cuando hay un
 * `id` (el modal está abierto). No reintenta ante un 404 (factura inexistente).
 */
export function useInvoiceDetail(id: string | null) {
  return useQuery({
    queryKey: ['invoice', id],
    queryFn: ({ signal }) => getInvoiceDetail(id as string, signal),
    enabled: id != null,
  })
}
