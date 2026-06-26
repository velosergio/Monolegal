import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { InvoicesPage } from '@/features/invoices/components/InvoicesPage'
import { mockFetchJson, renderWithQuery } from '../../test-utils'

describe('InvoicesPage — estado de error', () => {
  it('muestra mensaje de error y permite reintentar cuando la API falla', async () => {
    const fetchMock = mockFetchJson({ error: 'boom' }, { ok: false, status: 500 })
    renderWithQuery(<InvoicesPage />)

    expect(await screen.findByText('No se pudieron cargar las facturas.')).toBeInTheDocument()

    const callsBeforeRetry = fetchMock.mock.calls.length
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }))

    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThan(callsBeforeRetry)
    })
  })

  it('muestra el estado vacío cuando no hay facturas', async () => {
    mockFetchJson({ data: [], total: 0, pageSize: 10 })
    renderWithQuery(<InvoicesPage />)

    expect(await screen.findByText('No se encontraron facturas')).toBeInTheDocument()
  })
})
