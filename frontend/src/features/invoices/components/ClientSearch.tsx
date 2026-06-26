import { Search } from 'lucide-react'
import { useId } from 'react'
import { Input } from '@/components/ui/input'

interface ClientSearchProps {
  /** Texto actual del campo (controlado por el contenedor). */
  value: string
  /** Notifica cada cambio del input; el debounce vive en el contenedor. */
  onChange: (value: string) => void
}

/**
 * Campo de búsqueda global por cliente. Es un input controlado: reporta cada
 * cambio al contenedor, que estabiliza el valor (debounce) antes de consultar.
 */
export function ClientSearch({ value, onChange }: ClientSearchProps) {
  const inputId = useId()

  return (
    <div className="relative w-full sm:w-[280px]">
      <Search
        className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
        aria-hidden="true"
      />
      <Input
        id={inputId}
        type="search"
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder="Buscar por cliente…"
        aria-label="Buscar por cliente"
        className="pl-9"
      />
    </div>
  )
}
