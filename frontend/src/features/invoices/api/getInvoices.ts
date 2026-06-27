import type { Invoice, InvoiceStatus, PagedInvoices } from '../types'

/**
 * Parámetros de consulta del listado. `status` `'all'` significa sin filtro.
 */
export interface GetInvoicesParams {
  status: InvoiceStatus | 'all'
  search: string
  page: number
  pageSize: number
}

interface InvoiceListItemResponse {
  id: string
  clientId: string
  clientName: string
  amount: number
  status: InvoiceStatus
  createdAt: string
  lastStatusTransitionAt: string
}

interface PagedResponse {
  data: InvoiceListItemResponse[]
  total: number
  pageSize: number
}

/**
 * Construye la query string omitiendo el filtro de estado cuando es `'all'` y la
 * búsqueda cuando está vacía.
 */
function buildQuery({ status, search, page, pageSize }: GetInvoicesParams): string {
  const params = new URLSearchParams()
  if (status !== 'all') params.set('status', status)
  const trimmed = search.trim()
  if (trimmed.length > 0) params.set('search', trimmed)
  params.set('page', String(page))
  params.set('pageSize', String(pageSize))
  return params.toString()
}

/**
 * Obtiene una página de facturas desde `GET /api/invoices`. Lanza un `Error` con
 * mensaje legible ante respuestas no satisfactorias para que TanStack Query lo
 * exponga como estado de error.
 */
export async function getInvoices(
  params: GetInvoicesParams,
  signal?: AbortSignal
): Promise<PagedInvoices> {
  const response = await fetch(`/api/invoices?${buildQuery(params)}`, { signal })

  if (!response.ok) {
    throw new Error(`No se pudieron cargar las facturas (${response.status}).`)
  }

  const body = (await response.json()) as PagedResponse
  const data: Invoice[] = body.data.map((item) => ({
    id: item.id,
    clientId: item.clientId,
    clientName: item.clientName,
    amount: item.amount,
    status: item.status,
    createdAt: item.createdAt,
    lastStatusTransitionAt: item.lastStatusTransitionAt,
  }))

  return { data, total: body.total, pageSize: body.pageSize }
}
