import type { APIRequestContext } from '@playwright/test'

/**
 * URL base del backend ASP.NET (Development). El frontend lo alcanza por el proxy
 * `/api`, pero el reset de datos se invoca directamente contra el backend para no
 * depender de que una página esté cargada. Override con `E2E_API_URL` en CI.
 */
export const API_BASE_URL = process.env.E2E_API_URL ?? 'http://localhost:5155'

/** Distribución conocida tras el sembrado idempotente (data-model.md §1). */
export const SEEDED_INVOICE_COUNT = 8
export const SEEDED_CLIENT_COUNT = 3

interface FlushDatabaseResponse {
  deletedInvoices: number
  seeded: boolean
  clientsCreated: number
  invoicesCreated: number
}

/**
 * Restablece la base de datos a un estado conocido y reproducible: vacía la BD,
 * reconstruye índices y vuelve a ejecutar el sembrador idempotente (3 clientes,
 * 8 facturas con estados variados). Es la precondición de las pruebas que mutan
 * estado (research.md D3, FR-007).
 *
 * Devuelve el resumen del backend para que la prueba pueda afirmar que el sembrado
 * dejó la cantidad esperada de datos.
 */
export async function resetData(request: APIRequestContext): Promise<FlushDatabaseResponse> {
  const response = await request.post(`${API_BASE_URL}/api/settings/maintenance/flush-database`)

  if (!response.ok()) {
    throw new Error(
      `No se pudo restablecer la base de datos (${response.status()} ${response.statusText()}). ` +
        `Verifica que el backend esté levantado en ${API_BASE_URL} (ASPNETCORE_ENVIRONMENT=Development).`
    )
  }

  const body = (await response.json()) as FlushDatabaseResponse

  if (!body.seeded || body.invoicesCreated !== SEEDED_INVOICE_COUNT) {
    throw new Error(
      `El sembrado no dejó el estado esperado: ${JSON.stringify(body)}. ` +
        `Se esperaban ${SEEDED_INVOICE_COUNT} facturas y ${SEEDED_CLIENT_COUNT} clientes.`
    )
  }

  return body
}
