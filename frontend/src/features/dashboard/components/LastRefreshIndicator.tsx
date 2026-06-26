import { RefreshCw } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { formatLastRefresh } from '../utils'

interface LastRefreshIndicatorProps {
  /** Instante de la última actualización (`dataUpdatedAt` de TanStack Query). */
  updatedAt: number
  onRefresh: () => void
  isRefreshing: boolean
}

/**
 * Indica cuándo se actualizaron por última vez las estadísticas y permite
 * refrescarlas manualmente (sin polling automático, FR-021a).
 */
export function LastRefreshIndicator({
  updatedAt,
  onRefresh,
  isRefreshing,
}: LastRefreshIndicatorProps) {
  return (
    <div className="flex items-center gap-3 text-sm text-muted-foreground">
      <span>Actualizado: {formatLastRefresh(updatedAt)}</span>
      <Button type="button" variant="outline" size="sm" onClick={onRefresh} disabled={isRefreshing}>
        <RefreshCw aria-hidden="true" className={isRefreshing ? 'size-4 animate-spin' : 'size-4'} />
        {isRefreshing ? 'Actualizando…' : 'Actualizar'}
      </Button>
    </div>
  )
}
