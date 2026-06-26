import type { DeleteAllDataResult, FlushDatabaseResult } from '../types'
import { readErrorMessage } from './readErrorMessage'

const BASE = '/api/settings/maintenance'

/** Elimina todos los registros de negocio (facturas). Requiere confirmación previa. */
export async function deleteAllData(): Promise<DeleteAllDataResult> {
  const response = await fetch(`${BASE}/delete-all-data`, { method: 'POST' })
  if (!response.ok) {
    throw new Error(await readErrorMessage(response, 'No se pudieron eliminar los datos'))
  }
  return (await response.json()) as DeleteAllDataResult
}

/** Vacía toda la base de datos y vuelve a sembrar. Requiere confirmación previa. */
export async function flushDatabase(): Promise<FlushDatabaseResult> {
  const response = await fetch(`${BASE}/flush-database`, { method: 'POST' })
  if (!response.ok) {
    throw new Error(await readErrorMessage(response, 'No se pudo vaciar la base de datos'))
  }
  return (await response.json()) as FlushDatabaseResult
}
