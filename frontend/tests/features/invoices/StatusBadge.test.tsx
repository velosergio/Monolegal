import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { StatusBadge } from '@/features/invoices/components/StatusBadge'

describe('StatusBadge', () => {
  it('muestra la etiqueta legible de un estado conocido', () => {
    render(<StatusBadge status="pagado" />)
    expect(screen.getByText('Pagado')).toBeInTheDocument()
  })

  it('muestra el valor en bruto con estilo neutro para un estado desconocido', () => {
    render(<StatusBadge status="estado-futuro" />)
    const badge = screen.getByText('estado-futuro')
    expect(badge).toBeInTheDocument()
    expect(badge).toHaveClass('bg-muted', 'text-muted-foreground')
  })

  it('aplica una clase de color específica para cada estado conocido', () => {
    render(<StatusBadge status="pending" />)
    expect(screen.getByText('Pendiente')).toHaveClass('bg-amber-100')
  })
})
