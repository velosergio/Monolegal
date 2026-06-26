import { render, screen } from '@testing-library/react'
import { describe, expect, it, vi } from 'vitest'
import { ErrorBoundary } from '@/components/feedback/ErrorBoundary'

function Boom(): never {
  throw new Error('boom')
}

describe('ErrorBoundary', () => {
  it('renderiza los hijos cuando no hay error', () => {
    render(
      <ErrorBoundary>
        <p>Contenido</p>
      </ErrorBoundary>
    )
    expect(screen.getByText('Contenido')).toBeInTheDocument()
  })

  it('muestra la degradación por defecto cuando un hijo lanza', () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    render(
      <ErrorBoundary>
        <Boom />
      </ErrorBoundary>
    )
    expect(screen.getByRole('alert')).toHaveTextContent('Algo salió mal.')
    spy.mockRestore()
  })

  it('usa el fallback proporcionado', () => {
    const spy = vi.spyOn(console, 'error').mockImplementation(() => {})
    render(
      <ErrorBoundary fallback={<p>Respaldo personalizado</p>}>
        <Boom />
      </ErrorBoundary>
    )
    expect(screen.getByText('Respaldo personalizado')).toBeInTheDocument()
    spy.mockRestore()
  })
})
