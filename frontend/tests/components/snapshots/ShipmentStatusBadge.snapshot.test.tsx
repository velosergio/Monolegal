import { render } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { ShipmentStatusBadge } from '@/features/shipments/components/ShipmentStatusBadge'

describe('ShipmentStatusBadge (snapshot)', () => {
  it('mantiene el marcado para un estado conocido (sent)', () => {
    const { asFragment } = render(<ShipmentStatusBadge status="sent" />)
    expect(asFragment()).toMatchSnapshot()
  })
})
