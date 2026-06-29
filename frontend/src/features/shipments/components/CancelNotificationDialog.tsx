import { useToast } from '@/components/feedback/useToast'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { useCancelNotification } from '../api/useShipmentMutations'
import type { Shipment } from '../types'

interface CancelNotificationDialogProps {
  /** Envío a cancelar; `null` ⇒ cerrado. */
  shipment: Shipment | null
  onClose: () => void
}

/**
 * Confirmación de "cancelar envío" (spec 019, US4): marca como omitida la notificación pendiente de
 * una factura para que el worker no la procese. Acción de dominio que requiere confirmación explícita.
 */
export function CancelNotificationDialog({ shipment, onClose }: CancelNotificationDialogProps) {
  const toast = useToast()
  const cancel = useCancelNotification()

  async function handleConfirm() {
    if (!shipment) return
    try {
      await cancel.mutateAsync(shipment.id)
      toast.success(`Envío de ${shipment.clientName} marcado como omitido.`)
      onClose()
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'No se pudo cancelar el envío.')
    }
  }

  return (
    <Dialog open={Boolean(shipment)} onOpenChange={(next) => !next && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Cancelar envío</DialogTitle>
          <DialogDescription>
            Se marcará como <strong>omitida</strong> la notificación de{' '}
            <strong>{shipment?.clientName ?? ''}</strong> y el worker no la procesará. El registro
            se conserva. ¿Deseas continuar?
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button type="button" variant="outline" onClick={onClose} disabled={cancel.isPending}>
            Volver
          </Button>
          <Button
            type="button"
            variant="destructive"
            onClick={handleConfirm}
            disabled={cancel.isPending}
          >
            {cancel.isPending ? 'Cancelando…' : 'Cancelar envío'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
