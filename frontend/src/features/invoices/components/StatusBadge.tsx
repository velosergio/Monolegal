import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'
import { type InvoiceStatus, type KnownInvoiceStatus, statusLabel } from '../types'

/**
 * Clases de color por estado. Cada entrada incluye variante clara y oscura para
 * mantener el contraste (WCAG A) en ambos temas.
 */
const STATUS_CLASSES: Record<KnownInvoiceStatus, string> = {
  pending: 'bg-amber-100 text-amber-800 dark:bg-amber-950 dark:text-amber-200',
  pagado: 'bg-lime-100 text-lime-800 dark:bg-lime-950 dark:text-lime-300',
  primerrecordatorio: 'bg-blue-100 text-blue-800 dark:bg-blue-950 dark:text-blue-200',
  segundorecordatorio: 'bg-orange-100 text-orange-800 dark:bg-orange-950 dark:text-orange-200',
  desactivado: 'bg-zinc-200 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-300',
}

interface StatusBadgeProps {
  status: InvoiceStatus
}

/**
 * Insignia de estado de factura. Para estados desconocidos usa un estilo neutro
 * y muestra el valor en bruto (compatibilidad futura).
 */
export function StatusBadge({ status }: StatusBadgeProps) {
  const colorClass =
    STATUS_CLASSES[status as KnownInvoiceStatus] ?? 'bg-muted text-muted-foreground'

  return (
    <Badge variant="outline" className={cn('border-transparent', colorClass)}>
      {statusLabel(status)}
    </Badge>
  )
}
