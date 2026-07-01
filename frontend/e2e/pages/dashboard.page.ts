import { expect, type Locator, type Page } from '@playwright/test'

/**
 * Page object del Dashboard. Lee la distribución por estado desde la leyenda textual
 * del gráfico de dona (no del color), comparando por **delta** (research.md D6).
 * Las etiquetas de estado son distintas de los nombres de cliente, por lo que se
 * localizan a nivel de página sin ambigüedad.
 */
export class DashboardPage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto('/')
    await expect(this.page.getByRole('heading', { name: 'Dashboard' })).toBeVisible()
  }

  /** Conteo de facturas para un estado, leído de la leyenda "Facturas por estado". */
  async readStatusCount(statusLabel: string): Promise<number> {
    const item = this.page.getByRole('listitem').filter({ hasText: statusLabel }).first()
    await expect(item).toBeVisible()
    // El valor es el único span cuyo texto es solo dígitos (la etiqueta es texto; el % lleva "%").
    const valueText = await item.locator('span').filter({ hasText: /^\d+$/ }).first().innerText()
    return Number(valueText.trim())
  }

  /** Valor de la tarjeta "Total de facturas". */
  async readTotalInvoices(): Promise<number> {
    const card = this.page
      .getByText('Total de facturas', { exact: true })
      .locator('..')
      .locator('..')
    const valueText = await card.locator('p').first().innerText()
    return Number(valueText.trim())
  }

  /** Indica si se muestra el estado vacío del dashboard. */
  async isEmptyStateVisible(): Promise<boolean> {
    return this.page.getByText('No hay facturas todavía').isVisible()
  }

  /** El gráfico de dona de distribución por estado (spec 016). */
  donut(): Locator {
    return this.page.getByRole('img', { name: 'Distribución de facturas por estado' })
  }

  /** Número total mostrado en el centro de la dona. */
  async readDonutCenterTotal(): Promise<number> {
    const text = await this.page.getByTestId('donut-total').innerText()
    return Number(text.trim())
  }

  /** Enlace de navegación lateral por su nombre (p. ej. "Dashboard", "Facturas"). */
  navLink(name: string): Locator {
    return this.page.getByRole('link', { name, exact: true })
  }
}
