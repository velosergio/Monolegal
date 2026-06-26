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
import { useDeleteInvoice } from '../api/invoiceMutations'
import { shortId } from '../utils'

interface DeleteInvoiceDialogProps {
  /** Id de la factura a eliminar; `null` ⇒ cerrado. */
  invoiceId: string | null
  onClose: () => void
}

/**
 * Modal de confirmación de eliminación de factura (spec 018, RF-005). La eliminación es permanente
 * y permitida en cualquier estado.
 */
export function DeleteInvoiceDialog({ invoiceId, onClose }: DeleteInvoiceDialogProps) {
  const toast = useToast()
  const deleteMutation = useDeleteInvoice()

  async function handleConfirm() {
    if (!invoiceId) return
    try {
      await deleteMutation.mutateAsync(invoiceId)
      toast.success('Factura eliminada correctamente.')
      onClose()
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'No se pudo eliminar la factura.')
    }
  }

  return (
    <Dialog open={Boolean(invoiceId)} onOpenChange={(next) => !next && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Eliminar factura</DialogTitle>
          <DialogDescription>
            ¿Seguro que deseas eliminar la factura{' '}
            <strong>{invoiceId ? shortId(invoiceId) : ''}</strong>? Esta acción es permanente.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button
            type="button"
            variant="outline"
            onClick={onClose}
            disabled={deleteMutation.isPending}
          >
            Cancelar
          </Button>
          <Button
            type="button"
            variant="destructive"
            onClick={handleConfirm}
            disabled={deleteMutation.isPending}
          >
            {deleteMutation.isPending ? 'Eliminando…' : 'Eliminar'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
