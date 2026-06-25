import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import { describe, expect, it } from 'vitest'

// Smoke test del framework: Vitest + Testing Library + user-event + matchers jest-dom (US6 / FR-007).

function Contador() {
  const [n, setN] = useState(0)
  return (
    <button type="button" onClick={() => setN((v) => v + 1)}>
      contador: {n}
    </button>
  )
}

describe('Disponibilidad de Vitest + Testing Library', () => {
  it('renderiza, simula interacción y asevera con matcher jest-dom', async () => {
    const user = userEvent.setup()
    render(<Contador />)
    const boton = screen.getByRole('button')
    expect(boton).toBeInTheDocument()
    await user.click(boton)
    expect(boton).toHaveTextContent('contador: 1')
  })
})
