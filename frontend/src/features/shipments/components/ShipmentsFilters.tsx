import { Search } from 'lucide-react'
import { Input } from '@/components/ui/input'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { FILTERABLE_SEND_STATUSES, SEND_STATUS_LABELS, type ServerSendStatus } from '../types'

interface ShipmentsFiltersProps {
  sendStatus: ServerSendStatus | 'all'
  searchInput: string
  onSendStatusChange: (value: ServerSendStatus | 'all') => void
  onSearchChange: (value: string) => void
}

/**
 * Controles de filtro por estado de envío y búsqueda por cliente/correo (spec 019, US3).
 * El filtrado y la búsqueda son server-side (alimentan los parámetros de `useShipments`).
 */
export function ShipmentsFilters({
  sendStatus,
  searchInput,
  onSendStatusChange,
  onSearchChange,
}: ShipmentsFiltersProps) {
  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
      <Select
        value={sendStatus}
        onValueChange={(next) => onSendStatusChange(next as ServerSendStatus | 'all')}
      >
        <SelectTrigger className="w-full sm:w-[200px]" aria-label="Filtrar por estado de envío">
          <SelectValue placeholder="Estado de envío" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="all">Todos los estados</SelectItem>
          {FILTERABLE_SEND_STATUSES.map((status) => (
            <SelectItem key={status} value={status}>
              {SEND_STATUS_LABELS[status]}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <div className="relative w-full sm:w-[280px]">
        <Search
          className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
          aria-hidden="true"
        />
        <Input
          type="search"
          value={searchInput}
          onChange={(event) => onSearchChange(event.target.value)}
          placeholder="Buscar por cliente o correo"
          aria-label="Buscar por cliente o correo"
          className="pl-9"
        />
      </div>
    </div>
  )
}
