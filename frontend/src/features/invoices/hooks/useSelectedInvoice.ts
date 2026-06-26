import { useCallback } from 'react'
import { useSearchParams } from 'react-router-dom'

/** Nombre del parámetro de búsqueda que refleja la factura seleccionada en la URL. */
const SELECTED_INVOICE_PARAM = 'factura'

export interface SelectedInvoice {
  /** Id de la factura seleccionada, o `null` si no hay ninguna (modal cerrado). */
  selectedId: string | null
  /** Selecciona una factura (abre el modal) reflejándola en `?factura=<id>`. */
  open: (id: string) => void
  /** Limpia la selección (cierra el modal) quitando el parámetro de la URL. */
  close: () => void
}

/**
 * Gestiona la factura seleccionada mediante el search param `?factura=<id>` (spec 015, D4),
 * habilitando deep-linking y navegación por el historial del navegador.
 */
export function useSelectedInvoice(): SelectedInvoice {
  const [searchParams, setSearchParams] = useSearchParams()
  const selectedId = searchParams.get(SELECTED_INVOICE_PARAM)

  const open = useCallback(
    (id: string) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev)
          next.set(SELECTED_INVOICE_PARAM, id)
          return next
        },
        { replace: false }
      )
    },
    [setSearchParams]
  )

  const close = useCallback(() => {
    setSearchParams(
      (prev) => {
        const next = new URLSearchParams(prev)
        next.delete(SELECTED_INVOICE_PARAM)
        return next
      },
      { replace: false }
    )
  }, [setSearchParams])

  return { selectedId, open, close }
}
