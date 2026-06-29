import { render, screen } from '@testing-library/react'
import { afterAll, beforeAll, describe, expect, it, vi } from 'vitest'
import { Footer } from '@/components/layout/Footer'

describe('Footer', () => {
  beforeAll(() => {
    vi.useFakeTimers()
    // Fecha local (no UTC) para que getFullYear() sea 2026 con independencia de la zona horaria.
    vi.setSystemTime(new Date(2026, 5, 15, 12, 0, 0))
  })

  afterAll(() => {
    vi.useRealTimers()
  })

  it('en estado expandido muestra el producto, la versión y el año actual', () => {
    render(<Footer />)
    expect(screen.getByText(/Monolegal · v0\.1\.0 · © 2026/)).toBeInTheDocument()
  })

  it('en estado colapsado muestra solo la versión', () => {
    render(<Footer collapsed={true} />)
    expect(screen.getByText('v0.1.0')).toBeInTheDocument()
    expect(screen.queryByText(/Monolegal/)).not.toBeInTheDocument()
  })
})
