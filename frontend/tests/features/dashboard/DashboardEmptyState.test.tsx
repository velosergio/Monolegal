import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { DashboardEmptyState } from '@/features/dashboard/components/DashboardEmptyState'

describe('DashboardEmptyState', () => {
  it('comunica que aún no hay facturas con su texto de ayuda', () => {
    render(<DashboardEmptyState />)
    expect(screen.getByText('No hay facturas todavía')).toBeInTheDocument()
    expect(screen.getByText(/aquí verás las estadísticas de la cartera/i)).toBeInTheDocument()
  })
})
