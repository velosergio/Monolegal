import { useState } from 'react'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useTransitionInvoice } from '../api/useTransitionInvoice'
import type { InvoiceStatus } from '../types'
import { statusLabel } from '../types'

interface ChangeStatusControlProps {
  invoiceId: string
  /** Estado actual de la factura (contexto; no es un destino seleccionable). */
  currentStatus: InvoiceStatus
  /** Destinos válidos provistos por el backend (única fuente de verdad). */
  allowedTransitions: InvoiceStatus[]
}

/**
 * Control de cambio de estado dentro del modal (spec 015, US3).
 *
 * Ofrece solo los destinos válidos del backend y ejecuta la transición vía
 * `useTransitionInvoice`. En estados terminales (sin transiciones) no muestra el
 * control. Gestiona el estado ocupado (evita doble envío) y muestra errores legibles.
 */
export function ChangeStatusControl({
  invoiceId,
  currentStatus,
  allowedTransitions,
}: ChangeStatusControlProps) {
  const [selected, setSelected] = useState<InvoiceStatus | ''>('')
  const mutation = useTransitionInvoice()

  if (allowedTransitions.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        Esta factura está en estado «{statusLabel(currentStatus)}» y no admite cambios de estado.
      </p>
    )
  }

  const handleSubmit = () => {
    if (selected === '' || mutation.isPending) return
    mutation.mutate({ id: invoiceId, newStatus: selected }, { onSuccess: () => setSelected('') })
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
        <Select
          value={selected || undefined}
          onValueChange={(next) => setSelected(next as InvoiceStatus)}
          disabled={mutation.isPending}
        >
          <SelectTrigger className="w-full sm:w-[220px]" aria-label="Nuevo estado">
            <SelectValue placeholder="Selecciona un estado" />
          </SelectTrigger>
          <SelectContent>
            {allowedTransitions.map((status) => (
              <SelectItem key={status} value={status}>
                {statusLabel(status)}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>

        <Button
          type="button"
          onClick={handleSubmit}
          disabled={selected === '' || mutation.isPending}
        >
          {mutation.isPending ? 'Cambiando…' : 'Cambiar Estado'}
        </Button>
      </div>

      {mutation.isError ? (
        <p role="alert" className="text-sm text-destructive">
          No se pudo cambiar el estado. {(mutation.error as Error).message}
        </p>
      ) : null}
    </div>
  )
}
