import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import type { SendStatus } from '../types'
import { ShipmentStatusBadge } from './ShipmentStatusBadge'

describe('ShipmentStatusBadge', () => {
  it.each<[SendStatus, string]>([
    ['pending', 'Pendiente'],
    ['sent', 'Enviado'],
    ['failed', 'Fallido'],
    ['skipped', 'Omitido'],
    ['retrying', 'Reintentando'],
  ])('muestra la etiqueta textual para %s', (status, label) => {
    render(<ShipmentStatusBadge status={status} />)
    expect(screen.getByText(label)).toBeInTheDocument()
  })

  it('aplica una clase de color distinta a "fallido" (no sólo color: también etiqueta)', () => {
    const { container } = render(<ShipmentStatusBadge status="failed" />)
    // La insignia incluye una clase de color de fallo (rojo) además del texto.
    expect(container.firstChild).toHaveClass('text-red-800')
    expect(screen.getByText('Fallido')).toBeInTheDocument()
  })
})
