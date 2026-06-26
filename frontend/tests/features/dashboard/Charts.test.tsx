import { render, screen } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ClientDistributionChart } from '@/features/dashboard/components/ClientDistributionChart'
import { StatusDistributionChart } from '@/features/dashboard/components/StatusDistributionChart'

afterEach(() => {
  vi.unstubAllGlobals()
})

function stubReducedMotion(matches: boolean) {
  vi.stubGlobal(
    'matchMedia',
    vi.fn((query: string) => ({
      matches,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }))
  )
}

describe('StatusDistributionChart', () => {
  it('renderiza una entrada por estado con su etiqueta y valor', () => {
    render(
      <StatusDistributionChart
        data={[
          { label: 'Pendiente', value: 40 },
          { label: 'Pagado', value: 60 },
        ]}
      />
    )

    expect(screen.getByText('Pendiente')).toBeInTheDocument()
    expect(screen.getByText('Pagado')).toBeInTheDocument()
    expect(screen.getByText('40')).toBeInTheDocument()
    expect(screen.getByText('60')).toBeInTheDocument()
  })

  it('renderiza los valores también con movimiento reducido', () => {
    stubReducedMotion(true)
    render(<StatusDistributionChart data={[{ label: 'Pendiente', value: 40 }]} />)
    expect(screen.getByText('40')).toBeInTheDocument()
  })
})

describe('ClientDistributionChart', () => {
  it('renderiza una entrada por cliente recibido (incluida "Otros")', () => {
    render(
      <ClientDistributionChart
        data={[
          { label: 'cliente-001', value: 30 },
          { label: 'cliente-002', value: 25 },
          { label: 'Otros', value: 15 },
        ]}
      />
    )

    expect(screen.getByText('cliente-001')).toBeInTheDocument()
    expect(screen.getByText('cliente-002')).toBeInTheDocument()
    expect(screen.getByText('Otros')).toBeInTheDocument()
  })
})
