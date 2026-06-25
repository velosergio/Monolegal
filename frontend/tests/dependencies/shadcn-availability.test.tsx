import { render, screen } from '@testing-library/react'
import { describe, expect, it } from 'vitest'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

// Smoke test de disponibilidad: shadcn/ui + Tailwind v4 + cn() (US3 / FR-004).
// Verifica que un componente shadcn renderiza y que cn() resuelve clases/conflictos.

describe('Disponibilidad de shadcn/ui', () => {
  it('renderiza un componente Button de shadcn/ui en el DOM', () => {
    render(<Button>Guardar</Button>)
    expect(screen.getByRole('button', { name: 'Guardar' })).toBeInTheDocument()
  })

  it('cn() combina clases y resuelve conflictos de Tailwind', () => {
    expect(cn('px-2', 'px-4')).toBe('px-4')
    expect(cn('text-sm', false && 'hidden', 'font-medium')).toBe('text-sm font-medium')
  })
})
