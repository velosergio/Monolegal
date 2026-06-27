import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { describe, expect, it, vi } from 'vitest'
import { useTransitionInvoice } from '@/features/invoices/api/useTransitionInvoice'
import type { InvoiceDetail } from '@/features/invoices/types'
import { mockFetchJson } from '../../test-utils'

const updated: InvoiceDetail = {
  id: 'abcdef1234567890',
  clientId: 'client-1',
  clientName: 'Acme S.A.',
  amount: 1_500_000,
  dueDate: '2026-02-01T00:00:00.000Z',
  items: [{ description: 'Concepto', quantity: 1, unitPrice: 1_500_000, subtotal: 1_500_000 }],
  status: 'segundorecordatorio',
  createdAt: '2026-01-01T08:00:00.000Z',
  updatedAt: '2026-06-02T10:30:00.000Z',
  remindersCount: 2,
  lastReminderSentAt: '2026-06-02T10:30:00.000Z',
  lastStatusTransitionAt: '2026-06-02T10:30:00.000Z',
  statusHistory: [
    {
      from: 'primerrecordatorio',
      to: 'segundorecordatorio',
      at: '2026-06-02T10:30:00.000Z',
      source: 'manual',
    },
  ],
  allowedTransitions: ['pagado', 'desactivado'],
}

function createWrapper(client: QueryClient) {
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={client}>{children}</QueryClientProvider>
  )
}

describe('useTransitionInvoice', () => {
  it('al tener éxito invalida las claves de detalle, listado y estadísticas', async () => {
    mockFetchJson(updated)
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    })
    const invalidateSpy = vi.spyOn(client, 'invalidateQueries')

    const { result } = renderHook(() => useTransitionInvoice(), {
      wrapper: createWrapper(client),
    })

    result.current.mutate({ id: updated.id, newStatus: 'segundorecordatorio' })

    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    const invalidatedKeys = invalidateSpy.mock.calls.map((call) =>
      JSON.stringify(call[0]?.queryKey)
    )
    expect(invalidatedKeys).toContain(JSON.stringify(['invoice', updated.id]))
    expect(invalidatedKeys).toContain(JSON.stringify(['invoices']))
    expect(invalidatedKeys).toContain(JSON.stringify(['invoice-stats']))
  })

  it('envía el nuevo estado al endpoint de transición', async () => {
    const fetchMock = mockFetchJson(updated)
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    })

    const { result } = renderHook(() => useTransitionInvoice(), {
      wrapper: createWrapper(client),
    })

    result.current.mutate({ id: updated.id, newStatus: 'segundorecordatorio' })
    await waitFor(() => expect(result.current.isSuccess).toBe(true))

    const [url, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit]
    expect(url).toContain(`/api/invoices/transition/${updated.id}`)
    expect(init.method).toBe('POST')
    expect(JSON.parse(init.body as string)).toEqual({ newStatus: 'segundorecordatorio' })
  })
})
