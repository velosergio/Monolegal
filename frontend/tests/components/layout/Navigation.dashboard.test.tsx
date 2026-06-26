import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { NAV_ITEMS } from '@/components/layout/navigation'
import { Sidebar } from '@/components/layout/Sidebar'

function renderSidebar(route = '/facturas') {
  return render(
    <MemoryRouter initialEntries={[route]}>
      <Sidebar />
    </MemoryRouter>
  )
}

describe('Navegación · Dashboard', () => {
  it('expone Dashboard como ítem habilitado (sin "Próximamente")', () => {
    renderSidebar()
    const link = screen.getByRole('link', { name: 'Dashboard' })
    expect(link).toBeInTheDocument()
    expect(screen.queryByText('Próximamente')).not.toBeInTheDocument()
  })

  it('marca Dashboard como sección activa en /dashboard', () => {
    renderSidebar('/dashboard')
    const link = screen.getByRole('link', { name: 'Dashboard' })
    expect(link).toHaveAttribute('aria-current', 'page')
  })

  it('la entrada de navegación apunta a /dashboard y está habilitada', () => {
    const dashboard = NAV_ITEMS.find((item) => item.label === 'Dashboard')
    expect(dashboard).toBeDefined()
    expect(dashboard?.to).toBe('/dashboard')
    expect(dashboard?.disabled).toBe(false)
  })
})
