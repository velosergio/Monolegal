import { FileSearch, MailX } from 'lucide-react'

interface ShipmentsEmptyStateProps {
  /** `true` cuando hay filtros/búsqueda activos: distingue "sin coincidencias" de "no hay envíos". */
  filtered: boolean
}

/**
 * Estado vacío del listado de envíos. Diferencia el caso "no hay envíos" del caso
 * "sin coincidencias con los filtros" (spec 019, FR-004).
 */
export function ShipmentsEmptyState({ filtered }: ShipmentsEmptyStateProps) {
  const Icon = filtered ? FileSearch : MailX
  const title = filtered ? 'No se encontraron envíos' : 'Aún no hay envíos'
  const description = filtered
    ? 'Ajusta los filtros o el término de búsqueda e inténtalo de nuevo.'
    : 'Cuando las facturas en estados notificables generen avisos, aparecerán aquí.'

  return (
    <output className="flex flex-col items-center justify-center gap-3 rounded-lg border border-dashed py-16 text-center">
      <Icon className="h-10 w-10 text-muted-foreground" aria-hidden="true" />
      <span>
        <span className="block font-medium text-foreground">{title}</span>
        <span className="mt-1 block text-sm text-muted-foreground">{description}</span>
      </span>
    </output>
  )
}
