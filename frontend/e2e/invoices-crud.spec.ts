import { request as playwrightRequest } from '@playwright/test'
import { API_BASE_URL, resetData } from './fixtures/reset-data'
import { expect, test } from './fixtures/test'

/**
 * US1 (spec 018) — CRUD de facturas: jornada crear → editar → eliminar y bloqueo en
 * estado terminal (RF-001, RF-003, RF-004a, RF-005). Se serializa porque muta el estado
 * compartido del backend; el reset deja el estado sembrado conocido (3 clientes / 8 facturas).
 *
 * La factura creada se identifica por un monto distintivo (no presente en el seed) para
 * localizar su fila de forma estable a lo largo de la jornada.
 */
test.describe
  .serial('US1 (018) — CRUD de facturas', () => {
    // Monto distintivo: 1 × 99.999 = 99.999 (ningún monto sembrado lo contiene como subcadena).
    const CREATE_AMOUNT = '99.999'
    // Tras editar a cantidad 2: 2 × 99.999 = 199.998.
    const EDIT_AMOUNT = '199.998'
    const DUE_DATE = '2026-12-31'

    test.beforeEach(async () => {
      const context = await playwrightRequest.newContext({ baseURL: API_BASE_URL })
      await resetData(context)
      await context.dispose()
    })

    test('crear → editar → eliminar una factura', async ({ invoicesPage }) => {
      await invoicesPage.goto()

      // 1) Crear: cliente, vencimiento y una línea; el total se deriva (solo lectura).
      await invoicesPage.openCreateForm()
      await invoicesPage.fillForm({
        clientLabel: 'Cliente A (cliente.a@monolegal.test)',
        dueDate: DUE_DATE,
        description: 'Servicio E2E',
        quantity: 1,
        unitPrice: 99999,
      })
      // El total calculado aparece en el formulario antes de enviar (RF-007).
      // (Subtotal y total muestran el mismo valor: basta con la primera coincidencia.)
      await expect(invoicesPage.dialog().getByText(new RegExp(CREATE_AMOUNT)).first()).toBeVisible()
      await invoicesPage.submitCreate()

      await expect(invoicesPage.toast('Factura creada correctamente.')).toBeVisible()
      await expect(invoicesPage.rowByText(CREATE_AMOUNT)).toBeVisible()

      // 2) Editar: cambiar la cantidad recalcula el monto (RF-003).
      await invoicesPage.openEditForm(CREATE_AMOUNT)
      await invoicesPage.dialog().getByLabel('Cantidad de la línea 1').fill('2')
      await expect(invoicesPage.dialog().getByText(new RegExp(EDIT_AMOUNT)).first()).toBeVisible()
      await invoicesPage.submitEdit()

      await expect(invoicesPage.toast('Factura actualizada correctamente.')).toBeVisible()
      await expect(invoicesPage.rowByText(EDIT_AMOUNT)).toBeVisible()
      await expect(invoicesPage.rowByText(CREATE_AMOUNT)).toHaveCount(0)

      // 3) Eliminar: confirmación y desaparición de la tabla (RF-005).
      await invoicesPage.deleteByRowText(EDIT_AMOUNT)
      await expect(invoicesPage.toast('Factura eliminada correctamente.')).toBeVisible()
      await expect(invoicesPage.rowByText(EDIT_AMOUNT)).toHaveCount(0)
    })

    test('una factura en estado terminal no ofrece edición', async ({ invoicesPage }) => {
      await invoicesPage.goto()
      // El seed incluye una factura "Pagado" (Cliente A, 900.000): estado terminal (RF-004a).
      await invoicesPage.filterByStatus('Pagado')
      const terminalRow = invoicesPage.rows().first()
      await expect(terminalRow).toBeVisible()
      // No hay botón de edición en filas terminales; sí el de eliminación (permitido en cualquier estado).
      await expect(terminalRow.getByRole('button', { name: /Editar la factura/ })).toHaveCount(0)
      await expect(terminalRow.getByRole('button', { name: /Eliminar la factura/ })).toBeVisible()
    })
  })
