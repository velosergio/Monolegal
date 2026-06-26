import type { StatusChange } from '../types'
import { statusLabel } from '../types'
import { formatDate, statusChangeSourceLabel } from '../utils'

interface StatusHistoryTimelineProps {
  /** Historial de cambios de estado de la factura (puede llegar desordenado). */
  statusHistory: StatusChange[]
  /** Fecha de creación de la factura; respalda el evento inicial cuando no hay historial. */
  createdAt: string
}

/**
 * Línea de tiempo del historial de cambios de estado (spec 015, US2).
 *
 * Ordena los eventos cronológicamente (más antiguo primero) y, cuando el historial
 * está vacío, deriva un único evento de creación a partir de `createdAt` (FR-010).
 */
export function StatusHistoryTimeline({ statusHistory, createdAt }: StatusHistoryTimelineProps) {
  const ordered = [...statusHistory].sort(
    (a, b) => new Date(a.at).getTime() - new Date(b.at).getTime()
  )

  if (ordered.length === 0) {
    return (
      <ol className="flex flex-col gap-3">
        <li className="flex flex-col gap-1 border-l-2 border-border pl-4">
          <span className="text-sm font-medium text-foreground">Factura creada</span>
          <time className="text-xs text-muted-foreground" dateTime={createdAt}>
            {formatDate(createdAt)}
          </time>
        </li>
      </ol>
    )
  }

  return (
    <ol className="flex flex-col gap-3">
      {ordered.map((change) => (
        <li
          key={`${change.at}-${change.from}-${change.to}`}
          className="flex flex-col gap-1 border-l-2 border-border pl-4"
        >
          <span className="text-sm text-foreground">
            <FromLabel status={change.from} />
            <span aria-hidden="true" className="mx-1 text-muted-foreground">
              →
            </span>
            <span className="font-medium">{statusLabel(change.to)}</span>
          </span>
          <span className="flex flex-wrap items-center gap-2 text-xs text-muted-foreground">
            <time dateTime={change.at}>{formatDate(change.at)}</time>
            <span aria-hidden="true">·</span>
            <span>{statusChangeSourceLabel(change.source)}</span>
          </span>
        </li>
      ))}
    </ol>
  )
}

/**
 * Estado origen de una transición, renderizado en tono atenuado. El estado destino
 * de un evento coincide con el origen del siguiente; para que cada etiqueta de estado
 * tenga un único nodo de texto identificable (y no duplicar el destino), el origen se
 * compone por palabras dentro de un mismo contenedor.
 */
function FromLabel({ status }: { status: StatusChange['from'] }) {
  const words = statusLabel(status).split(' ')
  const segments: { key: string; text: string }[] = []
  let prefix = ''
  for (const word of words) {
    const text = prefix === '' ? word : ` ${word}`
    prefix = prefix === '' ? word : `${prefix} ${word}`
    segments.push({ key: prefix, text })
  }
  return (
    <span className="text-muted-foreground">
      {segments.map((segment) => (
        <span key={segment.key}>{segment.text}</span>
      ))}
    </span>
  )
}
