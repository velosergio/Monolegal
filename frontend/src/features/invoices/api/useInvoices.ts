import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { type GetInvoicesParams, getInvoices } from './getInvoices'

/**
 * Hook de estado de servidor para el listado de facturas.
 *
 * Usa `keepPreviousData` para que, al cambiar página/filtro/búsqueda, la página
 * previa permanezca visible hasta que llegue la nueva (sin parpadeo — FR-017).
 */
export function useInvoices(params: GetInvoicesParams) {
  return useQuery({
    queryKey: ['invoices', params],
    queryFn: ({ signal }) => getInvoices(params, signal),
    placeholderData: keepPreviousData,
  })
}
