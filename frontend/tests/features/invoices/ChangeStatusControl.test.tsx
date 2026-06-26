import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it } from 'vitest'
import { ChangeStatusControl } from '@/features/invoices/components/ChangeStatusControl'
import type { InvoiceDetail } from '@/features/invoices/types'
import { mockFetchJson, renderWithQuery } from '../../test-utils'

const updated: InvoiceDetail = {
  id: 'abcdef1234567890',
  clientId: 'Acme S.A.',
  amount: 1_500_000,
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

  it('en estado terminal (sin transiciones) no muestra el botón de cambio', () => {
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

  it('el botón de cambio está deshabilitado hasta elegir un destino', () => {
    renderWithQuery(
      <ChangeStatusControl
        invoiceId="abcdef1234567890"
        currentStatus="primerrecordatorio"
        allowedTransitions={['segundorecordatorio', 'pagado']}
      />
    )

    expect(screen.getByRole('button', { name: /cambiar estado/i })).toBeDisabled()
  })

  it('aplica el cambio de estado seleccionado', async () => {
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
  })

  it('muestra un mensaje de error legible ante un 400', async () => {
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

    expect(await screen.findByRole('alert')).toHaveTextContent(/no se pudo cambiar el estado/i)
  })
})
