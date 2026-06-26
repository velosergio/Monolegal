import { useCallback, useState } from 'react'
import { useDebouncedValue } from '@/hooks/use-debounced-value'
import type { InvoiceStatus } from '../types'

export const PAGE_SIZE = 10
export const SEARCH_DEBOUNCE_MS = 300

export interface InvoicesViewState {
  status: InvoiceStatus | 'all'
  /** Texto crudo del input de búsqueda (controla el campo). */
  searchInput: string
  /** Término estabilizado (debounced) que alimenta la consulta. */
  search: string
  page: number
  setStatus: (status: InvoiceStatus | 'all') => void
  setSearchInput: (search: string) => void
  setPage: (page: number) => void
}

/**
 * Estado de presentación del listado: filtro por estado, término de búsqueda
 * (con debounce derivado, sin efectos) y página actual. Cambiar el estado o el
 * texto de búsqueda reinicia la página a 1 (FR-014).
 */
export function useInvoicesViewState(): InvoicesViewState {
  const [status, setStatusRaw] = useState<InvoiceStatus | 'all'>('all')
  const [searchInput, setSearchInputRaw] = useState('')
  const [page, setPage] = useState(1)
  const search = useDebouncedValue(searchInput, SEARCH_DEBOUNCE_MS)

  const setStatus = useCallback((next: InvoiceStatus | 'all') => {
    setStatusRaw(next)
    setPage(1)
  }, [])

  const setSearchInput = useCallback((next: string) => {
    setSearchInputRaw(next)
    setPage(1)
  }, [])

  return { status, searchInput, search, page, setStatus, setSearchInput, setPage }
}
