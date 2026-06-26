import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import {
  ClientHasInvoicesError,
  useCreateClient,
  useDeleteClient,
  useUpdateClient,
} from '@/features/clients/api/mutations'
import type { Client, ClientFormValues } from '@/features/clients/types'

const sample: Client = {
  id: 'c1',
  name: 'Acme',
  email: 'acme@correo.com',
  phone: null,
  address: null,
  createdAt: '2026-01-01T00:00:00Z',
  updatedAt: '2026-01-01T00:00:00Z',
}

const values: ClientFormValues = { name: 'Acme', email: 'acme@correo.com', phone: '', address: '' }

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

describe('mutaciones de clientes', () => {
  it('useCreateClient envía POST y invalida ["clients"]', async () => {
    const fetchMock = vi.fn(() =>
      Promise.resolve({ ok: true, status: 201, json: () => Promise.resolve(sample) } as Response)
    )
    vi.stubGlobal('fetch', fetchMock)
    const client = newClient()
    const invalidateSpy = vi.spyOn(client, 'invalidateQueries')

    const { result } = renderHook(() => useCreateClient(), { wrapper: wrapper(client) })
    result.current.mutate(values)
    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(fetchMock).toHaveBeenCalledWith('/api/clients', expect.objectContaining({ method: 'POST' }))
    expect(invalidateSpy.mock.calls.map((c) => JSON.stringify(c[0]?.queryKey))).toContain(
      JSON.stringify(['clients'])
    )
  })

  it('useUpdateClient envía PUT al id correcto', async () => {
    const fetchMock = vi.fn(() =>
      Promise.resolve({ ok: true, status: 200, json: () => Promise.resolve(sample) } as Response)
    )
    vi.stubGlobal('fetch', fetchMock)
    const client = newClient()

    const { result } = renderHook(() => useUpdateClient(), { wrapper: wrapper(client) })
    result.current.mutate({ id: 'c1', values })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(fetchMock).toHaveBeenCalledWith('/api/clients/c1', expect.objectContaining({ method: 'PUT' }))
  })

  it('useDeleteClient lanza ClientHasInvoicesError ante 409', async () => {
    const fetchMock = vi.fn(() =>
      Promise.resolve({
        ok: false,
        status: 409,
        json: () => Promise.resolve({ error: 'El cliente tiene facturas asociadas.' }),
      } as Response)
    )
    vi.stubGlobal('fetch', fetchMock)
    const client = newClient()

    const { result } = renderHook(() => useDeleteClient(), { wrapper: wrapper(client) })
    result.current.mutate('c1')
    await waitFor(() => expect(result.current.isError).toBe(true))

    expect(result.current.error).toBeInstanceOf(ClientHasInvoicesError)
  })

  it('useDeleteClient resuelve con 204', async () => {
    const fetchMock = vi.fn(() =>
      Promise.resolve({ ok: true, status: 204, json: () => Promise.resolve({}) } as Response)
    )
    vi.stubGlobal('fetch', fetchMock)
    const client = newClient()

    const { result } = renderHook(() => useDeleteClient(), { wrapper: wrapper(client) })
    result.current.mutate('c1')
    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    expect(fetchMock).toHaveBeenCalledWith('/api/clients/c1', expect.objectContaining({ method: 'DELETE' }))
  })
})
