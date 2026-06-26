import { useQuery } from '@tanstack/react-query'
import { getInvoiceStats } from './getInvoiceStats'

/**
 * Hook de estado de servidor para las estadísticas del dashboard (spec 015, US4).
 * Sin auto-refresco periódico (FR-021a): el refresco es manual vía `refetch`.
 */
export function useInvoiceStats() {
  return useQuery({
    queryKey: ['invoice-stats'],
    queryFn: ({ signal }) => getInvoiceStats(signal),
  })
}
