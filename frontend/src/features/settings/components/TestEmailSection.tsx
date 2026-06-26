import { useState } from 'react'
import { useToast } from '@/components/feedback/useToast'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { useSendTestEmail } from '../api/useSendTestEmail'
import type { NotificationType } from '../types'

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

const TYPE_OPTIONS: ReadonlyArray<{ value: NotificationType; label: string }> = [
  { value: 'reminder', label: 'Recordatorio' },
  { value: 'paymentconfirmation', label: 'Confirmación de pago' },
  { value: 'deactivationnotice', label: 'Aviso de desactivación' },
]

/** Sección de envío de correo de prueba (spec 017, US3). */
export function TestEmailSection() {
  const toast = useToast()
  const send = useSendTestEmail()
  const [to, setTo] = useState('')
  const [templateType, setTemplateType] = useState<NotificationType>('reminder')

  function handleSubmit() {
    if (!EMAIL_RE.test(to.trim())) {
      toast.error('Introduce un correo de destino válido.')
      return
    }
    send.mutate(
      { to: to.trim(), templateType },
      {
        onSuccess: (result) => {
          if (result.result === 'sent') {
            toast.success(`Correo de prueba enviado a ${result.to}.`)
          } else {
            toast.error(result.message ?? 'No se pudo enviar el correo de prueba.')
          }
        },
        onError: (err) =>
          toast.error(
            err instanceof Error ? err.message : 'No se pudo enviar el correo de prueba.'
          ),
      }
    )
  }

  return (
    <div className="rounded-lg border p-5">
      <div className="flex flex-col gap-1">
        <h2 className="font-heading text-base font-bold">Prueba de envío</h2>
        <p className="text-sm text-muted-foreground">
          Envía un correo de prueba con el proveedor y la plantilla reales para verificar la
          configuración.
        </p>
      </div>

      <form
        noValidate
        className="mt-4 flex flex-col gap-4"
        onSubmit={(event) => {
          event.preventDefault()
          handleSubmit()
        }}
      >
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="flex flex-col gap-1.5">
            <label htmlFor="test-to" className="text-sm font-medium">
              Correo de destino
            </label>
            <Input
              id="test-to"
              type="email"
              value={to}
              autoComplete="email"
              onChange={(e) => setTo(e.target.value)}
            />
          </div>
          <div className="flex flex-col gap-1.5">
            <label htmlFor="test-template" className="text-sm font-medium">
              Plantilla
            </label>
            <Select
              value={templateType}
              onValueChange={(value) => setTemplateType(value as NotificationType)}
            >
              <SelectTrigger id="test-template">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {TYPE_OPTIONS.map((option) => (
                  <SelectItem key={option.value} value={option.value}>
                    {option.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>

        <div>
          <Button type="submit" disabled={send.isPending}>
            {send.isPending ? 'Enviando…' : 'Enviar prueba'}
          </Button>
        </div>
      </form>
    </div>
  )
}
