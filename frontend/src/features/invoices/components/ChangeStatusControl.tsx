import { domAnimation, LazyMotion, m, useReducedMotion } from 'motion/react'
import { type FormEvent, useId, useState } from 'react'
import { useToast } from '@/components/feedback/useToast'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { motionTransition } from '@/lib/motion'
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
 * Formulario de cambio de estado dentro del modal (spec 016, US1 / roadmap 4.5).
 *
 * Ofrece solo los destinos válidos del backend, valida en el cliente que haya un
 * destino seleccionado antes de enviar, ejecuta la transición vía
 * `useTransitionInvoice` y notifica el resultado con un *toast* de éxito/error,
 * conservando además un mensaje de error inline persistente. En estados terminales
 * (sin transiciones) no muestra el formulario. Gestiona el estado ocupado para
 * evitar el doble envío.
 */
export function ChangeStatusControl({
  invoiceId,
  currentStatus,
  allowedTransitions,
}: ChangeStatusControlProps) {
  const [selected, setSelected] = useState<InvoiceStatus | ''>('')
  const [validationError, setValidationError] = useState<string | null>(null)
  const mutation = useTransitionInvoice()
  const toast = useToast()
  const reduced = useReducedMotion()
  const validationId = useId()

  if (allowedTransitions.length === 0) {
    return (
      <p className="text-sm text-muted-foreground">
        Esta factura está en estado «{statusLabel(currentStatus)}» y no admite cambios de estado.
      </p>
    )
  }

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    if (mutation.isPending) return
    if (selected === '') {
      setValidationError('Selecciona un estado destino.')
      return
    }
    setValidationError(null)
    const targetLabel = statusLabel(selected)
    mutation.mutate(
      { id: invoiceId, newStatus: selected },
      {
        onSuccess: () => {
          setSelected('')
          toast.success(`Estado actualizado a «${targetLabel}».`)
        },
        onError: (error) => {
          toast.error(`No se pudo cambiar el estado. ${(error as Error).message}`)
        },
      }
    )
  }

  return (
    <LazyMotion features={domAnimation}>
      <m.form
        onSubmit={handleSubmit}
        className="flex flex-col gap-3"
        initial={{ opacity: 0.6 }}
        animate={{ opacity: 1 }}
        transition={motionTransition(reduced)}
      >
        <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
          <Select
            value={selected}
            onValueChange={(next) => {
              setSelected(next as InvoiceStatus)
              setValidationError(null)
            }}
            disabled={mutation.isPending}
          >
            <SelectTrigger
              className="w-full sm:w-[220px]"
              aria-label="Nuevo estado"
              aria-describedby={validationError ? validationId : undefined}
            >
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

          <Button type="submit" disabled={mutation.isPending}>
            {mutation.isPending ? 'Cambiando…' : 'Cambiar Estado'}
          </Button>
        </div>

        {validationError ? (
          <p id={validationId} className="text-sm text-destructive">
            {validationError}
          </p>
        ) : null}

        {mutation.isError ? (
          <p role="alert" className="text-sm text-destructive">
            No se pudo cambiar el estado. {(mutation.error as Error).message}
          </p>
        ) : null}
      </m.form>
    </LazyMotion>
  )
}
