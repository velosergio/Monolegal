import { useReducer } from 'react'
import { useToast } from '@/components/feedback/useToast'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { cn } from '@/lib/utils'
import {
  useEmailSettings,
  useUpdateEmailSettings,
  useValidateEmailCredentials,
} from '../api/useEmailSettings'
import type { CredentialStatus, EmailProvider, EmailSettings, EmailSettingsInput } from '../types'

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/

const CREDENTIAL_BADGE: Record<
  CredentialStatus,
  { label: string; variant: 'default' | 'secondary' | 'destructive' | 'outline' }
> = {
  validated: { label: 'Validada', variant: 'default' },
  configured: { label: 'Configurada', variant: 'secondary' },
  invalid: { label: 'Inválida', variant: 'destructive' },
  notconfigured: { label: 'No configurada', variant: 'outline' },
}

/** Estado editable del formulario del proveedor de email. */
type FormState = {
  provider: EmailProvider
  fromAddress: string
  fromName: string
  smtpHost: string
  smtpPort: string
  smtpUsername: string
  smtpUseStartTls: boolean
  resendFromDomain: string
}

/** Construye el estado inicial del formulario a partir de la configuración cargada. */
function initFormState(initial: EmailSettings): FormState {
  return {
    provider: initial.activeProvider,
    fromAddress: initial.fromAddress,
    fromName: initial.fromName,
    smtpHost: initial.smtp.host ?? '',
    smtpPort: String(initial.smtp.port ?? 587),
    smtpUsername: initial.smtp.username ?? '',
    smtpUseStartTls: initial.smtp.useStartTls,
    resendFromDomain: initial.resend.fromDomain ?? '',
  }
}

function formReducer(state: FormState, patch: Partial<FormState>): FormState {
  return { ...state, ...patch }
}

/** Validación de cliente del formulario; pura (sin estado local), por eso vive a nivel de módulo. */
function validateClient(input: EmailSettingsInput): string | null {
  if (!EMAIL_RE.test(input.fromAddress)) return 'Introduce un correo remitente válido.'
  if (input.fromName.length === 0) return 'El nombre del remitente es obligatorio.'
  if (input.activeProvider === 'smtp') {
    if (input.smtp.host.length === 0) return 'El host SMTP es obligatorio.'
    if (input.smtp.port < 1 || input.smtp.port > 65535)
      return 'El puerto SMTP debe estar entre 1 y 65535.'
  }
  if (input.activeProvider === 'resend' && input.resend.fromDomain.length === 0) {
    return 'El dominio remitente de Resend es obligatorio.'
  }
  return null
}

/** Sección de configuración del proveedor de email (spec 017, US1). */
export function EmailProviderSection() {
  const { data, isLoading, isError, error } = useEmailSettings()

  return (
    <div className="rounded-lg border p-5">
      <div className="flex flex-col gap-1">
        <h2 className="font-heading text-base font-bold">Proveedor de email</h2>
        <p className="text-sm text-muted-foreground">
          Elige el proveedor de envío, configura el remitente y valida la credencial. Los secretos
          (contraseña / API key) se gestionan por variables de entorno.
        </p>
      </div>

      {isLoading && (
        <p className="mt-4 text-sm text-muted-foreground" role="status">
          Cargando configuración…
        </p>
      )}

      {isError && (
        <p className="mt-4 text-sm text-destructive" role="alert">
          {error instanceof Error ? error.message : 'No se pudo cargar la configuración.'}
        </p>
      )}

      {data && <EmailProviderForm key={data.activeProvider} initial={data} />}
    </div>
  )
}

function EmailProviderForm({ initial }: { initial: EmailSettings }) {
  const toast = useToast()
  const update = useUpdateEmailSettings()
  const validate = useValidateEmailCredentials()

  const [form, dispatch] = useReducer(formReducer, initial, initFormState)

  const badge = CREDENTIAL_BADGE[initial.credentialStatus]
  const busy = update.isPending || validate.isPending

  function buildInput(): EmailSettingsInput {
    return {
      activeProvider: form.provider,
      fromAddress: form.fromAddress.trim(),
      fromName: form.fromName.trim(),
      smtp: {
        host: form.smtpHost.trim(),
        port: Number.parseInt(form.smtpPort, 10) || 0,
        username: form.smtpUsername.trim(),
        useStartTls: form.smtpUseStartTls,
      },
      resend: { fromDomain: form.resendFromDomain.trim() },
    }
  }

  function handleSave() {
    const input = buildInput()
    const validationError = validateClient(input)
    if (validationError) {
      toast.error(validationError)
      return
    }
    update.mutate(input, {
      onSuccess: () => toast.success('Configuración guardada.'),
      onError: (err) => toast.error(err instanceof Error ? err.message : 'No se pudo guardar.'),
    })
  }

  function handleValidate() {
    validate.mutate(form.provider, {
      onSuccess: (result) => {
        if (result.status === 'validated') {
          toast.success(result.message ?? 'Credencial validada correctamente.')
        } else {
          toast.error(result.message ?? 'La credencial no es válida.')
        }
      },
      onError: (err) => toast.error(err instanceof Error ? err.message : 'No se pudo validar.'),
    })
  }

  return (
    <form
      noValidate
      className="mt-4 flex flex-col gap-4"
      onSubmit={(event) => {
        event.preventDefault()
        handleSave()
      }}
    >
      <div className="flex flex-col gap-1.5">
        <label htmlFor="email-provider" className="text-sm font-medium">
          Proveedor activo
        </label>
        <Select
          value={form.provider}
          onValueChange={(value) => dispatch({ provider: value as EmailProvider })}
        >
          <SelectTrigger id="email-provider" className="sm:max-w-xs">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="smtp">SMTP</SelectItem>
            <SelectItem value="resend">Resend</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="grid gap-4 sm:grid-cols-2">
        <Field id="from-address" label="Correo remitente">
          <Input
            id="from-address"
            type="email"
            value={form.fromAddress}
            autoComplete="email"
            onChange={(e) => dispatch({ fromAddress: e.target.value })}
          />
        </Field>
        <Field id="from-name" label="Nombre remitente">
          <Input
            id="from-name"
            value={form.fromName}
            onChange={(e) => dispatch({ fromName: e.target.value })}
          />
        </Field>
      </div>

      {form.provider === 'smtp' && (
        <fieldset className="grid gap-4 border-0 p-0 sm:grid-cols-2">
          <legend className="sr-only">Parámetros SMTP</legend>
          <Field id="smtp-host" label="Host SMTP">
            <Input
              id="smtp-host"
              value={form.smtpHost}
              onChange={(e) => dispatch({ smtpHost: e.target.value })}
            />
          </Field>
          <Field id="smtp-port" label="Puerto">
            <Input
              id="smtp-port"
              type="number"
              inputMode="numeric"
              value={form.smtpPort}
              onChange={(e) => dispatch({ smtpPort: e.target.value })}
            />
          </Field>
          <Field id="smtp-username" label="Usuario">
            <Input
              id="smtp-username"
              value={form.smtpUsername}
              autoComplete="username"
              onChange={(e) => dispatch({ smtpUsername: e.target.value })}
            />
          </Field>
          <label className="flex items-center gap-2 self-end text-sm font-medium">
            <input
              type="checkbox"
              checked={form.smtpUseStartTls}
              onChange={(e) => dispatch({ smtpUseStartTls: e.target.checked })}
              className="h-4 w-4 rounded border-input"
            />
            Usar STARTTLS
          </label>
        </fieldset>
      )}

      {form.provider === 'resend' && (
        <Field id="resend-domain" label="Dominio remitente (Resend)">
          <Input
            id="resend-domain"
            value={form.resendFromDomain}
            onChange={(e) => dispatch({ resendFromDomain: e.target.value })}
          />
        </Field>
      )}

      <div className="flex flex-wrap items-center gap-3">
        <span className="text-sm text-muted-foreground">Estado de la credencial:</span>
        <Badge variant={badge.variant}>{badge.label}</Badge>
      </div>

      <div className="flex flex-wrap gap-2">
        <Button type="submit" disabled={busy}>
          {update.isPending ? 'Guardando…' : 'Guardar'}
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={busy}
          onClick={handleValidate}
          className={cn(validate.isPending && 'opacity-80')}
        >
          {validate.isPending ? 'Validando…' : 'Validar credencial'}
        </Button>
      </div>
    </form>
  )
}

function Field({ id, label, children }: { id: string; label: string; children: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-medium">
        {label}
      </label>
      {children}
    </div>
  )
}
