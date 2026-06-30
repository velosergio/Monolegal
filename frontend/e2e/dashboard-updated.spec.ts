import { expect, test } from './fixtures/test'

/**
 * US3 (P2) — Dashboard actualizado tras una transición.
 *
 * Cierra la jornada crítica: tras una transición, el dashboard refleja la nueva
 * distribución por estado (comparación por **delta**, research.md D6) y el total no
 * cambia. Bloque serial con reset+seed previo (D4).
 *
 * NOTA (US3.3 — estado vacío): no es reproducible con las herramientas disponibles.
 * El endpoint de reset (`flush-database`) siempre re-ejecuta el sembrador idempotente,
 * por lo que la base nunca queda sin facturas. El estado vacío del dashboard ya está
 * cubierto por las pruebas de componente (Vitest, spec 022); aquí se omite de forma
 * consciente (sin `.skip`) por imposibilidad de precondición, no por exclusión.
 */
test.describe
  .serial('US3 — Dashboard actualizado', () => {
    test.beforeEach(async ({ resetData }) => {
      await resetData()
    })

    test('3.1 — la distribución por estado refleja la transición (delta)', async ({
      invoicesPage,
      dashboardPage,
    }) => {
      await dashboardPage.goto()
      const pendingBefore = await dashboardPage.readStatusCount('Pendiente')
      const firstReminderBefore = await dashboardPage.readStatusCount('1er Recordatorio')

      await invoicesPage.transitionFirstPendingToFirstReminder()

      await dashboardPage.goto()
      expect(await dashboardPage.readStatusCount('Pendiente')).toBe(pendingBefore - 1)
      expect(await dashboardPage.readStatusCount('1er Recordatorio')).toBe(firstReminderBefore + 1)
    })

    test('3.2 — el total de facturas permanece igual tras la transición', async ({
      invoicesPage,
      dashboardPage,
    }) => {
      await dashboardPage.goto()
      const totalBefore = await dashboardPage.readTotalInvoices()

      await invoicesPage.transitionFirstPendingToFirstReminder()

      await dashboardPage.goto()
      expect(await dashboardPage.readTotalInvoices()).toBe(totalBefore)
    })
  })
