import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { ShipmentsEmptyState } from '@/features/shipments/components/ShipmentsEmptyState'

describe('ShipmentsEmptyState', () => {
  it('con filtros activos comunica que no hubo coincidencias', () => {
    render(<ShipmentsEmptyState filtered={true} />)
    expect(screen.getByText('No se encontraron envíos')).toBeInTheDocument()
    expect(screen.getByText(/Ajusta los filtros o el término de búsqueda/i)).toBeInTheDocument()
  })

  it('sin filtros comunica que aún no hay envíos', () => {
    render(<ShipmentsEmptyState filtered={false} />)
    expect(screen.getByText('Aún no hay envíos')).toBeInTheDocument()
    expect(screen.getByText(/generen avisos, aparecerán aquí/i)).toBeInTheDocument()
  })
})
