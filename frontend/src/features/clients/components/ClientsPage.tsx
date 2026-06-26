import { ChevronLeft, ChevronRight, Plus, Search } from 'lucide-react'
import { useEffect, useId, useReducer, useState } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { useDocumentTitle } from '@/hooks/use-document-title'
import { useClients } from '../api/useClients'
import type { Client } from '../types'
import { ClientFormModal } from './ClientFormModal'
import { ClientsTable } from './ClientsTable'
import { DeleteClientDialog } from './DeleteClientDialog'

const PAGE_SIZE = 10

/**
 * Estado del listado: término en edición (`searchInput`), término confirmado tras el debounce
 * (`search`) y página actual. Están acoplados (confirmar búsqueda vuelve a la página 1), por eso
 * se agrupan en un reducer en lugar de tres useState sueltos.
 */
interface ListState {
  searchInput: string
  search: string
  page: number
}

type ListAction =
  | { type: 'searchInputChanged'; value: string }
  | { type: 'searchCommitted'; value: string }
  | { type: 'pageChanged'; page: number }

const INITIAL_LIST_STATE: ListState = { searchInput: '', search: '', page: 1 }

function listReducer(state: ListState, action: ListAction): ListState {
  switch (action.type) {
    case 'searchInputChanged':
      return { ...state, searchInput: action.value }
    case 'searchCommitted':
      return { ...state, search: action.value, page: 1 }
    case 'pageChanged':
      return { ...state, page: action.page }
    default:
      return state
  }
}

/**
 * Página de Clientes (spec 018, US2): listado paginado con búsqueda, alta/edición/baja con
 * confirmación, toasts y refresco automático vía TanStack Query.
 */
export function ClientsPage() {
  useDocumentTitle('Clientes')
  const searchId = useId()

  const [list, dispatch] = useReducer(listReducer, INITIAL_LIST_STATE)
  const { searchInput, search, page } = list

  // Modo del modal de formulario: cerrado | crear | editar(cliente).
  const [formState, setFormState] = useState<{ open: boolean; client: Client | null }>({
    open: false,
    client: null,
  })
  const [toDelete, setToDelete] = useState<Client | null>(null)

  // Debounce de la búsqueda (estabiliza el valor antes de consultar).
  useEffect(() => {
    const timer = setTimeout(() => dispatch({ type: 'searchCommitted', value: searchInput }), 300)
    return () => clearTimeout(timer)
  }, [searchInput])

  const query = useClients({ search, page, pageSize: PAGE_SIZE })
  const { data, isLoading, isError, isPlaceholderData } = query

  const totalPages = data ? Math.max(1, Math.ceil(data.total / (data.pageSize || PAGE_SIZE))) : 1

  return (
    <section aria-labelledby="clients-title" className="flex flex-col gap-6">
      <header className="flex flex-col gap-1">
        <h1 id="clients-title" className="font-heading text-2xl font-black tracking-tight">
          Clientes
        </h1>
        <p className="text-sm text-muted-foreground">
          Administra el catálogo de clientes asociados a las facturas.
        </p>
      </header>

      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div className="relative w-full sm:w-[280px]">
          <Search
            className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
            aria-hidden="true"
          />
          <Input
            id={searchId}
            type="search"
            value={searchInput}
            onChange={(e) => dispatch({ type: 'searchInputChanged', value: e.target.value })}
            placeholder="Buscar por nombre o email…"
            aria-label="Buscar clientes"
            className="pl-9"
          />
        </div>
        <Button type="button" onClick={() => setFormState({ open: true, client: null })}>
          <Plus className="h-4 w-4" aria-hidden="true" />
          Nuevo cliente
        </Button>
      </div>

      <div className="rounded-lg border">
        {isError ? (
          <div role="alert" className="flex flex-col items-center gap-3 py-16 text-center">
            <p className="font-medium text-foreground">No se pudieron cargar los clientes.</p>
            <Button type="button" variant="outline" onClick={() => query.refetch()}>
              Reintentar
            </Button>
          </div>
        ) : isLoading ? (
          <p className="py-16 text-center text-sm text-muted-foreground">Cargando clientes…</p>
        ) : !data || data.data.length === 0 ? (
          <p className="py-16 text-center text-sm text-muted-foreground">
            No se encontraron clientes.
          </p>
        ) : (
          <div
            className={isPlaceholderData ? 'opacity-60 transition-opacity' : 'transition-opacity'}
          >
            <ClientsTable
              clients={data.data}
              onEdit={(client) => setFormState({ open: true, client })}
              onDelete={setToDelete}
            />
          </div>
        )}
      </div>

      {!isError && data && data.total > 0 ? (
        <nav
          className="flex items-center justify-between gap-3"
          aria-label="Paginación de clientes"
        >
          <span className="text-sm text-muted-foreground">
            Página {page} de {totalPages} · {data.total} clientes
          </span>
          <div className="flex gap-2">
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={page <= 1}
              onClick={() => dispatch({ type: 'pageChanged', page: Math.max(1, page - 1) })}
            >
              <ChevronLeft className="h-4 w-4" aria-hidden="true" />
              Anterior
            </Button>
            <Button
              type="button"
              size="sm"
              variant="outline"
              disabled={page >= totalPages}
              onClick={() => dispatch({ type: 'pageChanged', page: page + 1 })}
            >
              Siguiente
              <ChevronRight className="h-4 w-4" aria-hidden="true" />
            </Button>
          </div>
        </nav>
      ) : null}

      <ClientFormModal
        open={formState.open}
        client={formState.client}
        onClose={() => setFormState({ open: false, client: null })}
      />
      <DeleteClientDialog client={toDelete} onClose={() => setToDelete(null)} />
    </section>
  )
}
