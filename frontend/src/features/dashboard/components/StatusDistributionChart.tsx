import type { ChartDatum } from '../types'
import { DonutChart } from './DonutChart'

interface StatusDistributionChartProps {
  data: ChartDatum[]
  /** Total de facturas (centro de la dona). Si se omite, se suma de `data`. */
  total?: number
}

/** Distribución de facturas por estado (gráfico de dona animado con colores por estado). */
export function StatusDistributionChart({ data, total }: StatusDistributionChartProps) {
  return (
    <DonutChart
      data={data}
      total={total}
      ariaLabel="Distribución de facturas por estado"
      centerLabel="Total"
    />
  )
}
