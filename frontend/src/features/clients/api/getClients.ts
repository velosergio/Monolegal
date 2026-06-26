import type { Client, PagedClients } from '../types'

/** Parámetros de consulta del listado de clientes. */
export interface GetClientsParams {
  search: string
  page: number
  pageSize: number
}

interface PagedResponse {
  data: Client[]
  total: number
  pageSize: number
}

function buildQuery({ search, page, pageSize }: GetClientsParams): string {
  const params = new URLSearchParams()
  const trimmed = search.trim()
  if (trimmed.length > 0) params.set('search', trimmed)
  params.set('page', String(page))
  params.set('pageSize', String(pageSize))
  return params.toString()
}

/**
 * Obtiene una página de clientes desde `GET /api/clients`. Lanza un `Error` con
 * mensaje legible ante respuestas no satisfactorias.
 */
export async function getClients(
  params: GetClientsParams,
  signal?: AbortSignal
): Promise<PagedClients> {
  const response = await fetch(`/api/clients?${buildQuery(params)}`, { signal })

  if (!response.ok) {
    throw new Error(`No se pudieron cargar los clientes (${response.status}).`)
  }

  const body = (await response.json()) as PagedResponse
  return { data: body.data, total: body.total, pageSize: body.pageSize }
}
