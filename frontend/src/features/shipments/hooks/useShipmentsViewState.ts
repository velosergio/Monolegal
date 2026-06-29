import { useCallback, useState } from 'react'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import type { ServerSendStatus } from '../types'

export const PAGE_SIZE = 10
const SEARCH_DEBOUNCE_MS = 300

export interface ShipmentsViewState {
  sendStatus: ServerSendStatus | 'all'
  /** Texto crudo del input de búsqueda (controla el campo). */
  searchInput: string
  /** Término estabilizado (debounced) que alimenta la consulta. */
  search: string
  page: number
  /** `true` cuando hay filtro o búsqueda activos (para diferenciar el empty state). */
  hasActiveFilters: boolean
  setSendStatus: (sendStatus: ServerSendStatus | 'all') => void
  setSearchInput: (search: string) => void
  setPage: (page: number) => void
}

/**
 * Estado de presentación del listado de envíos: filtro por estado de envío, término de búsqueda
 * (con debounce derivado) y página actual. Cambiar el estado o el texto reinicia la página a 1.
 */
export function useShipmentsViewState(): ShipmentsViewState {
  const [sendStatus, setSendStatusRaw] = useState<ServerSendStatus | 'all'>('all')
  const [searchInput, setSearchInputRaw] = useState('')
  const [page, setPage] = useState(1)
  const search = useDebouncedValue(searchInput, SEARCH_DEBOUNCE_MS)

  const setSendStatus = useCallback((next: ServerSendStatus | 'all') => {
    setSendStatusRaw(next)
    setPage(1)
  }, [])

  const setSearchInput = useCallback((next: string) => {
    setSearchInputRaw(next)
    setPage(1)
  }, [])

  const hasActiveFilters = sendStatus !== 'all' || search.trim().length > 0

  return {
    sendStatus,
    searchInput,
    search,
    page,
    hasActiveFilters,
    setSendStatus,
    setSearchInput,
    setPage,
  }
}
