import { useEffect, useId, useState } from 'react'
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
import { Input } from '@/components/ui/input'
import { useClients } from '@/features/clients/api/useClients'
import { useCreateInvoice, useUpdateInvoice } from '../api/invoiceMutations'
import type { InvoiceDetail, InvoiceFormValues } from '../types'
import { InvoiceItemsEditor } from './InvoiceItemsEditor'

interface InvoiceFormModalProps {
  open: boolean
  /** Factura a editar; `null`/ausente ⇒ modo creación. */
  invoice?: InvoiceDetail | null
  onClose: () => void
}

function emptyValues(): InvoiceFormValues {
  return { clientId: '', dueDate: '', items: [{ description: '', quantity: 1, unitPrice: 0 }] }
}

function toDateInput(iso: string): string {
  return iso ? new Date(iso).toISOString().slice(0, 10) : ''
}

interface FormErrors {
  clientId?: string
  dueDate?: string
  items?: string
}

function validate(values: InvoiceFormValues): FormErrors {
  const errors: FormErrors = {}
  if (!values.clientId) errors.clientId = 'El cliente es obligatorio.'
  if (!values.dueDate) errors.dueDate = 'La fecha de vencimiento es obligatoria.'
  const validItems = values.items.filter(
    (i) => i.description.trim() && i.quantity > 0 && i.unitPrice > 0
  )
  if (validItems.length === 0)
    errors.items = 'Agrega al menos una línea con descripción, cantidad y precio positivos.'
  return errors
}

/**
 * Modal de alta/edición de factura (spec 018, RF-001/RF-003). El monto se deriva de los items
 * (solo lectura). Bloquea la edición en estado terminal (RF-004a), valida en cliente y muestra toasts.
 */
export function InvoiceFormModal({ open, invoice, onClose }: InvoiceFormModalProps) {
  const isEdit = Boolean(invoice)
  const toast = useToast()
  const formId = useId()
  const createMutation = useCreateInvoice()
  const updateMutation = useUpdateInvoice()
  const isPending = createMutation.isPending || updateMutation.isPending

  // Para el selector de cliente (admin tool: primeras 50 entradas por nombre).
  const clientsQuery = useClients({ search: '', page: 1, pageSize: 50 })

  const [values, setValues] = useState<InvoiceFormValues>(emptyValues)
  const [errors, setErrors] = useState<FormErrors>({})

  const isTerminal = invoice?.status === 'pagado' || invoice?.status === 'desactivado'

  useEffect(() => {
    if (!open) return
    setErrors({})
    setValues(
      invoice
        ? {
            clientId: invoice.clientId,
            dueDate: toDateInput(invoice.dueDate),
            items: invoice.items.map((i) => ({
              description: i.description,
              quantity: i.quantity,
              unitPrice: i.unitPrice,
            })),
          }
        : emptyValues()
    )
  }, [open, invoice])

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    if (isTerminal) {
      toast.error('No se puede editar una factura en estado terminal (pagado/desactivado).')
      return
    }
    const validationErrors = validate(values)
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors)
      return
    }

    // Solo se envían líneas válidas (descartando filas vacías intermedias).
    const payload: InvoiceFormValues = {
      ...values,
      items: values.items.filter((i) => i.description.trim() && i.quantity > 0 && i.unitPrice > 0),
    }

    try {
      if (isEdit && invoice) {
        await updateMutation.mutateAsync({ id: invoice.id, values: payload })
        toast.success('Factura actualizada correctamente.')
      } else {
        await createMutation.mutateAsync(payload)
        toast.success('Factura creada correctamente.')
      }
      onClose()
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Ocurrió un error inesperado.')
    }
  }

  const clients = clientsQuery.data?.data ?? []

  return (
    <Dialog open={open} onOpenChange={(next) => !next && onClose()}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Editar factura' : 'Nueva factura'}</DialogTitle>
          <DialogDescription>
            {isTerminal
              ? 'Esta factura está en un estado terminal y no puede editarse.'
              : 'El monto total se calcula automáticamente a partir de las líneas de detalle.'}
          </DialogDescription>
        </DialogHeader>

        <form id={formId} onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
          <label className="flex flex-col gap-1.5 text-sm">
            <span className="font-medium">Cliente</span>
            <select
              className="h-10 rounded-[2px] border border-input bg-background px-3 text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring disabled:opacity-50"
              value={values.clientId}
              onChange={(e) => setValues((p) => ({ ...p, clientId: e.target.value }))}
              disabled={isTerminal || isPending}
              aria-invalid={Boolean(errors.clientId)}
            >
              <option value="">Selecciona un cliente…</option>
              {clients.map((client) => (
                <option key={client.id} value={client.id}>
                  {client.name} ({client.email})
                </option>
              ))}
            </select>
            {errors.clientId ? (
              <span role="alert" className="text-xs text-destructive">
                {errors.clientId}
              </span>
            ) : null}
          </label>

          <label className="flex flex-col gap-1.5 text-sm">
            <span className="font-medium">Fecha de vencimiento</span>
            <Input
              type="date"
              value={values.dueDate}
              onChange={(e) => setValues((p) => ({ ...p, dueDate: e.target.value }))}
              disabled={isTerminal || isPending}
              aria-invalid={Boolean(errors.dueDate)}
            />
            {errors.dueDate ? (
              <span role="alert" className="text-xs text-destructive">
                {errors.dueDate}
              </span>
            ) : null}
          </label>

          <InvoiceItemsEditor
            items={values.items}
            onChange={(items) => setValues((p) => ({ ...p, items }))}
            error={errors.items}
            disabled={isTerminal || isPending}
          />
        </form>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={onClose} disabled={isPending}>
            Cancelar
          </Button>
          <Button type="submit" form={formId} disabled={isPending || isTerminal}>
            {isPending ? 'Guardando…' : isEdit ? 'Guardar cambios' : 'Crear factura'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
