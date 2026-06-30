import { request as playwrightRequest } from '@playwright/test'
import { API_BASE_URL, resetData } from './fixtures/reset-data'
import { expect, test } from './fixtures/test'

/**
 * US1 (P1) — Listar y filtrar facturas por estado.
 *
 * Verifica de extremo a extremo que la lista carga datos reales del backend y que el
 * filtro por estado devuelve únicamente facturas del estado seleccionado. Se restablece
 * el estado conocido una vez (beforeAll) para ser independiente del orden de ejecución
 * (SC-004); los casos no mutan datos.
 */
test.describe('US1 — Lista y filtro de facturas', () => {
  test.beforeAll(async () => {
    const context = await playwrightRequest.newContext({ baseURL: API_BASE_URL })
    await resetData(context)
    await context.dispose()
  })

  test('1.1 — la lista carga facturas sin error', async ({ invoicesPage }) => {
    await invoicesPage.goto()
    await expect(invoicesPage.rows().first()).toBeVisible()
    expect(await invoicesPage.rows().count()).toBeGreaterThan(0)
  })

  test('1.2 — filtrar por estado muestra solo ese estado', async ({ invoicesPage }) => {
    await invoicesPage.goto()
    await invoicesPage.filterByStatus('1er Recordatorio')
    await invoicesPage.expectAllRowsHaveStatus('1er Recordatorio')
  })

  test('1.3 — volver a "Todos los estados" restaura el listado', async ({ invoicesPage }) => {
    await invoicesPage.goto()
    await invoicesPage.filterByStatus('Pagado')
    await invoicesPage.expectAllRowsHaveStatus('Pagado')

    await invoicesPage.filterByStatus('Todos los estados')
    // Con el filtro limpio aparecen facturas de varios estados (más que solo "Pagado").
    await expect(invoicesPage.rowByStatus('Pendiente')).toBeVisible()
    await expect(invoicesPage.rowByStatus('1er Recordatorio')).toBeVisible()
  })

  test('1.4 — sin resultados muestra el estado vacío sin error', async ({ invoicesPage, page }) => {
    await invoicesPage.goto()
    // El seed pobla todos los estados filtrables; para ejercitar el estado vacío del
    // listado de forma determinista buscamos un cliente inexistente (mismo componente).
    await page.getByLabel('Buscar por cliente').fill('cliente-inexistente-zzz')
    await expect(page.getByText('No se encontraron facturas')).toBeVisible()
  })
})
