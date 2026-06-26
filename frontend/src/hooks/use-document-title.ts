import { useEffect } from 'react'

const SUFFIX = 'Monolegal'

/**
 * Fija el `document.title` de la vista activa con el formato "<título> ·
 * Monolegal" y lo restaura al desmontar. Permite títulos dinámicos por vista.
 */
export function useDocumentTitle(title: string) {
  useEffect(() => {
    const previous = document.title
    document.title = title ? `${title} · ${SUFFIX}` : SUFFIX
    return () => {
      document.title = previous
    }
  }, [title])
}
