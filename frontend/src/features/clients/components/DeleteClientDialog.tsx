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
import { useDeleteClient } from '../api/mutations'
import type { Client } from '../types'

interface DeleteClientDialogProps {
  /** Cliente a eliminar; `null` ⇒ cerrado. */
  client: Client | null
  onClose: () => void
}

/**
 * Modal de confirmación de eliminación de cliente (spec 018, RF-017). Traduce el conflicto 409
 * (facturas asociadas, RF-018) en un mensaje claro vía toast.
 */
export function DeleteClientDialog({ client, onClose }: DeleteClientDialogProps) {
  const toast = useToast()
  const deleteMutation = useDeleteClient()

  async function handleConfirm() {
    if (!client) return
    try {
      await deleteMutation.mutateAsync(client.id)
      toast.success('Cliente eliminado correctamente.')
      onClose()
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'No se pudo eliminar el cliente.')
    }
  }

  return (
    <Dialog open={Boolean(client)} onOpenChange={(next) => !next && onClose()}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle>Eliminar cliente</DialogTitle>
          <DialogDescription>
            ¿Seguro que deseas eliminar a <strong>{client?.name}</strong>? Esta acción no se puede
            deshacer.
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
