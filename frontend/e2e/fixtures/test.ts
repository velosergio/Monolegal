import { test as base, expect } from '@playwright/test'
import { ClientsPage } from '../pages/clients.page'
import { DashboardPage } from '../pages/dashboard.page'
import { InvoicesPage } from '../pages/invoices.page'
import { resetData } from './reset-data'

/**
 * Fixtures de la suite E2E (research.md D4):
 * - `invoicesPage` / `dashboardPage`: page objects con localizadores accesibles.
 * - `resetData`: restablece la BD a un estado conocido. Las pruebas que mutan estado
 *   lo invocan en su `beforeEach` y se ejecutan en bloques `test.describe.serial`
 *   para no interferir entre sí sobre la única base de datos compartida.
 *
 * Las pruebas de solo lectura pueden apoyarse en el estado sembrado sin reset previo.
 */
export const test = base.extend<{
  invoicesPage: InvoicesPage
  dashboardPage: DashboardPage
  clientsPage: ClientsPage
  resetData: () => Promise<void>
}>({
  invoicesPage: async ({ page }, use) => {
    await use(new InvoicesPage(page))
  },
  dashboardPage: async ({ page }, use) => {
    await use(new DashboardPage(page))
  },
  clientsPage: async ({ page }, use) => {
    await use(new ClientsPage(page))
  },
  resetData: async ({ request }, use) => {
    await use(async () => {
      await resetData(request)
    })
  },
})

export { expect }
