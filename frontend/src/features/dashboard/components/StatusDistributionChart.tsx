import type { ChartDatum } from '../types'
import { BarChart } from './BarChart'

interface StatusDistributionChartProps {
  data: ChartDatum[]
}

/** Distribución de facturas por estado (gráfico de barras animado). */
export function StatusDistributionChart({ data }: StatusDistributionChartProps) {
  return <BarChart data={data} ariaLabel="Distribución de facturas por estado" />
}
