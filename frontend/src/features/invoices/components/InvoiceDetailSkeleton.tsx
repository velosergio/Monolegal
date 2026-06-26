import { Skeleton } from '@/components/ui/skeleton'

/** Claves estables para los marcadores del skeleton (evita usar el índice como key). */
const FIELD_PLACEHOLDERS = ['id', 'cliente', 'monto', 'estado', 'creada', 'actualizada'] as const

/**
 * Skeleton del contenido del modal de detalle: replica la forma de la lista de campos
 * mientras se cargan los datos (FR-005).
 */
export function InvoiceDetailSkeleton() {
  return (
    <div className="flex flex-col gap-4" aria-hidden="true">
      <Skeleton className="h-5 w-40" />
      <div className="grid grid-cols-2 gap-4">
        {FIELD_PLACEHOLDERS.map((placeholder) => (
          <div key={placeholder} className="flex flex-col gap-2">
            <Skeleton className="h-3 w-20" />
            <Skeleton className="h-4 w-28" />
          </div>
        ))}
      </div>
    </div>
  )
}
