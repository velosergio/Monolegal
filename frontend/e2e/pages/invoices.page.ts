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
    // Aserciones web-first auto-reintentables: esperan a que la tabla termine de
    // re-renderizar la lista filtrada. Snapshotear `count()` aquí provocaba carreras
    // (se capturaba durante el render transitorio sin filtrar y luego se iteraban
    // índices ya inexistentes). En su lugar afirmamos el estado final: ninguna fila
    // sin la etiqueta y al menos una con ella.
    await expect(this.rows().filter({ hasNotText: statusLabel })).toHaveCount(0)
    await expect(this.rows().filter({ hasText: statusLabel }).first()).toBeVisible()
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

  // ── CRUD de facturas (spec 018, US1) ──────────────────────────────────────

  /** Abre el formulario de alta de factura. */
  async openCreateForm(): Promise<void> {
    await this.page.getByRole('button', { name: 'Nueva factura' }).click()
    await expect(this.dialog().getByText('Nueva factura')).toBeVisible()
  }

  /**
   * Rellena el formulario de factura: cliente, vencimiento y la primera línea de
   * detalle (descripción, cantidad, precio unitario). El total es de solo lectura
   * y se deriva de las líneas.
   */
  async fillForm(values: {
    clientLabel?: string
    dueDate?: string
    description?: string
    quantity?: number
    unitPrice?: number
  }): Promise<void> {
    const dialog = this.dialog()
    if (values.clientLabel !== undefined)
      await dialog.getByLabel('Cliente').selectOption({ label: values.clientLabel })
    if (values.dueDate !== undefined)
      await dialog.getByLabel('Fecha de vencimiento').fill(values.dueDate)
    if (values.description !== undefined)
      await dialog.getByLabel('Descripción de la línea 1').fill(values.description)
    if (values.quantity !== undefined)
      await dialog.getByLabel('Cantidad de la línea 1').fill(String(values.quantity))
    if (values.unitPrice !== undefined)
      await dialog.getByLabel('Precio unitario de la línea 1').fill(String(values.unitPrice))
  }

  /** Total (solo lectura) mostrado en el editor de líneas del formulario. */
  formTotal(): Locator {
    return this.dialog().getByText('Total')
  }

  async submitCreate(): Promise<void> {
    await this.dialog().getByRole('button', { name: 'Crear factura' }).click()
  }

  async submitEdit(): Promise<void> {
    await this.dialog().getByRole('button', { name: 'Guardar cambios' }).click()
  }

  /** Fila de la tabla cuyo contenido incluye el texto dado (p. ej. un monto formateado). */
  rowByText(text: string): Locator {
    return this.rows().filter({ hasText: text })
  }

  /** Abre el formulario de edición de la factura cuya fila contiene el texto dado. */
  async openEditForm(rowText: string): Promise<void> {
    await this.rowByText(rowText)
      .getByRole('button', { name: /Editar la factura/ })
      .click()
    await expect(this.dialog().getByText('Editar factura')).toBeVisible()
  }

  /** Elimina la factura cuya fila contiene el texto dado, confirmando el modal. */
  async deleteByRowText(rowText: string): Promise<void> {
    await this.rowByText(rowText)
      .getByRole('button', { name: /Eliminar la factura/ })
      .click()
    await expect(this.dialog().getByText('Eliminar factura')).toBeVisible()
    await this.dialog().getByRole('button', { name: 'Eliminar', exact: true }).click()
  }

  /** Toast por su texto (role="status"). */
  toast(text: string): Locator {
    return this.page.getByText(text)
  }
}
