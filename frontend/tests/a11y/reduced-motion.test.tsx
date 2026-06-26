import { screen } from '@testing-library/react'
import { MemoryRouter } from 'react-router-dom'
import { describe, expect, it, vi } from 'vitest'
import { InvoicesPage } from '@/features/invoices/components/InvoicesPage'
import { DURATION, motionTransition, REDUCED_TRANSITION } from '@/lib/motion'
import { mockFetchJson, renderWithQuery } from '../test-utils'

describe('Movimiento reducido', () => {
  it('motionTransition devuelve una transición instantánea cuando se reduce el movimiento', () => {
    expect(motionTransition(true)).toEqual(REDUCED_TRANSITION)
    expect(motionTransition(true).duration).toBe(0)
  })

  it('motionTransition usa la duración base cuando no hay reducción', () => {
    expect(motionTransition(false).duration).toBe(DURATION.base)
  })

  it('la página se monta correctamente con prefers-reduced-motion activo', async () => {
    vi.stubGlobal(
      'matchMedia',
      vi.fn((query: string) => ({
        matches: query.includes('reduce'),
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
      }))
    )
    mockFetchJson({ data: [], total: 0, pageSize: 10 })

    renderWithQuery(
      <MemoryRouter>
        <InvoicesPage />
      </MemoryRouter>
    )
    expect(await screen.findByRole('heading', { name: 'Facturas' })).toBeInTheDocument()
  })
})
