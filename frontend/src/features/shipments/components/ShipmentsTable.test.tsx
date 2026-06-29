import { screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
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

describe('ShipmentsTable', () => {
  it('renderiza las columnas y los datos de la fila', () => {
    renderWithProviders(<ShipmentsTable shipments={[makeShipment()]} onCancelShipment={vi.fn()} />)

    expect(screen.getByText('Cliente')).toBeInTheDocument()
    expect(screen.getByText('Email')).toBeInTheDocument()
    expect(screen.getByText('Estado de envío')).toBeInTheDocument()
    expect(screen.getByText('Reintentos')).toBeInTheDocument()

    expect(screen.getByText('ACME S.A.')).toBeInTheDocument()
    expect(screen.getByText('pagos@acme.com')).toBeInTheDocument()
    expect(screen.getByText('Fallido')).toBeInTheDocument()
    expect(screen.getByText('2')).toBeInTheDocument()
  })

  it('deshabilita "Cancelar" cuando el envío no está pendiente', () => {
    renderWithProviders(
      <ShipmentsTable
        shipments={[makeShipment({ sendStatus: 'sent' })]}
        onCancelShipment={vi.fn()}
      />
    )
    const cancelBtn = screen.getByRole('button', { name: /cancelar el envío/i })
    expect(cancelBtn).toBeDisabled()
  })

  it('habilita "Cancelar" para envíos pendientes', () => {
    renderWithProviders(
      <ShipmentsTable
        shipments={[makeShipment({ sendStatus: 'pending', lastAttemptAt: null, retryCount: 0 })]}
        onCancelShipment={vi.fn()}
      />
    )
    const cancelBtn = screen.getByRole('button', { name: /cancelar el envío/i })
    expect(cancelBtn).toBeEnabled()
  })

  it('marca "Sin correo" y deshabilita reenviar cuando no hay correo', () => {
    renderWithProviders(
      <ShipmentsTable
        shipments={[makeShipment({ clientEmail: null })]}
        onCancelShipment={vi.fn()}
      />
    )
    expect(screen.getByText('Sin correo')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /reenviar la notificación/i })).toBeDisabled()
  })
})
