import { useState } from 'react'
import { useToast } from '@/components/feedback/useToast'
import { Badge } from '@/components/ui/badge'
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { cn } from '@/lib/utils'
import {
  useEmailTemplates,
  usePreviewEmailTemplate,
  useResetEmailTemplate,
  useUpdateEmailTemplate,
} from '../api/useEmailTemplates'
import type { EmailTemplate, NotificationType } from '../types'

const TYPE_LABELS: Record<NotificationType, string> = {
  reminder: 'Recordatorio',
  paymentconfirmation: 'Confirmación de pago',
  deactivationnotice: 'Aviso de desactivación',
}

const VARIABLE_RE = /\{\{\s*([\w.]+)\s*\}\}/g

/** Sección de gestión de plantillas de email (spec 017, US2). */
export function EmailTemplatesSection() {
  const { data, isLoading, isError, error } = useEmailTemplates()
  const [selectedType, setSelectedType] = useState<NotificationType>('reminder')

  const selected = data?.templates.find((t) => t.type === selectedType)

  return (
    <div className="rounded-lg border p-5">
      <div className="flex flex-col gap-1">
        <h2 className="font-heading text-base font-bold">Plantillas de email</h2>
        <p className="text-sm text-muted-foreground">
          Edita el asunto y el cuerpo de cada notificación. Usa solo las variables admitidas.
        </p>
      </div>

      {isLoading && (
        <p className="mt-4 text-sm text-muted-foreground" role="status">
          Cargando plantillas…
        </p>
      )}

      {isError && (
        <p className="mt-4 text-sm text-destructive" role="alert">
          {error instanceof Error ? error.message : 'No se pudieron cargar las plantillas.'}
        </p>
      )}

      {data && selected && (
        <div className="mt-4 flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <label htmlFor="template-type" className="text-sm font-medium">
              Plantilla
            </label>
            <Select
              value={selectedType}
              onValueChange={(value) => setSelectedType(value as NotificationType)}
            >
              <SelectTrigger id="template-type" className="sm:max-w-xs">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {data.templates.map((t) => (
                  <SelectItem key={t.type} value={t.type}>
                    {TYPE_LABELS[t.type]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <TemplateEditor
            key={selected.type}
            template={selected}
            allowedVariables={data.allowedVariables}
          />
        </div>
      )}
    </div>
  )
}

function TemplateEditor({
  template,
  allowedVariables,
}: {
  template: EmailTemplate
  allowedVariables: string[]
}) {
  const toast = useToast()
  const update = useUpdateEmailTemplate()
  const reset = useResetEmailTemplate()
  const preview = usePreviewEmailTemplate()

  const [subject, setSubject] = useState(() => template.subject)
  const [body, setBody] = useState(() => template.body)
  const [previewResult, setPreviewResult] = useState<{ subject: string; body: string } | null>(null)
  const [confirmOpen, setConfirmOpen] = useState(false)

  const allowed = new Set(allowedVariables)
  const busy = update.isPending || reset.isPending || preview.isPending

  function findInvalidVariables(text: string): string[] {
    const invalid: string[] = []
    const seen = new Set<string>()
    for (const match of text.matchAll(VARIABLE_RE)) {
      const name = match[1]
      if (!allowed.has(name) && !seen.has(name)) {
        seen.add(name)
        invalid.push(name)
      }
    }
    return invalid
  }

  function validateClient(): string | null {
    if (subject.trim().length === 0) return 'El asunto es obligatorio.'
    if (body.trim().length === 0) return 'El cuerpo es obligatorio.'
    const invalid = [...findInvalidVariables(subject), ...findInvalidVariables(body)]
    if (invalid.length > 0)
      return `Variable no admitida: ${invalid.map((v) => `{{${v}}}`).join(', ')}.`
    return null
  }

  function insertVariable(variable: string) {
    setBody((current) => `${current}{{${variable}}}`)
  }

  function handleSave() {
    const validationError = validateClient()
    if (validationError) {
      toast.error(validationError)
      return
    }
    update.mutate(
      { type: template.type, content: { subject: subject.trim(), body } },
      {
        onSuccess: () => toast.success('Plantilla guardada.'),
        onError: (err) => toast.error(err instanceof Error ? err.message : 'No se pudo guardar.'),
      }
    )
  }

  function handlePreview() {
    const validationError = validateClient()
    if (validationError) {
      toast.error(validationError)
      return
    }
    preview.mutate(
      { type: template.type, content: { subject: subject.trim(), body } },
      {
        onSuccess: (result) => setPreviewResult(result),
        onError: (err) =>
          toast.error(err instanceof Error ? err.message : 'No se pudo generar la vista previa.'),
      }
    )
  }

  function handleReset() {
    reset.mutate(template.type, {
      onSuccess: () => {
        toast.success('Plantilla restablecida al contenido por defecto.')
        setConfirmOpen(false)
      },
      onError: (err) => toast.error(err instanceof Error ? err.message : 'No se pudo restablecer.'),
    })
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center gap-2">
        <Badge variant={template.isCustomized ? 'default' : 'outline'}>
          {template.isCustomized ? 'Personalizada' : 'Por defecto'}
        </Badge>
      </div>

      <div className="flex flex-col gap-1.5">
        <label htmlFor="template-subject" className="text-sm font-medium">
          Asunto
        </label>
        <Input id="template-subject" value={subject} onChange={(e) => setSubject(e.target.value)} />
      </div>

      <div className="flex flex-col gap-1.5">
        <label htmlFor="template-body" className="text-sm font-medium">
          Cuerpo
        </label>
        <textarea
          id="template-body"
          value={body}
          onChange={(e) => setBody(e.target.value)}
          rows={6}
          className="flex w-full rounded-[2px] border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
        />
      </div>

      <fieldset className="flex flex-col gap-2 border-0 p-0">
        <legend className="text-sm font-medium">Variables admitidas</legend>
        <div className="flex flex-wrap gap-2">
          {allowedVariables.map((variable) => (
            <button
              key={variable}
              type="button"
              onClick={() => insertVariable(variable)}
              className="rounded-[2px] border border-input px-2 py-1 font-mono text-xs text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
            >
              {`{{${variable}}}`}
            </button>
          ))}
        </div>
      </fieldset>

      {previewResult && (
        <div className="rounded-[2px] border border-dashed p-3">
          <p className="text-xs font-semibold uppercase text-muted-foreground">Vista previa</p>
          <p className="mt-1 text-sm font-medium">{previewResult.subject}</p>
          <p className="mt-1 whitespace-pre-wrap text-sm text-muted-foreground">
            {previewResult.body}
          </p>
        </div>
      )}

      <div className="flex flex-wrap gap-2">
        <Button type="button" disabled={busy} onClick={handleSave}>
          {update.isPending ? 'Guardando…' : 'Guardar'}
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={busy}
          onClick={handlePreview}
          className={cn(preview.isPending && 'opacity-80')}
        >
          {preview.isPending ? 'Generando…' : 'Vista previa'}
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={busy}
          onClick={() => setConfirmOpen(true)}
        >
          Restablecer por defecto
        </Button>
      </div>

      <Dialog open={confirmOpen} onOpenChange={setConfirmOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Restablecer la plantilla</DialogTitle>
            <DialogDescription>
              Se eliminará la personalización y se volverá al contenido por defecto. Esta acción no
              se puede deshacer.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline">
                Cancelar
              </Button>
            </DialogClose>
            <Button
              type="button"
              variant="destructive"
              disabled={reset.isPending}
              onClick={handleReset}
            >
              {reset.isPending ? 'Restableciendo…' : 'Restablecer'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
