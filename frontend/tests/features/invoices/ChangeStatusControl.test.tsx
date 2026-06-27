import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { ChangeStatusControl } from '@/features/invoices/components/ChangeStatusControl'
import type { InvoiceDetail } from '@/features/invoices/types'
import { mockFetchJson, renderWithQuery } from '../../test-utils'

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
  statusHistory: [],
  allowedTransitions: ['pagado', 'desactivado'],
}

describe('ChangeStatusControl', () => {
  it('ofrece únicamente los destinos permitidos', async () => {
    mockFetchJson(updated)
    const user = userEvent.setup()
    renderWithQuery(
      <ChangeStatusControl
        invoiceId="abcdef1234567890"
        currentStatus="primerrecordatorio"
        allowedTransitions={['segundorecordatorio', 'pagado']}
      />
    )

    await user.click(screen.getByRole('combobox', { name: /nuevo estado/i }))

    expect(await screen.findByRole('option', { name: '2do Recordatorio' })).toBeInTheDocument()
    expect(screen.getByRole('option', { name: 'Pagado' })).toBeInTheDocument()
    expect(screen.queryByRole('option', { name: 'Pendiente' })).not.toBeInTheDocument()
  })

  it('en estado terminal (sin transiciones) no muestra el formulario de cambio', () => {
    renderWithQuery(
      <ChangeStatusControl
        invoiceId="abcdef1234567890"
        currentStatus="pagado"
        allowedTransitions={[]}
      />
    )

    expect(screen.queryByRole('button', { name: /cambiar estado/i })).not.toBeInTheDocument()
    expect(screen.getByText(/no admite cambios de estado/i)).toBeInTheDocument()
  })

  it('al confirmar sin selección muestra validación y no realiza ninguna petición', async () => {
    const fetchMock = mockFetchJson(updated)
    const user = userEvent.setup()
    renderWithQuery(
      <ChangeStatusControl
        invoiceId="abcdef1234567890"
        currentStatus="primerrecordatorio"
        allowedTransitions={['segundorecordatorio', 'pagado']}
      />
    )

    await user.click(screen.getByRole('button', { name: /cambiar estado/i }))

    expect(await screen.findByText(/selecciona un estado destino/i)).toBeInTheDocument()
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('aplica el cambio seleccionado y notifica el éxito con un toast', async () => {
    const fetchMock = mockFetchJson(updated)
    const user = userEvent.setup()
    renderWithQuery(
      <ChangeStatusControl
        invoiceId="abcdef1234567890"
        currentStatus="primerrecordatorio"
        allowedTransitions={['segundorecordatorio', 'pagado']}
      />
    )

    await user.click(screen.getByRole('combobox', { name: /nuevo estado/i }))
    await user.click(await screen.findByRole('option', { name: '2do Recordatorio' }))
    await user.click(screen.getByRole('button', { name: /cambiar estado/i }))

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalled()
    })
    const [url, init] = fetchMock.mock.calls[0] as unknown as [string, RequestInit]
    expect(url).toContain('/api/invoices/transition/abcdef1234567890')
    expect(JSON.parse(init.body as string)).toEqual({ newStatus: 'segundorecordatorio' })

    expect(await screen.findByText(/estado actualizado a «2do Recordatorio»/i)).toBeInTheDocument()
  })

  it('ante un 400 muestra un toast de error y conserva el mensaje inline', async () => {
    mockFetchJson({ error: 'Transición no permitida' }, { ok: false, status: 400 })
    const user = userEvent.setup()
    renderWithQuery(
      <ChangeStatusControl
        invoiceId="abcdef1234567890"
        currentStatus="primerrecordatorio"
        allowedTransitions={['segundorecordatorio', 'pagado']}
      />
    )

    await user.click(screen.getByRole('combobox', { name: /nuevo estado/i }))
    await user.click(await screen.findByRole('option', { name: '2do Recordatorio' }))
    await user.click(screen.getByRole('button', { name: /cambiar estado/i }))

    // El toast (role alert) y el mensaje inline persistente comparten el texto.
    const matches = await screen.findAllByText(/transición no permitida/i)
    expect(matches.length).toBeGreaterThanOrEqual(2)
  })
})
