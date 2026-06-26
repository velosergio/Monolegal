import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  useCreateInvoice,
  useDeleteInvoice,
  useUpdateInvoice,
} from '@/features/invoices/api/invoiceMutations'
import type { InvoiceFormValues } from '@/features/invoices/types'

const values: InvoiceFormValues = {
  clientId: 'c1',
  dueDate: '2026-09-01',
  items: [{ rowId: 'row-1', description: 'Asesoría', quantity: 2, unitPrice: 150 }],
}

function wrapper(client: QueryClient) {
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={client}>{children}</QueryClientProvider>
  )
}

function newClient() {
  return new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
}

afterEach(() => vi.restoreAllMocks())

describe('mutaciones de facturas', () => {
  it('useCreateInvoice hace POST y manda items sin amount; invalida listado y stats', async () => {
    const fetchMock = vi.fn((_url: string, _init: RequestInit) =>
      Promise.resolve({
        ok: true,
        status: 201,
        json: () => Promise.resolve({ id: 'i1' }),
      } as Response)
    )
    vi.stubGlobal('fetch', fetchMock)
    const client = newClient()
    const invalidateSpy = vi.spyOn(client, 'invalidateQueries')

    const { result } = renderHook(() => useCreateInvoice(), { wrapper: wrapper(client) })
    result.current.mutate(values)
    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    const [url, init] = fetchMock.mock.calls[0]
    expect(url).toBe('/api/invoices')
    expect(init.method).toBe('POST')
    const body = JSON.parse(init.body as string)
    expect(body.amount).toBeUndefined()
    expect(body.items).toHaveLength(1)

    const keys = invalidateSpy.mock.calls.map((c) => JSON.stringify(c[0]?.queryKey))
    expect(keys).toContain(JSON.stringify(['invoices']))
    expect(keys).toContain(JSON.stringify(['invoice-stats']))
  })

  it('useUpdateInvoice hace PUT al id correcto', async () => {
    const fetchMock = vi.fn(() =>
      Promise.resolve({
        ok: true,
        status: 200,
        json: () => Promise.resolve({ id: 'i1' }),
      } as Response)
    )
    vi.stubGlobal('fetch', fetchMock)
    const client = newClient()

    const { result } = renderHook(() => useUpdateInvoice(), { wrapper: wrapper(client) })
    result.current.mutate({ id: 'i1', values })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/invoices/i1',
      expect.objectContaining({ method: 'PUT' })
    )
  })

  it('useDeleteInvoice hace DELETE', async () => {
    const fetchMock = vi.fn(() =>
      Promise.resolve({ ok: true, status: 204, json: () => Promise.resolve({}) } as Response)
    )
    vi.stubGlobal('fetch', fetchMock)
    const client = newClient()

    const { result } = renderHook(() => useDeleteInvoice(), { wrapper: wrapper(client) })
    result.current.mutate('i1')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(fetchMock).toHaveBeenCalledWith(
      '/api/invoices/i1',
      expect.objectContaining({ method: 'DELETE' })
    )
  })
})
