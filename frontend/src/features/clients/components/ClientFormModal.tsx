import { useId, useState } from 'react'
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
import { useCreateClient, useUpdateClient } from '../api/mutations'
import type { Client, ClientFormValues } from '../types'

interface ClientFormModalProps {
  open: boolean
  /** Cliente a editar; `null`/ausente ⇒ modo creación. */
  client?: Client | null
  onClose: () => void
}

const EMPTY: ClientFormValues = { name: '', email: '', phone: '', address: '' }

/** Valores iniciales del formulario derivados del cliente a editar (o vacíos en alta). */
function initialValues(client?: Client | null): ClientFormValues {
  return client
    ? {
        name: client.name,
        email: client.email,
        phone: client.phone ?? '',
        address: client.address ?? '',
      }
    : EMPTY
}

/** Validación espejo de la del backend (RF-015): nombre no vacío y email con formato válido. */
function validate(values: ClientFormValues): Partial<Record<keyof ClientFormValues, string>> {
  const errors: Partial<Record<keyof ClientFormValues, string>> = {}
  if (!values.name.trim()) errors.name = 'El nombre es obligatorio.'
  if (!values.email.trim()) errors.email = 'El email es obligatorio.'
  else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(values.email.trim()))
    errors.email = 'El email debe tener un formato válido.'
  return errors
}

/**
 * Modal de alta/edición de cliente (spec 018, RF-014/RF-016). Valida en cliente, muestra toast
 * de éxito/error y conserva los datos del formulario ante error (RF-009).
 */
export function ClientFormModal({ open, client, onClose }: ClientFormModalProps) {
  return (
    <Dialog open={open} onOpenChange={(next) => !next && onClose()}>
      <DialogContent>
        {/* La key remonta el formulario al cambiar de cliente, reiniciando su estado sin efectos. */}
        {open ? <ClientForm key={client?.id ?? 'new'} client={client} onClose={onClose} /> : null}
      </DialogContent>
    </Dialog>
  )
}

interface ClientFormProps {
  client?: Client | null
  onClose: () => void
}

function ClientForm({ client, onClose }: ClientFormProps) {
  const isEdit = Boolean(client)
  const toast = useToast()
  const formId = useId()
  const createMutation = useCreateClient()
  const updateMutation = useUpdateClient()
  const isPending = createMutation.isPending || updateMutation.isPending

  const [values, setValues] = useState<ClientFormValues>(() => initialValues(client))
  const [errors, setErrors] = useState<Partial<Record<keyof ClientFormValues, string>>>({})

  const setField = (field: keyof ClientFormValues) => (value: string) =>
    setValues((prev) => ({ ...prev, [field]: value }))

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    const validationErrors = validate(values)
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors)
      return
    }

    try {
      if (isEdit && client) {
        await updateMutation.mutateAsync({ id: client.id, values })
        toast.success('Cliente actualizado correctamente.')
      } else {
        await createMutation.mutateAsync(values)
        toast.success('Cliente creado correctamente.')
      }
      onClose()
    } catch (error) {
      toast.error(error instanceof Error ? error.message : 'Ocurrió un error inesperado.')
    }
  }

  return (
    <>
      <DialogHeader>
        <DialogTitle>{isEdit ? 'Editar cliente' : 'Nuevo cliente'}</DialogTitle>
        <DialogDescription>
          Nombre y email son obligatorios. Teléfono y dirección son opcionales.
        </DialogDescription>
      </DialogHeader>

      <form id={formId} onSubmit={handleSubmit} className="flex flex-col gap-4" noValidate>
        <label htmlFor={`${formId}-name`} className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium">Nombre</span>
          <Input
            id={`${formId}-name`}
            value={values.name}
            onChange={(e) => setField('name')(e.target.value)}
            aria-invalid={Boolean(errors.name)}
          />
          {errors.name ? (
            <span role="alert" className="text-xs text-destructive">
              {errors.name}
            </span>
          ) : null}
        </label>

        <label htmlFor={`${formId}-email`} className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium">Email</span>
          <Input
            id={`${formId}-email`}
            type="email"
            value={values.email}
            onChange={(e) => setField('email')(e.target.value)}
            aria-invalid={Boolean(errors.email)}
          />
          {errors.email ? (
            <span role="alert" className="text-xs text-destructive">
              {errors.email}
            </span>
          ) : null}
        </label>

        <label htmlFor={`${formId}-phone`} className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium">Teléfono (opcional)</span>
          <Input
            id={`${formId}-phone`}
            value={values.phone}
            onChange={(e) => setField('phone')(e.target.value)}
          />
        </label>

        <label htmlFor={`${formId}-address`} className="flex flex-col gap-1.5 text-sm">
          <span className="font-medium">Dirección (opcional)</span>
          <Input
            id={`${formId}-address`}
            value={values.address}
            onChange={(e) => setField('address')(e.target.value)}
          />
        </label>
      </form>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onClose} disabled={isPending}>
          Cancelar
        </Button>
        <Button type="submit" form={formId} disabled={isPending}>
          {isPending ? 'Guardando…' : isEdit ? 'Guardar cambios' : 'Crear cliente'}
        </Button>
      </DialogFooter>
    </>
  )
}
