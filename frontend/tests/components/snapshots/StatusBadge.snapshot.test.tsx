import { render } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { StatusBadge } from '@/features/invoices/components/StatusBadge'

describe('StatusBadge (snapshot)', () => {
  it('mantiene el marcado para un estado conocido (pagado)', () => {
    const { asFragment } = render(<StatusBadge status="pagado" />)
    expect(asFragment()).toMatchSnapshot()
  })

  it('mantiene el marcado neutro para un estado desconocido', () => {
    const { asFragment } = render(<StatusBadge status="estado-futuro" />)
    expect(asFragment()).toMatchSnapshot()
  })
})
