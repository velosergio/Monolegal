import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { renderWithProviders } from '../../../../tests/renderWithProviders'
import type { Shipment } from '../types'
import { CancelNotificationDialog } from './CancelNotificationDialog'

const pendingShipment: Shipment = {
  id: 'a1b2c3d4e5',
  clientId: 'cli-1',
  clientName: 'ACME S.A.',
  clientEmail: 'pagos@acme.com',
  status: 'primerrecordatorio',
  sendStatus: 'pending',
  lastAttemptAt: null,
  retryCount: 0,
  lastError: null,
}

describe('CancelNotificationDialog', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('pide confirmación y muestra el cliente afectado', () => {
    renderWithProviders(<CancelNotificationDialog shipment={pendingShipment} onClose={vi.fn()} />)
    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByRole('heading', { name: /cancelar envío/i })).toBeInTheDocument()
    expect(screen.getByText('ACME S.A.')).toBeInTheDocument()
  })

  it('confirma la cancelación, muestra toast y cierra', async () => {
    vi.stubGlobal(
      'fetch',
      vi.fn(() =>
        Promise.resolve({
          ok: true,
          json: () => Promise.resolve({ ...pendingShipment, sendStatus: 'skipped' }),
        } as Response)
      )
    )
    const onClose = vi.fn()
    renderWithProviders(<CancelNotificationDialog shipment={pendingShipment} onClose={onClose} />)

    await userEvent.click(screen.getByRole('button', { name: /^cancelar envío$/i }))

    await waitFor(() => expect(onClose).toHaveBeenCalled())
    expect(screen.getByText(/marcado como omitido/i)).toBeInTheDocument()
  })

  it('no renderiza nada cuando shipment es null', () => {
    renderWithProviders(<CancelNotificationDialog shipment={null} onClose={vi.fn()} />)
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  })
})
