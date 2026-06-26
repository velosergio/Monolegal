import type { ChartDatum } from '../types'
import { BarChart } from './BarChart'

interface ClientDistributionChartProps {
  /** Top-N clientes + "Otros" (derivado con `topClients` en `DashboardPage`). */
  data: ChartDatum[]
}

/** Distribución de facturas por cliente (top-N + "Otros"). */
export function ClientDistributionChart({ data }: ClientDistributionChartProps) {
  return <BarChart data={data} ariaLabel="Distribución de facturas por cliente" />
}
