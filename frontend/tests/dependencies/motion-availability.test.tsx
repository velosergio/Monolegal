import { render, screen } from '@testing-library/react'
import { motion } from 'motion/react'
import { describe, expect, it } from 'vitest'

// Smoke test de disponibilidad: Motion (US5 / FR-006).
// Verifica que un componente motion.* compila y renderiza en el DOM.

describe('Disponibilidad de Motion', () => {
  it('renderiza un motion.div con una animación trivial', () => {
    render(
      <motion.div initial={{ opacity: 0 }} animate={{ opacity: 1 }}>
        animado
      </motion.div>
    )
    expect(screen.getByText('animado')).toBeInTheDocument()
  })
})
