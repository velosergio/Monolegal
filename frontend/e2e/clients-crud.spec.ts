import { request as playwrightRequest } from '@playwright/test'
import { API_BASE_URL, resetData } from './fixtures/reset-data'
import { expect, test } from './fixtures/test'

/**
 * US2 (spec 018) — CRUD de clientes: listar/buscar, crear, editar, eliminar y el guard de
 * integridad referencial al borrar un cliente con facturas asociadas (RF-012..RF-018).
 * Se serializa porque muta el estado compartido; el reset deja 3 clientes sembrados
 * (Cliente A/B/C), todos con facturas asociadas.
 */
test.describe
  .serial('US2 (018) — CRUD de clientes', () => {
    // Cliente nuevo y único por corrida para no colisionar con datos sembrados.
    const unique = Date.now()
    const NEW_NAME = `Cliente E2E ${unique}`
    const NEW_EMAIL = `e2e.${unique}@monolegal.test`
    const EDITED_NAME = `${NEW_NAME} (editado)`

    test.beforeAll(async () => {
      const context = await playwrightRequest.newContext({ baseURL: API_BASE_URL })
      await resetData(context)
      await context.dispose()
    })

    test('listado y búsqueda por nombre', async ({ clientsPage }) => {
      await clientsPage.goto()
      await expect(clientsPage.rowByText('Cliente A')).toBeVisible()
      await expect(clientsPage.rowByText('Cliente B')).toBeVisible()

      await clientsPage.search('Cliente A')
      await expect(clientsPage.rowByText('Cliente A')).toBeVisible()
      // El filtro deja solo coincidencias: ninguna fila de "Cliente B".
      await expect(clientsPage.rowByText('Cliente B')).toHaveCount(0)

      // Término sin coincidencias → estado vacío explícito.
      await clientsPage.search('cliente-inexistente-zzz')
      await expect(clientsPage.emptyState()).toBeVisible()
    })

    test('crear → editar → eliminar un cliente sin facturas', async ({ clientsPage }) => {
      await clientsPage.goto()

      // 1) Crear (RF-014).
      await clientsPage.openCreateForm()
      await clientsPage.fillForm({ name: NEW_NAME, email: NEW_EMAIL, phone: '+57 300 123 4567' })
      await clientsPage.submitCreate()
      await expect(clientsPage.toast('Cliente creado correctamente.')).toBeVisible()
      await clientsPage.search(NEW_NAME)
      await expect(clientsPage.rowByText(NEW_NAME)).toBeVisible()

      // 2) Editar (RF-016).
      await clientsPage.openEditForm(NEW_NAME)
      await clientsPage.dialog().getByLabel('Nombre').fill(EDITED_NAME)
      await clientsPage.submitEdit()
      await expect(clientsPage.toast('Cliente actualizado correctamente.')).toBeVisible()
      await clientsPage.search(EDITED_NAME)
      await expect(clientsPage.rowByText(EDITED_NAME)).toBeVisible()

      // 3) Eliminar sin facturas (RF-017): se permite y desaparece.
      await clientsPage.deleteClient(EDITED_NAME)
      await expect(clientsPage.toast('Cliente eliminado correctamente.')).toBeVisible()
      await expect(clientsPage.rowByText(EDITED_NAME)).toHaveCount(0)
    })

    test('eliminar un cliente con facturas es rechazado (409)', async ({ clientsPage }) => {
      await clientsPage.goto()
      // "Cliente A" tiene 3 facturas sembradas: el borrado debe bloquearse (RF-018).
      await clientsPage.search('Cliente A')
      await clientsPage.deleteClient('Cliente A')

      await expect(
        clientsPage.toast('No se puede eliminar el cliente: tiene facturas asociadas.')
      ).toBeVisible()
      // El cliente NO se elimina: su fila sigue presente.
      await expect(clientsPage.rowByText('Cliente A')).toBeVisible()
    })
  })
