import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { DashboardPage } from '@/features/dashboard/components/DashboardPage'
import type { InvoiceStats } from '@/features/dashboard/types'
import { mockFetchJson, renderWithQuery } from '../../test-utils'

const stats: InvoiceStats = {
  totalInvoices: 100,
  byStatus: { pending: 40, pagado: 60 },
  byClient: { 'cliente-001': 60, 'cliente-002': 40 },
}

describe('DashboardPage', () => {
  it('muestra un skeleton mientras carga', () => {
    mockFetchJson(stats)
    renderWithQuery(<DashboardPage />)
    expect(screen.getByRole('status')).toBeInTheDocument()
  })

  it('muestra las tarjetas y el total al cargar', async () => {
    mockFetchJson(stats)
    renderWithQuery(<DashboardPage />)

    expect(await screen.findByText('100')).toBeInTheDocument()
    expect(screen.getByText(/total de facturas/i)).toBeInTheDocument()
  })

  it('muestra el indicador de último refresh con botón de actualizar', async () => {
    mockFetchJson(stats)
    renderWithQuery(<DashboardPage />)

    await screen.findByText('100')
    expect(screen.getByText(/actualizado/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /actualizar/i })).toBeInTheDocument()
  })

  it('muestra el estado vacío cuando no hay facturas', async () => {
    mockFetchJson({ totalInvoices: 0, byStatus: {}, byClient: {} } satisfies InvoiceStats)
    renderWithQuery(<DashboardPage />)

    expect(await screen.findByText(/no hay facturas/i)).toBeInTheDocument()
  })

  it('muestra un error con reintento ante un fallo', async () => {
    const fetchMock = mockFetchJson({ error: 'boom' }, { ok: false, status: 500 })
    renderWithQuery(<DashboardPage />)

    expect(await screen.findByText(/no se pudieron cargar las estadísticas/i)).toBeInTheDocument()
    const before = fetchMock.mock.calls.length
    await userEvent.click(screen.getByRole('button', { name: /reintentar/i }))
    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThan(before)
    })
  })
})
