import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import { AppShell } from '@/components/layout/AppShell'

function renderShell(children: React.ReactNode) {
  return render(
    <MemoryRouter>
      <AppShell>{children}</AppShell>
    </MemoryRouter>
  )
}

describe('AppShell', () => {
  it('renderiza navegación, main y pie', () => {
    renderShell(<p>Contenido de prueba</p>)

    expect(screen.getByRole('navigation', { name: 'Navegación principal' })).toBeInTheDocument()
    expect(screen.getByRole('main')).toBeInTheDocument()
    expect(screen.getByRole('contentinfo')).toBeInTheDocument()
  })

  it('renderiza el contenido hijo dentro del main', () => {
    renderShell(<p>Contenido de prueba</p>)

    const main = screen.getByRole('main')
    expect(main).toHaveTextContent('Contenido de prueba')
  })

  it('expone un main enfocable como destino del salto de contenido', () => {
    renderShell(<p>Contenido</p>)

    expect(screen.getByRole('main')).toHaveAttribute('id', 'main')
  })
})
