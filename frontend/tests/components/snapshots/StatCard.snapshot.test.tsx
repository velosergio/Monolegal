import { render } from '@testing-library/react'
import { Wallet } from 'lucide-react'
import { describe, expect, it } from 'vitest'
import { StatCard } from '@/features/dashboard/components/StatCard'

describe('StatCard (snapshot)', () => {
  it('mantiene el marcado sin ícono', () => {
    const { asFragment } = render(<StatCard label="Total facturas" value={128} />)
    expect(asFragment()).toMatchSnapshot()
  })

  it('mantiene el marcado con ícono decorativo', () => {
    const { asFragment } = render(<StatCard label="Total facturas" value={128} icon={Wallet} />)
    expect(asFragment()).toMatchSnapshot()
  })
})
