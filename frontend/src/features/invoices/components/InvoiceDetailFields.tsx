import type { InvoiceDetail } from '../types'
import { formatAmount, formatDate } from '../utils'
import { StatusBadge } from './StatusBadge'

interface InvoiceDetailFieldsProps {
  invoice: InvoiceDetail
}

interface FieldProps {
  label: string
  children: React.ReactNode
}

function Field({ label, children }: FieldProps) {
  return (
    <div className="flex flex-col gap-1">
      <dt className="text-xs font-medium uppercase tracking-wide text-muted-foreground">{label}</dt>
      <dd className="text-sm text-foreground">{children}</dd>
    </div>
  )
}

/**
 * Muestra todos los campos de una factura con formato legible en español (FR-002, FR-003).
 */
export function InvoiceDetailFields({ invoice }: InvoiceDetailFieldsProps) {
  return (
    <dl className="grid grid-cols-1 gap-4 sm:grid-cols-2">
      <Field label="Identificador">
        <span className="break-all font-mono text-xs">{invoice.id}</span>
      </Field>
      <Field label="Cliente">{invoice.clientId}</Field>
      <Field label="Monto">
        <span className="tabular-nums">{formatAmount(invoice.amount)}</span>
      </Field>
      <Field label="Estado">
        <StatusBadge status={invoice.status} />
      </Field>
      <Field label="Creada">{formatDate(invoice.createdAt)}</Field>
      <Field label="Última actualización">{formatDate(invoice.updatedAt)}</Field>
      <Field label="Recordatorios enviados">{invoice.remindersCount}</Field>
      <Field label="Último recordatorio">
        {invoice.lastReminderSentAt ? formatDate(invoice.lastReminderSentAt) : '—'}
      </Field>
      <Field label="Última transición de estado">
        {formatDate(invoice.lastStatusTransitionAt)}
      </Field>
    </dl>
  )
}
