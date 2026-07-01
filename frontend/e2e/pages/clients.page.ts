import { expect, type Locator, type Page } from '@playwright/test'

/**
 * Page object de la vista de Clientes (spec 018, US2) y de sus modales de
 * alta/edición/eliminación. Localiza por rol y etiqueta accesible (research.md D5):
 * no depende de clases CSS ni de la estructura interna del marcado.
 */
export class ClientsPage {
  constructor(private readonly page: Page) {}

  async goto(): Promise<void> {
    await this.page.goto('/clientes')
    await expect(this.page.getByRole('heading', { name: 'Clientes' })).toBeVisible()
  }

  /** Filas de datos de la tabla de clientes (excluye el encabezado). */
  rows(): Locator {
    return this.page.locator('tbody tr')
  }

  /** Fila cuyo contenido incluye el texto dado (p. ej. nombre o email del cliente). */
  rowByText(text: string): Locator {
    return this.rows().filter({ hasText: text })
  }

  /** Escribe en el buscador (nombre o email); la lista filtra con debounce. */
  async search(term: string): Promise<void> {
    await this.page.getByLabel('Buscar clientes').fill(term)
  }

  /** Abre el formulario de alta de cliente. */
  async openCreateForm(): Promise<void> {
    await this.page.getByRole('button', { name: 'Nuevo cliente' }).click()
    await expect(this.dialog().getByText('Nuevo cliente')).toBeVisible()
  }

  dialog(): Locator {
    return this.page.getByRole('dialog')
  }

  /** Rellena el formulario de cliente (nombre y email obligatorios; resto opcional). */
  async fillForm(values: {
    name?: string
    email?: string
    phone?: string
    address?: string
  }): Promise<void> {
    const dialog = this.dialog()
    if (values.name !== undefined) await dialog.getByLabel('Nombre').fill(values.name)
    if (values.email !== undefined) await dialog.getByLabel('Email').fill(values.email)
    if (values.phone !== undefined)
      await dialog.getByLabel('Teléfono (opcional)').fill(values.phone)
    if (values.address !== undefined)
      await dialog.getByLabel('Dirección (opcional)').fill(values.address)
  }

  /** Confirma el alta ("Crear cliente"). */
  async submitCreate(): Promise<void> {
    await this.dialog().getByRole('button', { name: 'Crear cliente' }).click()
  }

  /** Confirma la edición ("Guardar cambios"). */
  async submitEdit(): Promise<void> {
    await this.dialog().getByRole('button', { name: 'Guardar cambios' }).click()
  }

  /** Abre el formulario de edición del cliente con el nombre dado. */
  async openEditForm(clientName: string): Promise<void> {
    await this.rowByText(clientName)
      .getByRole('button', { name: `Editar a ${clientName}` })
      .click()
    await expect(this.dialog().getByText('Editar cliente')).toBeVisible()
  }

  /** Abre la confirmación de eliminación del cliente con el nombre dado y confirma. */
  async deleteClient(clientName: string): Promise<void> {
    await this.rowByText(clientName)
      .getByRole('button', { name: `Eliminar a ${clientName}` })
      .click()
    await expect(this.dialog().getByText('Eliminar cliente')).toBeVisible()
    await this.dialog().getByRole('button', { name: 'Eliminar', exact: true }).click()
  }

  /** Toast por su texto (role="status"). */
  toast(text: string): Locator {
    return this.page.getByText(text)
  }

  /** Mensaje de estado vacío del listado (búsqueda sin coincidencias). */
  emptyState(): Locator {
    return this.page.getByText('No se encontraron clientes.')
  }
}
