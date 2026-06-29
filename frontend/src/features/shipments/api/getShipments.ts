import type { PagedShipments, ServerSendStatus, Shipment } from '../types'

/**
 * Parámetros de consulta del listado de envíos. `sendStatus` `'all'` significa sin filtro.
 */
export interface GetShipmentsParams {
  sendStatus: ServerSendStatus | 'all'
  search: string
  page: number
  pageSize: number
}

interface ShipmentItemResponse {
  id: string
  clientId: string
  clientName: string
  clientEmail: string | null
  status: string
  sendStatus: ServerSendStatus
  lastAttemptAt: string | null
  retryCount: number
  lastError: string | null
}

interface PagedResponse {
  data: ShipmentItemResponse[]
  total: number
  pageSize: number
}

/**
 * Construye la query string omitiendo el filtro de estado cuando es `'all'` y la
 * búsqueda cuando está vacía.
 */
function buildQuery({ sendStatus, search, page, pageSize }: GetShipmentsParams): string {
  const params = new URLSearchParams()
  if (sendStatus !== 'all') params.set('sendStatus', sendStatus)
  const trimmed = search.trim()
  if (trimmed.length > 0) params.set('search', trimmed)
  params.set('page', String(page))
  params.set('pageSize', String(pageSize))
  return params.toString()
}

/**
 * Obtiene una página de envíos desde `GET /api/invoices/shipments`. Lanza un `Error`
 * con mensaje legible ante respuestas no satisfactorias para que TanStack Query lo
 * exponga como estado de error.
 */
export async function getShipments(
  params: GetShipmentsParams,
  signal?: AbortSignal
): Promise<PagedShipments> {
  const response = await fetch(`/api/invoices/shipments?${buildQuery(params)}`, { signal })

  if (!response.ok) {
    throw new Error(`No se pudieron cargar los envíos (${response.status}).`)
  }

  const body = (await response.json()) as PagedResponse
  const data: Shipment[] = body.data.map((item) => ({
    id: item.id,
    clientId: item.clientId,
    clientName: item.clientName,
    clientEmail: item.clientEmail,
    status: item.status,
    sendStatus: item.sendStatus,
    lastAttemptAt: item.lastAttemptAt,
    retryCount: item.retryCount,
    lastError: item.lastError,
  }))

  return { data, total: body.total, pageSize: body.pageSize }
}
