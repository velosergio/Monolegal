import { render, screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { Sidebar } from './Sidebar'

function renderSidebar() {
  return render(
    <MemoryRouter>
      <Sidebar />
    </MemoryRouter>
  )
}

describe('Sidebar — acceso a Swagger UI', () => {
  afterEach(() => {
    vi.unstubAllEnvs()
  })

  it('renderiza los ítems de ruta existentes', () => {
    renderSidebar()
    expect(screen.getByRole('link', { name: /Dashboard/i })).toBeInTheDocument()
    expect(screen.getByRole('link', { name: /Facturas/i })).toBeInTheDocument()
  })

  it('muestra el acceso "API (Swagger)" como enlace externo seguro (default /swagger)', () => {
    renderSidebar()
    const link = screen.getByRole('link', { name: /API \(Swagger\)/i })
    expect(link).toHaveAttribute('href', '/swagger')
    expect(link).toHaveAttribute('target', '_blank')
    expect(link).toHaveAttribute('rel', 'noopener noreferrer')
  })

  it('usa la URL configurada en VITE_SWAGGER_URL', () => {
    vi.stubEnv('VITE_SWAGGER_URL', 'https://api.example.com/swagger')
    renderSidebar()
    expect(screen.getByRole('link', { name: /API \(Swagger\)/i })).toHaveAttribute(
      'href',
      'https://api.example.com/swagger'
    )
  })

  it('oculta el acceso a Swagger cuando VITE_SWAGGER_URL está vacío', () => {
    vi.stubEnv('VITE_SWAGGER_URL', '')
    renderSidebar()
    expect(screen.queryByRole('link', { name: /API \(Swagger\)/i })).not.toBeInTheDocument()
  })
})
