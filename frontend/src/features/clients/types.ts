/**
 * Tipos del módulo de clientes (spec 018). El cliente es una entidad de primera clase
 * con email obligatorio y único; teléfono y dirección son opcionales.
 */

/** Cliente tal como lo devuelve la API (`/api/clients`). */
export interface Client {
  id: string
  name: string
  email: string
  phone: string | null
  address: string | null
  createdAt: string // ISO-8601 UTC
  updatedAt: string // ISO-8601 UTC
}

/** Respuesta paginada del listado de clientes. */
export interface PagedClients {
  data: Client[]
  total: number
  pageSize: number
}

/** Datos de un formulario de cliente (alta/edición). */
export interface ClientFormValues {
  name: string
  email: string
  phone: string
  address: string
}
