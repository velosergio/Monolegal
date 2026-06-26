import { render, screen, within } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { ClientDistributionChart } from '@/features/dashboard/components/ClientDistributionChart'
import { StatusDistributionChart } from '@/features/dashboard/components/StatusDistributionChart'
import { statusChartClass } from '@/features/dashboard/statusChartColors'

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

describe('StatusDistributionChart (donut)', () => {
  it('renderiza un gráfico (role img) con leyenda por estado y sus valores', () => {
    render(
      <StatusDistributionChart
        data={[
          { label: 'Pendiente', value: 40, color: statusChartClass('pending') },
          { label: 'Pagado', value: 60, color: statusChartClass('pagado') },
        ]}
        total={100}
      />
    )

    expect(
      screen.getByRole('img', { name: /distribución de facturas por estado/i })
    ).toBeInTheDocument()

    const legend = screen.getByRole('list')
    expect(within(legend).getByText('Pendiente')).toBeInTheDocument()
    expect(within(legend).getByText('Pagado')).toBeInTheDocument()
    expect(within(legend).getByText('40')).toBeInTheDocument()
    expect(within(legend).getByText('60')).toBeInTheDocument()
  })

  it('muestra el total en el centro con la etiqueta "Total"', () => {
    render(
      <StatusDistributionChart
        data={[
          { label: 'Pendiente', value: 40 },
          { label: 'Pagado', value: 60 },
        ]}
        total={100}
      />
    )

    expect(screen.getByText('100')).toBeInTheDocument()
    expect(screen.getByText('Total')).toBeInTheDocument()
  })

  it('usa un color coherente por estado (clase de la insignia) en los segmentos', () => {
    const { container } = render(
      <StatusDistributionChart
        data={[{ label: 'Pendiente', value: 10, color: statusChartClass('pending') }]}
        total={10}
      />
    )
    // statusChartClass('pending') => 'stroke-amber-400 ...'
    expect(container.querySelector('.stroke-amber-400')).not.toBeNull()
  })

  it('estado desconocido usa color neutro', () => {
    const { container } = render(
      <StatusDistributionChart
        data={[{ label: 'Raro', value: 5, color: statusChartClass('loquesea') }]}
        total={5}
      />
    )
    expect(container.querySelector('.stroke-muted-foreground')).not.toBeNull()
  })

  it('con total 0 (sin facturas) muestra 0 en el centro y no rompe', () => {
    render(
      <StatusDistributionChart
        data={[
          { label: 'Pendiente', value: 0 },
          { label: 'Pagado', value: 0 },
        ]}
        total={0}
      />
    )
    expect(screen.getByTestId('donut-total')).toHaveTextContent('0')
    expect(screen.getByText('Total')).toBeInTheDocument()
  })

  it('con un único estado dibuja un solo segmento (anillo completo)', () => {
    const { container } = render(
      <StatusDistributionChart
        data={[{ label: 'Pendiente', value: 7, color: statusChartClass('pending') }]}
        total={7}
      />
    )
    // 1 círculo de pista (stroke-muted) + 1 segmento.
    const segments = container.querySelectorAll('svg[role="img"] circle')
    expect(segments.length).toBe(2)
  })

  it('renderiza los valores también con movimiento reducido', () => {
    stubReducedMotion(true)
    render(<StatusDistributionChart data={[{ label: 'Pendiente', value: 40 }]} total={40} />)
    expect(screen.getByRole('list')).toHaveTextContent('Pendiente')
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
