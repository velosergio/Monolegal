import { expect, type Locator, type Page } from '@playwright/test'

/**
 * Page object de la vista de Facturas y del modal de detalle/transición.
 * Localiza por rol y etiqueta accesible / texto visible estable (research.md D5):
 * no depende de clases CSS ni de la estructura interna del marcado.
 */
export class InvoicesPage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto('/facturas')
    await expect(this.page.getByRole('heading', { name: 'Facturas' })).toBeVisible()
  }

  /** Filas de datos de la tabla (excluye el encabezado). */
  rows(): Locator {
    return this.page.locator('tbody tr')
  }

  /** Primera fila cuyo contenido incluye la etiqueta de estado dada. */
  rowByStatus(statusLabel: string): Locator {
    return this.rows().filter({ hasText: statusLabel }).first()
  }

  /** Abre el filtro de estado y selecciona la opción por su etiqueta visible. */
  async filterByStatus(optionLabel: string): Promise<void> {
    await this.page.getByLabel('Filtrar por estado').click()
    await this.page.getByRole('option', { name: optionLabel, exact: true }).click()
  }

  /** Afirma que hay al menos una fila y que TODAS muestran la etiqueta de estado dada. */
  async expectAllRowsHaveStatus(statusLabel: string): Promise<void> {
    const rows = this.rows()
    const count = await rows.count()
    expect(count).toBeGreaterThan(0)
    for (let i = 0; i < count; i++) {
      await expect(rows.nth(i)).toContainText(statusLabel)
    }
  }

  /** Abre el detalle de la primera factura que esté en el estado dado. */
  async openDetailForStatus(statusLabel: string): Promise<void> {
    await this.rowByStatus(statusLabel)
      .getByRole('button', { name: /Ver detalle de la factura de/ })
      .click()
    await expect(this.dialog().getByText('Detalle de la factura')).toBeVisible()
  }

  dialog(): Locator {
    return this.page.getByRole('dialog')
  }

  /** Devuelve las etiquetas de los destinos permitidos ofrecidos por el select "Nuevo estado". */
  async allowedTransitionOptions(): Promise<string[]> {
    await this.dialog().getByLabel('Nuevo estado').click()
    const options = this.page.getByRole('option')
    await expect(options.first()).toBeVisible()
    const labels = await options.allInnerTexts()
    // Cerrar el desplegable sin elegir nada (Escape) para dejar el control listo.
    await this.page.keyboard.press('Escape')
    return labels.map((label) => label.trim())
  }

  /** Selecciona un destino en el select "Nuevo estado" y confirma con "Cambiar Estado". */
  async changeStatusTo(optionLabel: string): Promise<void> {
    await this.dialog().getByLabel('Nuevo estado').click()
    await this.page.getByRole('option', { name: optionLabel, exact: true }).click()
    await this.dialog().getByRole('button', { name: 'Cambiar Estado' }).click()
  }

  /** Toast de éxito (role="status") tras una transición. */
  successToast(statusLabel: string): Locator {
    return this.page.getByText(`Estado actualizado a «${statusLabel}».`)
  }

  async closeDialog(): Promise<void> {
    await this.page.keyboard.press('Escape')
    await expect(this.dialog()).toBeHidden()
  }

  /**
   * Flujo reutilizable: transiciona la primera factura `Pendiente` a `1er Recordatorio`
   * desde la lista, esperando la confirmación y cerrando el modal. Útil para preparar
   * el estado en pruebas que verifican efectos derivados (p. ej. el dashboard).
   */
  async transitionFirstPendingToFirstReminder(): Promise<void> {
    await this.goto()
    await this.filterByStatus('Pendiente')
    await this.openDetailForStatus('Pendiente')
    await this.changeStatusTo('1er Recordatorio')
    await expect(this.successToast('1er Recordatorio')).toBeVisible()
    await this.closeDialog()
  }
}
