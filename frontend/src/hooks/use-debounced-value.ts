import { useEffect, useState } from 'react'

/**
 * Devuelve `value` estabilizado: solo se actualiza cuando han pasado
 * `delayMs` sin nuevos cambios. Útil para la búsqueda por cliente, de modo
 * que no se dispare una petición por cada pulsación.
 */
export function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState<T>(value)

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs)
    return () => clearTimeout(timer)
  }, [value, delayMs])

  return debounced
}
