import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'

// Smoke test de disponibilidad: React 19 + TypeScript strict (US1 / FR-001, FR-002).
// Verifica que un componente tipado trivial compila y renderiza en el DOM.

interface SaludoProps {
  nombre: string
}

function Saludo({ nombre }: SaludoProps) {
  return <span>Hola {nombre}</span>
}

describe('Disponibilidad de React + TypeScript', () => {
  it('renderiza un componente tipado en el DOM', () => {
    render(<Saludo nombre="Monolegal" />)
    expect(screen.getByText('Hola Monolegal')).toBeInTheDocument()
  })
})
