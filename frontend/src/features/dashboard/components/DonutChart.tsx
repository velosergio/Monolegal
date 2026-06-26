import { domAnimation, LazyMotion, m, useReducedMotion } from 'motion/react'
import { donutSweepTransition } from '@/lib/motion'
import { cn } from '@/lib/utils'
import type { ChartDatum } from '../types'

interface DonutChartProps {
  data: ChartDatum[]
  /** Total a mostrar en el centro. Si se omite, se calcula como la suma de valores. */
  total?: number
  /** Etiqueta accesible del gráfico en su conjunto. */
  ariaLabel: string
  /** Texto bajo el número central. */
  centerLabel?: string
}

const SIZE = 160
const STROKE = 22
const RADIUS = (SIZE - STROKE) / 2
const CENTER = SIZE / 2
const CIRCUMFERENCE = 2 * Math.PI * RADIUS
const DEFAULT_SEGMENT_CLASS = 'stroke-primary'

/**
 * Gráfico de dona (donut) in-house con SVG + Motion. Dibuja un segmento por
 * `datum` con valor positivo, animando el barrido (`stroke-dashoffset`) y
 * respetando `prefers-reduced-motion`. Muestra el total en el centro y una leyenda
 * textual accesible (la información no depende solo del color).
 */
export function DonutChart({ data, total, ariaLabel, centerLabel = 'Total' }: DonutChartProps) {
  const reduced = useReducedMotion()
  const sum = total ?? data.reduce((acc, datum) => acc + datum.value, 0)

  // Una sola pasada: filtramos valores positivos y calculamos el segmento a la vez.
  let cumulative = 0
  const segments: { datum: ChartDatum; index: number; arc: number; rotation: number }[] = []
  for (const datum of data) {
    if (datum.value <= 0) continue
    const fraction = sum > 0 ? datum.value / sum : 0
    const arc = fraction * CIRCUMFERENCE
    const rotation = (cumulative / (sum || 1)) * 360 - 90
    cumulative += datum.value
    segments.push({ datum, index: segments.length, arc, rotation })
  }

  return (
    <div className="flex flex-col items-center gap-4 sm:flex-row sm:items-center sm:gap-6">
      <div className="relative shrink-0">
        <LazyMotion features={domAnimation}>
          <svg
            width={SIZE}
            height={SIZE}
            viewBox={`0 0 ${SIZE} ${SIZE}`}
            role="img"
            aria-label={ariaLabel}
          >
            <circle
              cx={CENTER}
              cy={CENTER}
              r={RADIUS}
              fill="none"
              strokeWidth={STROKE}
              className="stroke-muted"
            />
            {segments.map(({ datum, index, arc, rotation }) => (
              <m.circle
                key={datum.label}
                cx={CENTER}
                cy={CENTER}
                r={RADIUS}
                fill="none"
                strokeWidth={STROKE}
                strokeLinecap="butt"
                className={cn(datum.color ?? DEFAULT_SEGMENT_CLASS)}
                strokeDasharray={`${arc} ${CIRCUMFERENCE}`}
                transform={`rotate(${rotation} ${CENTER} ${CENTER})`}
                initial={{ strokeDashoffset: arc }}
                animate={{ strokeDashoffset: 0 }}
                transition={donutSweepTransition(reduced, index)}
              />
            ))}
          </svg>
        </LazyMotion>
        <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
          <span
            data-testid="donut-total"
            className="text-2xl font-bold tabular-nums text-foreground"
          >
            {sum}
          </span>
          <span className="text-xs text-muted-foreground">{centerLabel}</span>
        </div>
      </div>

      <ul className="flex w-full flex-col gap-2">
        {data.map((datum) => {
          const pct = sum > 0 ? Math.round((datum.value / sum) * 100) : 0
          return (
            <li key={datum.label} className="flex items-center gap-2 text-sm">
              <svg width={12} height={12} aria-hidden="true" className="shrink-0">
                <circle
                  cx={6}
                  cy={6}
                  r={4}
                  fill="none"
                  strokeWidth={4}
                  className={cn(datum.color ?? DEFAULT_SEGMENT_CLASS)}
                />
              </svg>
              <span className="flex-1 truncate text-foreground" title={datum.label}>
                {datum.label}
              </span>
              <span className="font-medium tabular-nums text-foreground">{datum.value}</span>
              <span className="w-10 text-right tabular-nums text-muted-foreground">{pct}%</span>
            </li>
          )
        })}
      </ul>
    </div>
  )
}
