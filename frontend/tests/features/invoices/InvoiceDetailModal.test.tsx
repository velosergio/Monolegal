import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { InvoiceDetailModal } from '@/features/invoices/components/InvoiceDetailModal'
import type { InvoiceDetail } from '@/features/invoices/types'
import { mockFetchJson, renderWithQuery } from '../../test-utils'

const detail: InvoiceDetail = {
  id: 'abcdef1234567890',
  clientId: 'Acme S.A.',
  amount: 1_500_000,
  status: 'primerrecordatorio',
  createdAt: '2026-01-01T08:00:00.000Z',
  updatedAt: '2026-06-01T10:30:00.000Z',
  remindersCount: 1,
  lastReminderSentAt: '2026-06-01T10:30:00.000Z',
  lastStatusTransitionAt: '2026-06-01T10:30:00.000Z',
  statusHistory: [],
  allowedTransitions: ['segundorecordatorio', 'pagado'],
}

describe('InvoiceDetailModal', () => {
  it('carga y muestra el detalle de la factura seleccionada', async () => {
    mockFetchJson(detail)
    renderWithQuery(<InvoiceDetailModal invoiceId={detail.id} onClose={() => {}} />)

    expect(await screen.findByText('Acme S.A.')).toBeInTheDocument()
    expect(screen.getByRole('dialog')).toBeInTheDocument()
  })

  it('no consulta ni se muestra cuando no hay factura seleccionada', () => {
    const fetchMock = mockFetchJson(detail)
    renderWithQuery(<InvoiceDetailModal invoiceId={null} onClose={() => {}} />)

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('invoca onClose al pulsar el botón de cierre', async () => {
    mockFetchJson(detail)
    const onClose = vi.fn()
    renderWithQuery(<InvoiceDetailModal invoiceId={detail.id} onClose={onClose} />)

    await screen.findByText('Acme S.A.')
    await userEvent.click(screen.getByRole('button', { name: 'Cerrar' }))

    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('muestra "Factura no encontrada" ante un 404', async () => {
    mockFetchJson({ error: 'not found' }, { ok: false, status: 404 })
    renderWithQuery(<InvoiceDetailModal invoiceId="no-existe" onClose={() => {}} />)

    expect(await screen.findByText('Factura no encontrada.')).toBeInTheDocument()
  })

  it('muestra error genérico con reintento ante un fallo', async () => {
    const fetchMock = mockFetchJson({ error: 'boom' }, { ok: false, status: 500 })
    renderWithQuery(<InvoiceDetailModal invoiceId={detail.id} onClose={() => {}} />)

    expect(await screen.findByText('No se pudo cargar el detalle.')).toBeInTheDocument()
    const before = fetchMock.mock.calls.length
    await userEvent.click(screen.getByRole('button', { name: 'Reintentar' }))
    await waitFor(() => {
      expect(fetchMock.mock.calls.length).toBeGreaterThan(before)
    })
  })
})
