import { useState } from 'react'
import { useToast } from '@/components/feedback/useToast'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogClose,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { useResendFailed, useSanitizeStuck } from '../api/useEmailTools'

/** Sección de herramientas globales de administración de envíos (spec 017, US4). */
export function AdminToolsSection() {
  const toast = useToast()
  const resend = useResendFailed()
  const sanitize = useSanitizeStuck()
  const [confirmOpen, setConfirmOpen] = useState(false)

  function handleResend() {
    resend.mutate(undefined, {
      onSuccess: (result) => {
        if (result.attempted === 0) {
          toast.success('No hay notificaciones fallidas que reenviar.')
          return
        }
        toast.success(
          `Reenvío completado: ${result.resent} reenviadas, ${result.failed} fallidas (de ${result.attempted}).`
        )
      },
      onError: (err) =>
        toast.error(
          err instanceof Error ? err.message : 'No se pudo reenviar las notificaciones fallidas.'
        ),
    })
  }

  function handleSanitizeConfirmed() {
    setConfirmOpen(false)
    sanitize.mutate(undefined, {
      onSuccess: (result) => {
        if (result.sanitized === 0) {
          toast.success('No hay notificaciones atascadas que sanear.')
          return
        }
        toast.success(`Saneamiento completado: ${result.sanitized} notificaciones marcadas.`)
      },
      onError: (err) =>
        toast.error(
          err instanceof Error ? err.message : 'No se pudo sanear las notificaciones atascadas.'
        ),
    })
  }

  return (
    <div className="rounded-lg border p-5">
      <div className="flex flex-col gap-1">
        <h2 className="font-heading text-base font-bold">Herramientas de administración</h2>
        <p className="text-sm text-muted-foreground">
          Operaciones globales sobre el estado de notificación de las facturas.
        </p>
      </div>

      <div className="mt-4 flex flex-col gap-4">
        <div className="flex flex-col gap-2 rounded-md border p-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex flex-col gap-0.5">
            <span className="text-sm font-medium">Reenviar notificaciones fallidas</span>
            <span className="text-sm text-muted-foreground">
              Reintenta el envío de todas las facturas con último resultado fallido.
            </span>
          </div>
          <Button
            type="button"
            variant="outline"
            disabled={resend.isPending}
            onClick={handleResend}
          >
            {resend.isPending ? 'Reenviando…' : 'Reenviar fallidas'}
          </Button>
        </div>

        <div className="flex flex-col gap-2 rounded-md border p-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex flex-col gap-0.5">
            <span className="text-sm font-medium">Sanear notificaciones atascadas</span>
            <span className="text-sm text-muted-foreground">
              Marca como fallidas las facturas en estado notificable sin notificación registrada.
            </span>
          </div>
          <Button
            type="button"
            variant="destructive"
            disabled={sanitize.isPending}
            onClick={() => setConfirmOpen(true)}
          >
            {sanitize.isPending ? 'Saneando…' : 'Sanear atascadas'}
          </Button>
        </div>
      </div>

      <Dialog open={confirmOpen} onOpenChange={setConfirmOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Sanear notificaciones atascadas</DialogTitle>
            <DialogDescription>
              Esta acción marca como fallidas las notificaciones atascadas, conservando su registro.
              No reintenta el envío ni borra datos. ¿Deseas continuar?
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Cancelar
              </Button>
            </DialogClose>
            <Button type="button" variant="destructive" onClick={handleSanitizeConfirmed}>
              Sanear
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
