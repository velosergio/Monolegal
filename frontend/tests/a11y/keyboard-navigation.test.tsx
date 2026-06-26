import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { AppShell } from '@/components/layout/AppShell'

function renderShell(children: React.ReactNode, route = '/facturas') {
  return render(
    <MemoryRouter initialEntries={[route]}>
      <AppShell>{children}</AppShell>
    </MemoryRouter>
  )
}

describe('Navegación por teclado', () => {
  it('el botón de menú es enfocable y tiene nombre accesible', () => {
    renderShell(<button type="button">Acción</button>)

    const menu = screen.getByRole('button', { name: 'Abrir menú' })
    menu.focus()
    expect(menu).toHaveFocus()
  })

  it('abre el menú lateral activando el botón con teclado', async () => {
    const user = userEvent.setup()
    renderShell(<button type="button">Acción</button>)

    screen.getByRole('button', { name: 'Abrir menú' }).focus()
    await user.keyboard('{Enter}')

    expect(await screen.findByRole('dialog')).toBeInTheDocument()
  })

  it('los ítems de navegación son operables y exponen su estado', () => {
    renderShell(<button type="button">Acción</button>, '/facturas')

    const facturas = screen.getByRole('link', { name: 'Facturas' })
    expect(facturas).toHaveAttribute('aria-current', 'page')
  })
})
