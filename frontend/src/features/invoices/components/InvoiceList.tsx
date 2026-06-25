import React from 'react'
import { Button } from '@/components/ui/button'
import { usePayInvoice } from '../api/payInvoice'
import {
  Invoice,
  InvoiceStatus,
  INVOICE_STATUS_LABELS,
  TERMINAL_STATUSES,
} from '../types'

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Muestra solo los primeros 8 caracteres del ID para no saturar la columna.
 */
function shortId(id: string): string {
  return id.slice(0, 8).toUpperCase()
}

/**
 * Formatea una cadena ISO-8601 como fecha y hora local legible.
 */
function formatDate(iso: string): string {
  return new Date(iso).toLocaleString('es-MX', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  })
}

/**
 * Formatea un número como moneda (MXN).
 */
function formatAmount(amount: number): string {
  return new Intl.NumberFormat('es-MX', {
    style: 'currency',
    currency: 'MXN',
  }).format(amount)
}

/**
 * Devuelve la etiqueta legible de un estado, o el valor numérico como
 * respaldo si el estado no está mapeado (compatibilidad futura).
 */
function statusLabel(status: InvoiceStatus): string {
  return INVOICE_STATUS_LABELS[status] ?? String(status)
}

// ---------------------------------------------------------------------------
// PayButton — botón aislado por factura para manejar estado de carga
// ---------------------------------------------------------------------------

interface PayButtonProps {
  invoiceId: string
}

const PayButton: React.FC<PayButtonProps> = ({ invoiceId }) => {
  const { mutate, isPending, isError, error } = usePayInvoice()

  return (
    <div className="flex flex-col items-start gap-1">
      <Button
        size="sm"
        disabled={isPending}
        onClick={() => mutate(invoiceId)}
      >
        {isPending ? 'Procesando…' : 'Pagar'}
      </Button>
      {isError && (
        <p className="text-xs text-red-600">
          {error instanceof Error ? error.message : 'Error al pagar la factura'}
        </p>
      )}
    </div>
  )
}

// ---------------------------------------------------------------------------
// InvoiceList
// ---------------------------------------------------------------------------

export interface InvoiceListProps {
  invoices: Invoice[]
}

/**
 * Muestra una tabla con las facturas recibidas.
 *
 * - Columnas: ID parcial, cliente, monto, estado, última transición, acción.
 * - El botón "Pagar" solo aparece cuando la factura está en un estado NO
 *   terminal (es decir, distinto de Pagado, Desactivado y Cancelled).
 *
 * Validates: FR-004 | US2 (spec.md 006-invoice-status-transitions)
 */
export const InvoiceList: React.FC<InvoiceListProps> = ({ invoices }) => {
  if (invoices.length === 0) {
    return (
      <div className="p-4 text-center text-gray-500 dark:text-gray-400">
        No hay facturas para mostrar.
      </div>
    )
  }

  return (
    <div className="overflow-x-auto rounded shadow">
      <table className="w-full border-collapse bg-white text-sm dark:bg-gray-800">
        <thead>
          <tr className="border-b bg-gray-50 text-left text-xs font-semibold uppercase tracking-wide text-gray-600 dark:bg-gray-700 dark:text-gray-300">
            <th className="px-4 py-3">ID</th>
            <th className="px-4 py-3">Cliente</th>
            <th className="px-4 py-3 text-right">Monto</th>
            <th className="px-4 py-3">Estado</th>
            <th className="px-4 py-3">Última transición</th>
            <th className="px-4 py-3">Acción</th>
          </tr>
        </thead>
        <tbody>
          {invoices.map((invoice) => {
            const isTerminal = TERMINAL_STATUSES.has(invoice.status)

            return (
              <tr
                key={invoice.id}
                className="border-b last:border-0 hover:bg-gray-50 dark:hover:bg-gray-700"
              >
                <td className="px-4 py-3 font-mono text-xs text-gray-700 dark:text-gray-300">
                  {shortId(invoice.id)}
                </td>
                <td className="px-4 py-3 text-gray-800 dark:text-gray-200">
                  {invoice.clientId}
                </td>
                <td className="px-4 py-3 text-right font-medium text-gray-800 dark:text-gray-200">
                  {formatAmount(invoice.amount)}
                </td>
                <td className="px-4 py-3">
                  <StatusBadge status={invoice.status} />
                </td>
                <td className="px-4 py-3 text-gray-600 dark:text-gray-400">
                  {formatDate(invoice.lastStatusTransitionAt)}
                </td>
                <td className="px-4 py-3">
                  {!isTerminal && <PayButton invoiceId={invoice.id} />}
                </td>
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}

// ---------------------------------------------------------------------------
// StatusBadge — chip de color por estado
// ---------------------------------------------------------------------------

const STATUS_BADGE_CLASSES: Partial<Record<InvoiceStatus, string>> = {
  [InvoiceStatus.Pagado]: 'bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300',
  [InvoiceStatus.Pending]: 'bg-yellow-100 text-yellow-700 dark:bg-yellow-900 dark:text-yellow-300',
  [InvoiceStatus.PrimerRecordatorio]:
    'bg-orange-100 text-orange-700 dark:bg-orange-900 dark:text-orange-300',
  [InvoiceStatus.SegundoRecordatorio]:
    'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300',
  [InvoiceStatus.Desactivado]:
    'bg-gray-100 text-gray-500 dark:bg-gray-700 dark:text-gray-400',
  [InvoiceStatus.Cancelled]:
    'bg-gray-100 text-gray-500 dark:bg-gray-700 dark:text-gray-400',
  [InvoiceStatus.Draft]: 'bg-blue-100 text-blue-700 dark:bg-blue-900 dark:text-blue-300',
  [InvoiceStatus.Overdue]: 'bg-red-100 text-red-700 dark:bg-red-900 dark:text-red-300',
}

interface StatusBadgeProps {
  status: InvoiceStatus
}

const StatusBadge: React.FC<StatusBadgeProps> = ({ status }) => {
  const classes =
    STATUS_BADGE_CLASSES[status] ??
    'bg-gray-100 text-gray-600 dark:bg-gray-700 dark:text-gray-300'

  return (
    <span className={`inline-block rounded-full px-2 py-0.5 text-xs font-medium ${classes}`}>
      {statusLabel(status)}
    </span>
  )
}
