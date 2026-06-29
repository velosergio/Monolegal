import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { renderWithProviders } from '../../../../tests/renderWithProviders'
import type { Shipment } from '../types'
import { ShipmentsTable } from './ShipmentsTable'

function makeShipment(overrides: Partial<Shipment> = {}): Shipment {
  return {
    id: 'a1b2c3d4e5',
    clientId: 'cli-1',
    clientName: 'ACME S.A.',
    clientEmail: 'pagos@acme.com',
    status: 'primerrecordatorio',
    sendStatus: 'failed',
    lastAttemptAt: '2026-06-28T14:03:00Z',
    retryCount: 2,
    lastError: 'SMTP timeout',
    ...overrides,
  }
}

function mockResendResponse(body: Partial<Shipment>) {
  vi.stubGlobal(
    'fetch',
    vi.fn(() =>
      Promise.resolve({
        ok: true,
        json: () => Promise.resolve(makeShipment(body)),
      } as Response)
    )
  )
}

describe('ShipmentsTable — reenvío', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('muestra un toast de éxito cuando el reenvío resulta enviado', async () => {
    mockResendResponse({ sendStatus: 'sent', retryCount: 3, lastError: null })
    renderWithProviders(<ShipmentsTable shipments={[makeShipment()]} onCancelShipment={vi.fn()} />)

    await userEvent.click(screen.getByRole('button', { name: /reenviar la notificación/i }))

    await waitFor(() =>
      expect(screen.getByText(/notificación reenviada a ACME S\.A\./i)).toBeInTheDocument()
    )
  })

  it('muestra un toast de error cuando el reenvío vuelve a fallar', async () => {
    mockResendResponse({ sendStatus: 'failed', retryCount: 3, lastError: 'SMTP caído' })
    renderWithProviders(<ShipmentsTable shipments={[makeShipment()]} onCancelShipment={vi.fn()} />)

    await userEvent.click(screen.getByRole('button', { name: /reenviar la notificación/i }))

    await waitFor(() => expect(screen.getByText('SMTP caído')).toBeInTheDocument())
  })
})
