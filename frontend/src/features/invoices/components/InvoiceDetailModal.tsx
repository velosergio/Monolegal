import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { InvoiceNotFoundError } from '../api/getInvoiceDetail'
import { useInvoiceDetail } from '../api/useInvoiceDetail'
import { ChangeStatusControl } from './ChangeStatusControl'
import { InvoiceDetailFields } from './InvoiceDetailFields'
import { InvoiceDetailSkeleton } from './InvoiceDetailSkeleton'
import { StatusHistoryTimeline } from './StatusHistoryTimeline'

interface InvoiceDetailModalProps {
  /** Id de la factura a mostrar; `null` mantiene el modal cerrado. */
  invoiceId: string | null
  /** Se invoca cuando el usuario cierra el modal (botón, escape, overlay). */
  onClose: () => void
}

/**
 * Modal de detalle de una factura (spec 015, US1). Orquesta la carga del detalle vía
 * TanStack Query y muestra los estados de carga (skeleton), error (genérico/404) y datos.
 * El foco se gestiona y devuelve por el primitivo Dialog (Radix).
 */
export function InvoiceDetailModal({ invoiceId, onClose }: InvoiceDetailModalProps) {
  const query = useInvoiceDetail(invoiceId)

  return (
    <Dialog
      open={invoiceId != null}
      onOpenChange={(open) => {
        if (!open) onClose()
      }}
    >
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Detalle de la factura</DialogTitle>
          <DialogDescription>
            Información completa, historial de estados y acciones de la factura seleccionada.
          </DialogDescription>
        </DialogHeader>

        {query.isLoading ? (
          <InvoiceDetailSkeleton />
        ) : query.isError ? (
          <DetailError error={query.error} onRetry={() => query.refetch()} />
        ) : query.data ? (
          <div className="flex flex-col gap-6">
            <InvoiceDetailFields invoice={query.data} />
            <section className="flex flex-col gap-3">
              <h3 className="text-sm font-semibold text-foreground">Historial de estados</h3>
              <StatusHistoryTimeline
                statusHistory={query.data.statusHistory}
                createdAt={query.data.createdAt}
              />
            </section>
            <section className="flex flex-col gap-3 border-t border-border pt-4">
              <h3 className="text-sm font-semibold text-foreground">Cambiar estado</h3>
              <ChangeStatusControl
                invoiceId={query.data.id}
                currentStatus={query.data.status}
                allowedTransitions={query.data.allowedTransitions}
              />
            </section>
          </div>
        ) : null}
      </DialogContent>
    </Dialog>
  )
}

function DetailError({ error, onRetry }: { error: unknown; onRetry: () => void }) {
  if (error instanceof InvoiceNotFoundError) {
    return (
      <div role="alert" className="flex flex-col items-center gap-2 py-8 text-center">
        <p className="font-medium text-foreground">Factura no encontrada.</p>
        <p className="text-sm text-muted-foreground">
          La factura ya no existe o el enlace es inválido.
        </p>
      </div>
    )
  }

  return (
    <div role="alert" className="flex flex-col items-center gap-3 py-8 text-center">
      <p className="font-medium text-foreground">No se pudo cargar el detalle.</p>
      <p className="text-sm text-muted-foreground">Revisa tu conexión e inténtalo de nuevo.</p>
      <Button type="button" variant="outline" onClick={onRetry}>
        Reintentar
      </Button>
    </div>
  )
}
