import { render, screen } from '@testing-library/react'
import { Wallet } from 'lucide-react'
import { describe, expect, it } from 'vitest'
import { StatCard } from '@/features/dashboard/components/StatCard'

describe('StatCard', () => {
  it('muestra la etiqueta y el valor', () => {
    render(<StatCard label="Total facturas" value={128} />)
    expect(screen.getByText('Total facturas')).toBeInTheDocument()
    expect(screen.getByText('128')).toBeInTheDocument()
  })

  it('acepta un valor en formato de texto', () => {
    render(<StatCard label="Estados activos" value="3 / 5" />)
    expect(screen.getByText('3 / 5')).toBeInTheDocument()
  })

  it('marca el ícono opcional como decorativo (oculto a lectores de pantalla)', () => {
    const { container } = render(<StatCard label="Clientes" value={12} icon={Wallet} />)
    const icon = container.querySelector('svg')
    expect(icon).not.toBeNull()
    expect(icon).toHaveAttribute('aria-hidden', 'true')
  })

  it('no renderiza ícono cuando no se provee', () => {
    const { container } = render(<StatCard label="Clientes" value={12} />)
    expect(container.querySelector('svg')).toBeNull()
  })
})
