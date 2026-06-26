import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactElement } from 'react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { InvoicesPage } from '@/features/invoices/components/InvoicesPage'
import { mockFetchJson, renderWithQuery } from '../../test-utils'

/** Renderiza dentro de un router en memoria (InvoicesPage usa el search param de selección). */
function renderPage(ui: ReactElement) {
  return renderWithQuery(<MemoryRouter>{ui}</MemoryRouter>)
}

describe('InvoicesPage — estado de error', () => {
  it('muestra mensaje de error y permite reintentar cuando la API falla', async () => {
    const fetchMock = mockFetchJson({ error: 'boom' }, { ok: false, status: 500 })
    renderPage(<InvoicesPage />)

    expect(await screen.findByText('No se pudieron cargar las facturas.')).toBeInTheDocument()

    const callsBeforeRetry = fetchMock.mock.calls.length
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }))

    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThan(callsBeforeRetry)
    })
  })

  it('muestra el estado vacío cuando no hay facturas', async () => {
    mockFetchJson({ data: [], total: 0, pageSize: 10 })
    renderPage(<InvoicesPage />)

    expect(await screen.findByText('No se encontraron facturas')).toBeInTheDocument()
  })
})
