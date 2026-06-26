import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { FILTERABLE_STATUSES, INVOICE_STATUS_LABELS, type InvoiceStatus } from '../types'

interface StatusFilterProps {
  value: InvoiceStatus | 'all'
  onChange: (value: InvoiceStatus | 'all') => void
}

/**
 * Filtro por estado de factura (server-side). La opción "Todos" elimina el filtro.
 */
export function StatusFilter({ value, onChange }: StatusFilterProps) {
  return (
    <Select value={value} onValueChange={(next) => onChange(next as InvoiceStatus | 'all')}>
      <SelectTrigger className="w-full sm:w-[200px]" aria-label="Filtrar por estado">
        <SelectValue placeholder="Estado" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="all">Todos los estados</SelectItem>
        {FILTERABLE_STATUSES.map((status) => (
          <SelectItem key={status} value={status}>
            {INVOICE_STATUS_LABELS[status]}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
