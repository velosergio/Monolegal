import { Skeleton } from '@/components/ui/skeleton'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'

interface ShipmentsTableSkeletonProps {
  rows?: number
}

/**
 * Esqueleto de carga con la misma estructura de columnas que `ShipmentsTable`,
 * para evitar saltos de layout (CLS) al resolver los datos.
 */
export function ShipmentsTableSkeleton({ rows = 10 }: ShipmentsTableSkeletonProps) {
  const rowKeys = Array.from({ length: rows }, (_, index) => `shipment-skeleton-${index}`)

  return (
    <Table aria-hidden="true">
      <TableHeader>
        <TableRow>
          <TableHead className="w-[120px]">ID</TableHead>
          <TableHead>Cliente</TableHead>
          <TableHead>Email</TableHead>
          <TableHead>Estado de envío</TableHead>
          <TableHead>Último intento</TableHead>
          <TableHead className="text-right">Reintentos</TableHead>
          <TableHead className="text-right">Acciones</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {rowKeys.map((rowKey) => (
          <TableRow key={rowKey}>
            <TableCell>
              <Skeleton className="h-4 w-20" />
            </TableCell>
            <TableCell>
              <Skeleton className="h-4 w-32" />
            </TableCell>
            <TableCell>
              <Skeleton className="h-4 w-40" />
            </TableCell>
            <TableCell>
              <Skeleton className="h-5 w-24 rounded-full" />
            </TableCell>
            <TableCell>
              <Skeleton className="h-4 w-28" />
            </TableCell>
            <TableCell className="text-right">
              <Skeleton className="ml-auto h-4 w-8" />
            </TableCell>
            <TableCell className="text-right">
              <Skeleton className="ml-auto h-8 w-28" />
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
