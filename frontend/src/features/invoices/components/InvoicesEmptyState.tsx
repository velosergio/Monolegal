import { FileSearch } from 'lucide-react'

/**
 * Estado vacío del listado: sin facturas que coincidan con los filtros actuales.
 */
export function InvoicesEmptyState() {
  return (
    <output className="flex flex-col items-center justify-center gap-3 rounded-lg border border-dashed py-16 text-center">
      <FileSearch className="h-10 w-10 text-muted-foreground" aria-hidden="true" />
      <span>
        <span className="block font-medium text-foreground">No se encontraron facturas</span>
        <span className="mt-1 block text-sm text-muted-foreground">
          Ajusta los filtros o el término de búsqueda e inténtalo de nuevo.
        </span>
      </span>
    </output>
  )
}
