import { motion, useReducedMotion } from 'motion/react'
import { chartTransition } from '@/lib/motion'
import type { ChartDatum } from '../types'

interface BarChartProps {
  data: ChartDatum[]
  /** Etiqueta accesible del gráfico en su conjunto. */
  ariaLabel: string
}

const DEFAULT_BAR_COLOR = 'hsl(var(--primary))'

/**
 * Gráfico de barras horizontales (SVG + Motion). Cada barra entra animándose desde
 * cero respetando `prefers-reduced-motion`. La etiqueta y el valor de cada serie se
 * muestran como texto para legibilidad y accesibilidad.
 */
export function BarChart({ data, ariaLabel }: BarChartProps) {
  const reduced = useReducedMotion()
  const max = data.reduce((acc, datum) => Math.max(acc, datum.value), 0)

  if (data.length === 0) {
    return <p className="text-sm text-muted-foreground">Sin datos disponibles.</p>
  }

  return (
    <ul aria-label={ariaLabel} className="flex flex-col gap-3">
      {data.map((datum, index) => {
        const ratio = max > 0 ? datum.value / max : 0
        return (
          <li key={datum.label} className="grid grid-cols-[8rem_1fr_auto] items-center gap-3">
            <span className="truncate text-sm text-foreground" title={datum.label}>
              {datum.label}
            </span>
            <svg
              role="presentation"
              className="h-4 w-full overflow-visible"
              preserveAspectRatio="none"
              viewBox="0 0 100 10"
            >
              <rect x={0} y={0} width={100} height={10} rx={2} className="fill-muted" />
              <motion.rect
                x={0}
                y={0}
                width={Math.max(ratio * 100, datum.value > 0 ? 1 : 0)}
                height={10}
                rx={2}
                fill={datum.color ?? DEFAULT_BAR_COLOR}
                initial={{ scaleX: 0 }}
                animate={{ scaleX: 1 }}
                transition={chartTransition(reduced, index)}
                style={{ transformOrigin: 'left center' }}
              />
            </svg>
            <span className="text-sm font-medium tabular-nums text-foreground">{datum.value}</span>
          </li>
        )
      })}
    </ul>
  )
}
