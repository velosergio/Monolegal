import { act, renderHook } from '@testing-library/react'
import type { ReactNode } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { useSelectedInvoice } from '@/features/invoices/hooks/useSelectedInvoice'

function wrapper({ children }: { children: ReactNode }) {
  return <MemoryRouter initialEntries={['/facturas']}>{children}</MemoryRouter>
}

describe('useSelectedInvoice', () => {
  it('comienza sin selección', () => {
    const { result } = renderHook(() => useSelectedInvoice(), { wrapper })
    expect(result.current.selectedId).toBeNull()
  })

  it('open() selecciona una factura y close() la limpia', () => {
    const { result } = renderHook(() => useSelectedInvoice(), { wrapper })

    act(() => result.current.open('abc123'))
    expect(result.current.selectedId).toBe('abc123')

    act(() => result.current.close())
    expect(result.current.selectedId).toBeNull()
  })

  it('refleja una selección presente en la URL inicial', () => {
    const { result } = renderHook(() => useSelectedInvoice(), {
      wrapper: ({ children }: { children: ReactNode }) => (
        <MemoryRouter initialEntries={['/facturas?factura=xyz']}>{children}</MemoryRouter>
      ),
    })
    expect(result.current.selectedId).toBe('xyz')
  })
})
