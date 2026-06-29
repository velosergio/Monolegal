import { render } from '@testing-library/react'
import { afterAll, beforeAll, describe, expect, it, vi } from 'vitest'
import { Footer } from '@/components/layout/Footer'

// El pie usa `new Date().getFullYear()`. Fijamos la fecha del sistema para que el
// snapshot sea determinista y no cambie al pasar de año (FR-002, SC-003).
describe('Footer (snapshot)', () => {
  beforeAll(() => {
    vi.useFakeTimers()
    // Fecha local (no UTC) para que getFullYear() sea 2026 con independencia de la zona horaria.
    vi.setSystemTime(new Date(2026, 5, 15, 12, 0, 0))
  })

  afterAll(() => {
    vi.useRealTimers()
  })

  it('mantiene el marcado en estado expandido', () => {
    const { asFragment } = render(<Footer />)
    expect(asFragment()).toMatchSnapshot()
  })

  it('mantiene el marcado en estado colapsado', () => {
    const { asFragment } = render(<Footer collapsed={true} />)
    expect(asFragment()).toMatchSnapshot()
  })
})
