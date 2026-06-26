import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { type GetClientsParams, getClients } from './getClients'

/** Hook de estado de servidor para el listado de clientes (spec 018, RF-012/RF-013). */
export function useClients(params: GetClientsParams) {
  return useQuery({
    queryKey: ['clients', params],
    queryFn: ({ signal }) => getClients(params, signal),
    placeholderData: keepPreviousData,
  })
}
