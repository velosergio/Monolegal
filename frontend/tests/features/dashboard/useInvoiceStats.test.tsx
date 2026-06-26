import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { describe, expect, it } from 'vitest'
import { useInvoiceStats } from '@/features/dashboard/api/useInvoiceStats'
import type { InvoiceStats } from '@/features/dashboard/types'
import { topClients } from '@/features/dashboard/utils'
import { mockFetchJson } from '../../test-utils'

const stats: InvoiceStats = {
  totalInvoices: 100,
  byStatus: { pending: 40, pagado: 60 },
  byClient: {
    'cliente-001': 30,
    'cliente-002': 25,
    'cliente-003': 20,
    'cliente-004': 10,
    'cliente-005': 8,
    'cliente-006': 5,
    'cliente-007': 2,
  },
}

describe('topClients', () => {
  it('devuelve top-N ordenado de mayor a menor', () => {
    const result = topClients(stats.byClient, 3)
    expect(result[0]).toEqual({ label: 'cliente-001', value: 30 })
    expect(result[1]).toEqual({ label: 'cliente-002', value: 25 })
    expect(result[2]).toEqual({ label: 'cliente-003', value: 20 })
  })

  it('agrupa el resto bajo "Otros" cuando hay más de N clientes', () => {
    const result = topClients(stats.byClient, 3)
    const otros = result.find((entry) => entry.label === 'Otros')
    expect(otros).toBeDefined()
    // 10 + 8 + 5 + 2 = 25
    expect(otros?.value).toBe(25)
  })

  it('no añade "Otros" cuando el número de clientes no supera N', () => {
    const few = { a: 3, b: 2 }
    const result = topClients(few, 5)
    expect(result).toHaveLength(2)
    expect(result.some((entry) => entry.label === 'Otros')).toBe(false)
  })

  it('devuelve una lista vacía sin clientes', () => {
    expect(topClients({}, 5)).toEqual([])
  })
})

describe('useInvoiceStats', () => {
  function wrapper({ children }: { children: ReactNode }) {
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    })
    return <QueryClientProvider client={client}>{children}</QueryClientProvider>
  }

  it('consulta las estadísticas y expone los datos', async () => {
    mockFetchJson(stats)
    const { result } = renderHook(() => useInvoiceStats(), { wrapper })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))
    expect(result.current.data?.totalInvoices).toBe(100)
  })
})
