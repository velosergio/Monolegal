import type { ChartDatum } from './types'

/**
 * Reduce un mapa `clientId → cantidad` a los `limit` clientes con más facturas,
 * agrupando el resto bajo una entrada "Otros" (research D6). El resultado queda
 * ordenado de mayor a menor; "Otros" se añade al final solo si hay sobrantes.
 */
export function topClients(byClient: Record<string, number>, limit = 5): ChartDatum[] {
  const sorted = Object.entries(byClient)
    .map(([label, value]) => ({ label, value }))
    .sort((a, b) => b.value - a.value)

  if (sorted.length <= limit) {
    return sorted
  }

  const top = sorted.slice(0, limit)
  const restTotal = sorted.slice(limit).reduce((sum, entry) => sum + entry.value, 0)

  return restTotal > 0 ? [...top, { label: 'Otros', value: restTotal }] : top
}

const lastRefreshFormatter = new Intl.DateTimeFormat('es-CO', {
  dateStyle: 'medium',
  timeStyle: 'short',
})

/** Formatea el instante del último refresco (epoch ms) en español. */
export function formatLastRefresh(updatedAt: number): string {
  if (!updatedAt) return '—'
  return lastRefreshFormatter.format(new Date(updatedAt))
}
