import { AlertTriangle } from 'lucide-react'
import { type ReactNode, useId, useState } from 'react'
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
import { Input } from '@/components/ui/input'
import { useDeleteAllData, useFlushDatabase } from '../api/useMaintenance'

/**
 * Zona de peligro de /configuracion: operaciones destructivas e irreversibles.
 * Cada acción exige escribir una palabra de confirmación antes de habilitar el botón.
 */
export function DangerZoneSection() {
  const toast = useToast()
  const deleteAll = useDeleteAllData()
  const flush = useFlushDatabase()

  function handleDeleteAll() {
    deleteAll.mutate(undefined, {
      onSuccess: (result) => {
        toast.success(`Datos eliminados: ${result.deletedInvoices} factura(s) borrada(s).`)
      },
      onError: (err) =>
        toast.error(err instanceof Error ? err.message : 'No se pudieron eliminar los datos.'),
    })
  }

  function handleFlush() {
    flush.mutate(undefined, {
      onSuccess: (result) => {
        toast.success(
          `Base de datos reiniciada: ${result.deletedInvoices} factura(s) borrada(s); ` +
            `sembradas ${result.invoicesCreated} factura(s) de ${result.clientsCreated} cliente(s).`
        )
      },
      onError: (err) =>
        toast.error(err instanceof Error ? err.message : 'No se pudo vaciar la base de datos.'),
    })
  }

  return (
    <div className="rounded-lg border border-destructive/50 p-5">
      <div className="flex items-start gap-2">
        <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-destructive" aria-hidden="true" />
        <div className="flex flex-col gap-1">
          <h2 className="font-heading text-base font-bold text-destructive">Zona de peligro</h2>
          <p className="text-sm text-muted-foreground">
            Acciones destructivas e irreversibles. Procede con precaución.
          </p>
        </div>
      </div>

      <div className="mt-4 flex flex-col gap-4">
        <div className="flex flex-col gap-2 rounded-md border border-destructive/30 p-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex flex-col gap-0.5">
            <span className="text-sm font-medium">Eliminar todos los datos</span>
            <span className="text-sm text-muted-foreground">
              Borra todos los registros (facturas) conservando la base de datos y la configuración.
            </span>
          </div>
          <DangerAction
            triggerLabel="Eliminar datos"
            pendingLabel="Eliminando…"
            isPending={deleteAll.isPending}
            confirmWord="ELIMINAR"
            title="Eliminar todos los datos"
            confirmLabel="Eliminar todo"
            onConfirm={handleDeleteAll}
          >
            Esta acción borra <strong>todas las facturas</strong> de forma permanente. La
            configuración del sistema se conserva. No se puede deshacer.
          </DangerAction>
        </div>

        <div className="flex flex-col gap-2 rounded-md border border-destructive/30 p-4 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex flex-col gap-0.5">
            <span className="text-sm font-medium">Flush DB</span>
            <span className="text-sm text-muted-foreground">
              Vacía por completo la base de datos (incluida la configuración) y ejecuta el
              sembrador.
            </span>
          </div>
          <DangerAction
            triggerLabel="Vaciar base de datos"
            pendingLabel="Vaciando…"
            isPending={flush.isPending}
            confirmWord="FLUSH"
            title="Vaciar base de datos (Flush DB)"
            confirmLabel="Vaciar y sembrar"
            onConfirm={handleFlush}
          >
            Esta acción <strong>elimina toda la base de datos</strong>, incluida la configuración
            del sistema, y vuelve a sembrar los datos de ejemplo. No se puede deshacer.
          </DangerAction>
        </div>
      </div>
    </div>
  )
}

interface DangerActionProps {
  triggerLabel: string
  pendingLabel: string
  isPending: boolean
  confirmWord: string
  title: string
  confirmLabel: string
  onConfirm: () => void
  children: ReactNode
}

/** Botón + diálogo de confirmación que exige escribir una palabra clave antes de ejecutar. */
function DangerAction({
  triggerLabel,
  pendingLabel,
  isPending,
  confirmWord,
  title,
  confirmLabel,
  onConfirm,
  children,
}: DangerActionProps) {
  const [open, setOpen] = useState(false)
  const [text, setText] = useState('')
  const inputId = useId()
  const confirmed = text.trim().toUpperCase() === confirmWord

  function handleOpenChange(next: boolean) {
    setOpen(next)
    if (!next) setText('')
  }

  function handleConfirm() {
    if (!confirmed) return
    handleOpenChange(false)
    onConfirm()
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <Button
        type="button"
        variant="destructive"
        disabled={isPending}
        onClick={() => setOpen(true)}
      >
        {isPending ? pendingLabel : triggerLabel}
      </Button>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{children}</DialogDescription>
        </DialogHeader>

        <div className="flex flex-col gap-2">
          <label htmlFor={inputId} className="text-sm font-medium">
            Escribe <span className="font-mono font-bold">{confirmWord}</span> para confirmar
          </label>
          <Input
            id={inputId}
            value={text}
            onChange={(e) => setText(e.target.value)}
            autoComplete="off"
            placeholder={confirmWord}
          />
        </div>

        <DialogFooter>
          <DialogClose asChild>
            <Button type="button" variant="outline">
              Cancelar
            </Button>
          </DialogClose>
          <Button type="button" variant="destructive" disabled={!confirmed} onClick={handleConfirm}>
            {confirmLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
