import { request as playwrightRequest } from '@playwright/test'
import { API_BASE_URL, resetData } from './fixtures/reset-data'
import { expect, test } from './fixtures/test'

/**
 * Spec 016 — Dashboard como pantalla de inicio (escenario A) y gráfico de dona por
 * estado (escenario B) del quickstart. Cubre la parte automatizable de la validación
 * (ruteo y estructura accesible del donut); la validación puramente visual de
 * claro/oscuro, responsive y lector de pantalla (escenario D) sigue siendo manual.
 *
 * Pruebas de solo lectura sobre el estado sembrado; se restablece una vez (beforeAll)
 * para ser independientes del orden (SC-004).
 */
test.describe('016 — Dashboard de inicio y donut', () => {
  test.beforeAll(async () => {
    const context = await playwrightRequest.newContext({ baseURL: API_BASE_URL })
    await resetData(context)
    await context.dispose()
  })

  test('A — "/" muestra el Dashboard y resalta su navegación', async ({ dashboardPage, page }) => {
    await dashboardPage.goto()
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible()
    // El NavLink activo expone aria-current="page" (React Router).
    await expect(dashboardPage.navLink('Dashboard')).toHaveAttribute('aria-current', 'page')
  })

  test('A — navegar a Facturas y volver mantiene el activo coherente', async ({
    dashboardPage,
    page,
  }) => {
    await dashboardPage.goto()
    await dashboardPage.navLink('Facturas').click()
    await expect(page).toHaveURL(/\/facturas$/)
    // En /facturas, Dashboard deja de estar activo y Facturas pasa a estarlo.
    await expect(dashboardPage.navLink('Dashboard')).not.toHaveAttribute('aria-current', 'page')
    await expect(dashboardPage.navLink('Facturas')).toHaveAttribute('aria-current', 'page')

    await dashboardPage.navLink('Dashboard').click()
    await expect(page).toHaveURL(/\/$/)
    await expect(dashboardPage.navLink('Dashboard')).toHaveAttribute('aria-current', 'page')
  })

  test('A — "/dashboard" y rutas desconocidas redirigen a "/"', async ({ page }) => {
    await page.goto('/dashboard')
    await expect(page).toHaveURL(/\/$/)
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible()

    await page.goto('/ruta-inexistente-zzz')
    await expect(page).toHaveURL(/\/$/)
    await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible()
  })

  test('B — el donut muestra el total al centro y una leyenda por estado', async ({
    dashboardPage,
  }) => {
    await dashboardPage.goto()
    await expect(dashboardPage.donut()).toBeVisible()

    // El total del centro coincide con la tarjeta "Total de facturas" (8 sembradas).
    const totalCard = await dashboardPage.readTotalInvoices()
    const totalCenter = await dashboardPage.readDonutCenterTotal()
    expect(totalCenter).toBe(totalCard)
    expect(totalCenter).toBeGreaterThan(0)

    // La leyenda asocia estado ↔ valor: al menos los estados sembrados aparecen.
    expect(await dashboardPage.readStatusCount('Pendiente')).toBeGreaterThan(0)
    expect(await dashboardPage.readStatusCount('1er Recordatorio')).toBeGreaterThan(0)
  })
})
