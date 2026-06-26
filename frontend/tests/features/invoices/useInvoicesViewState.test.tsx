import { act, renderHook } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  SEARCH_DEBOUNCE_MS,
  useInvoicesViewState,
} from '@/features/invoices/hooks/useInvoicesViewState'

afterEach(() => {
  vi.useRealTimers()
})

describe('useInvoicesViewState', () => {
  it('inicializa con estado "all", búsqueda vacía y página 1', () => {
    const { result } = renderHook(() => useInvoicesViewState())
    expect(result.current.status).toBe('all')
    expect(result.current.searchInput).toBe('')
    expect(result.current.search).toBe('')
    expect(result.current.page).toBe(1)
  })

  it('cambia de página', () => {
    const { result } = renderHook(() => useInvoicesViewState())
    act(() => result.current.setPage(3))
    expect(result.current.page).toBe(3)
  })

  it('reinicia la página al cambiar el filtro de estado', () => {
    const { result } = renderHook(() => useInvoicesViewState())
    act(() => result.current.setPage(3))
    act(() => result.current.setStatus('pagado'))
    expect(result.current.status).toBe('pagado')
    expect(result.current.page).toBe(1)
  })

  it('reinicia la página al cambiar el texto de búsqueda', () => {
    const { result } = renderHook(() => useInvoicesViewState())
    act(() => result.current.setPage(3))
    act(() => result.current.setSearchInput('acme'))
    expect(result.current.searchInput).toBe('acme')
    expect(result.current.page).toBe(1)
  })

  it('estabiliza el término de búsqueda tras el debounce', () => {
    vi.useFakeTimers()
    const { result } = renderHook(() => useInvoicesViewState())

    act(() => result.current.setSearchInput('acme'))
    expect(result.current.searchInput).toBe('acme')
    expect(result.current.search).toBe('')

    act(() => {
      vi.advanceTimersByTime(SEARCH_DEBOUNCE_MS)
    })
    expect(result.current.search).toBe('acme')
  })
})
