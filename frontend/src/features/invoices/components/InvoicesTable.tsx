import { Pencil, Trash2 } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { usePayInvoice } from '../api/payInvoice'
import { type Invoice, TERMINAL_STATUSES } from '../types'
import { formatAmount, formatDate, shortId } from '../utils'
import { StatusBadge } from './StatusBadge'

interface InvoicesTableProps {
  invoices: Invoice[]
  /** Abre el detalle de una factura (modal). Cuando se omite, el ID se muestra como texto. */
  onSelectInvoice?: (id: string) => void
  /** Abre el formulario de edición de una factura (spec 018). Deshabilitado en estado terminal. */
  onEditInvoice?: (id: string) => void
  /** Abre la confirmación de eliminación de una factura (spec 018). */
  onDeleteInvoice?: (id: string) => void
}

/**
 * Tabla de facturas: ID, Cliente, Monto, Estado, Última Acción y acción "Pagar"
 * (solo para facturas en estado no terminal). El ID es accionable (abre el detalle).
 */
export function InvoicesTable({
  invoices,
  onSelectInvoice,
  onEditInvoice,
  onDeleteInvoice,
}: InvoicesTableProps) {
  const pay = usePayInvoice()

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead className="w-[120px]">ID</TableHead>
          <TableHead>Cliente</TableHead>
          <TableHead className="text-right">Monto</TableHead>
          <TableHead>Estado</TableHead>
          <TableHead>Última Acción</TableHead>
          <TableHead className="text-right">Acciones</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {invoices.map((invoice) => {
          const isTerminal = TERMINAL_STATUSES.has(invoice.status)
          const isPaying = pay.isPending && pay.variables === invoice.id

          return (
            <TableRow key={invoice.id}>
              <TableCell className="font-mono text-xs" title={invoice.id}>
                {onSelectInvoice ? (
                  <button
                    type="button"
                    className="rounded-sm text-left underline-offset-2 hover:underline focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
                    aria-label={`Ver detalle de la factura de ${invoice.clientName}`}
                    onClick={() => onSelectInvoice(invoice.id)}
                  >
                    {shortId(invoice.id)}
                  </button>
                ) : (
                  shortId(invoice.id)
                )}
              </TableCell>
              <TableCell className="font-medium" title={invoice.clientId}>
                {invoice.clientName}
              </TableCell>
              <TableCell className="text-right tabular-nums">
                {formatAmount(invoice.amount)}
              </TableCell>
              <TableCell>
                <StatusBadge status={invoice.status} />
              </TableCell>
              <TableCell className="text-muted-foreground">
                {formatDate(invoice.lastStatusTransitionAt)}
              </TableCell>
              <TableCell className="text-right">
                <div className="flex justify-end gap-2">
                  {!isTerminal ? (
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      disabled={isPaying}
                      onClick={() => pay.mutate(invoice.id)}
                    >
                      {isPaying ? 'Pagando…' : 'Pagar'}
                    </Button>
                  ) : null}
                  {onEditInvoice && !isTerminal ? (
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      onClick={() => onEditInvoice(invoice.id)}
                      aria-label={`Editar la factura ${shortId(invoice.id)}`}
                    >
                      <Pencil className="h-4 w-4" aria-hidden="true" />
                    </Button>
                  ) : null}
                  {onDeleteInvoice ? (
                    <Button
                      type="button"
                      size="sm"
                      variant="outline"
                      onClick={() => onDeleteInvoice(invoice.id)}
                      aria-label={`Eliminar la factura ${shortId(invoice.id)}`}
                    >
                      <Trash2 className="h-4 w-4" aria-hidden="true" />
                    </Button>
                  ) : null}
                </div>
              </TableCell>
            </TableRow>
          )
        })}
      </TableBody>
    </Table>
  )
}
