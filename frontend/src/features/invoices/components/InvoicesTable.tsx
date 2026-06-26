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
}

/**
 * Tabla de facturas: ID, Cliente, Monto, Estado, Última Acción y acción "Pagar"
 * (solo para facturas en estado no terminal).
 */
export function InvoicesTable({ invoices }: InvoicesTableProps) {
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
                {shortId(invoice.id)}
              </TableCell>
              <TableCell className="font-medium">{invoice.clientId}</TableCell>
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
                {isTerminal ? (
                  <span className="text-xs text-muted-foreground">—</span>
                ) : (
                  <Button
                    type="button"
                    size="sm"
                    variant="outline"
                    disabled={isPaying}
                    onClick={() => pay.mutate(invoice.id)}
                  >
                    {isPaying ? 'Pagando…' : 'Pagar'}
                  </Button>
                )}
              </TableCell>
            </TableRow>
          )
        })}
      </TableBody>
    </Table>
  )
}
