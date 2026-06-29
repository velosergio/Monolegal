import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { DashboardSkeleton } from '@/features/dashboard/components/DashboardSkeleton'

describe('DashboardSkeleton', () => {
  it('expone un estado de carga accesible con su etiqueta', () => {
    render(<DashboardSkeleton />)
    const status = screen.getByRole('status', { name: 'Cargando estadísticas' })
    expect(status).toBeInTheDocument()
    expect(status).toHaveTextContent('Cargando estadísticas…')
  })
})
