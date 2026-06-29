import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { type SendStatus, sendStatusLabel } from '../types'

/**
 * Clases de color por estado de envío. Cada entrada incluye variante clara y oscura para mantener
 * el contraste (WCAG A) en ambos temas. La información se transmite por color **y** por etiqueta
 * textual (nunca sólo por color).
 */
const SEND_STATUS_CLASSES: Record<SendStatus, string> = {
  pending: 'bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-200',
  sent: 'bg-lime-100 text-lime-800 dark:bg-lime-950 dark:text-lime-300',
  failed: 'bg-red-100 text-red-800 dark:bg-red-950 dark:text-red-200',
  skipped: 'bg-zinc-200 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300',
  retrying: 'bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-200',
}

interface ShipmentStatusBadgeProps {
  status: SendStatus
}

/**
 * Insignia del estado de envío de una factura (spec 019). Muestra color + etiqueta textual.
 */
export function ShipmentStatusBadge({ status }: ShipmentStatusBadgeProps) {
  const colorClass = SEND_STATUS_CLASSES[status] ?? 'bg-muted text-muted-foreground'

  return (
    <Badge variant="outline" className={cn('border-transparent', colorClass)}>
      {sendStatusLabel(status)}
    </Badge>
  )
}
