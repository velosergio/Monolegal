import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'

interface InvoicesPaginationProps {
  page: number
  pageSize: number
  total: number
  onPageChange: (page: number) => void
}

/**
 * Controles de paginación: rango mostrado y navegación anterior/siguiente.
 * Deshabilita los extremos cuando no hay más páginas.
 */
export function InvoicesPagination({
  page,
  pageSize,
  total,
  onPageChange,
}: InvoicesPaginationProps) {
  const totalPages = Math.max(1, Math.ceil(total / pageSize))
  const from = total === 0 ? 0 : (page - 1) * pageSize + 1
  const to = Math.min(page * pageSize, total)
  const canPrev = page > 1
  const canNext = page < totalPages

  return (
    <nav
      className="flex flex-col items-center justify-between gap-3 sm:flex-row"
      aria-label="Paginación de facturas"
    >
      <p className="text-sm text-muted-foreground" aria-live="polite">
        Mostrando {from}–{to} de {total}
      </p>
      <div className="flex items-center gap-2">
        <Button
          type="button"
          variant="outline"
          size="sm"
          disabled={!canPrev}
          onClick={() => onPageChange(page - 1)}
        >
          <ChevronLeft className="h-4 w-4" aria-hidden="true" />
          Anterior
        </Button>
        <span className="text-sm text-muted-foreground">
          Página {page} de {totalPages}
        </span>
        <Button
          type="button"
          variant="outline"
          size="sm"
          disabled={!canNext}
          onClick={() => onPageChange(page + 1)}
        >
          Siguiente
          <ChevronRight className="h-4 w-4" aria-hidden="true" />
        </Button>
      </div>
    </nav>
  )
}
