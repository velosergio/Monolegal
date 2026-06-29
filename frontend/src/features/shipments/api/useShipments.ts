import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { type GetShipmentsParams, getShipments } from './getShipments'

/**
 * Hook de estado de servidor para el listado de envíos (spec 019).
 *
 * Usa `keepPreviousData` para que, al cambiar página/filtro/búsqueda, la página
 * previa permanezca visible hasta que llegue la nueva (sin parpadeo).
 */
export function useShipments(params: GetShipmentsParams) {
  return useQuery({
    queryKey: ['shipments', params],
    queryFn: ({ signal }) => getShipments(params, signal),
    placeholderData: keepPreviousData,
  })
}
