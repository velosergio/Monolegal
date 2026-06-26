import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { Sidebar } from '@/components/layout/Sidebar'

function renderSidebar(ui: React.ReactElement, route = '/facturas') {
  return render(<MemoryRouter initialEntries={[route]}>{ui}</MemoryRouter>)
}

describe('Sidebar', () => {
  it('marca la ruta activa con aria-current', () => {
    renderSidebar(<Sidebar />, '/facturas')
    const facturas = screen.getByRole('link', { name: 'Facturas' })
    expect(facturas).toHaveAttribute('aria-current', 'page')
  })

  it('expone Dashboard como ruta navegable (habilitada)', () => {
    renderSidebar(<Sidebar />)
    expect(screen.getByRole('link', { name: 'Dashboard' })).toBeInTheDocument()
    expect(screen.queryByText('Próximamente')).not.toBeInTheDocument()
  })

  it('expone Configuración como ruta navegable', () => {
    renderSidebar(<Sidebar />)
    expect(screen.getByRole('link', { name: 'Configuración' })).toBeInTheDocument()
  })

  it('invoca onNavigate al activar un ítem navegable', async () => {
    const onNavigate = vi.fn()
    const user = userEvent.setup()
    renderSidebar(<Sidebar onNavigate={onNavigate} />)

    await user.click(screen.getByRole('link', { name: 'Facturas' }))
    expect(onNavigate).toHaveBeenCalledTimes(1)
  })

  it('muestra el botón de colapsar cuando se provee onToggleCollapse', async () => {
    const onToggleCollapse = vi.fn()
    const user = userEvent.setup()
    renderSidebar(<Sidebar onToggleCollapse={onToggleCollapse} />)

    await user.click(screen.getByRole('button', { name: 'Colapsar menú lateral' }))
    expect(onToggleCollapse).toHaveBeenCalledTimes(1)
  })
})
