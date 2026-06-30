import { expect, test } from './fixtures/test'

/**
 * US2 (P1) — Transición manual de estado.
 *
 * Ejecuta la acción de negocio central: abrir el detalle de una factura, elegir un
 * destino permitido y confirmar. Cada caso parte de un estado conocido (reset+seed) y
 * el bloque es serial para no interferir sobre la base de datos compartida (D4).
 */
test.describe
  .serial('US2 — Transición manual', () => {
    test.beforeEach(async ({ resetData }) => {
      await resetData()
    })

    test('2.1 — el detalle ofrece solo los destinos permitidos', async ({ invoicesPage }) => {
      await invoicesPage.goto()
      await invoicesPage.openDetailForStatus('Pendiente')

      const options = await invoicesPage.allowedTransitionOptions()
      // Backend: Pending → { PrimerRecordatorio, Pagado } (data-model.md §2).
      expect(options).toEqual(['1er Recordatorio', 'Pagado'])
    })

    test('2.2 — aplicar transición no terminal: confirmación y persistencia', async ({
      invoicesPage,
    }) => {
      await invoicesPage.goto()

      // Conteo previo de "1er Recordatorio" en el listado.
      await invoicesPage.filterByStatus('1er Recordatorio')
      await invoicesPage.expectAllRowsHaveStatus('1er Recordatorio')
      const before = await invoicesPage.rows().count()

      // Realizar la transición Pendiente → 1er Recordatorio.
      await invoicesPage.filterByStatus('Pendiente')
      await invoicesPage.openDetailForStatus('Pendiente')
      await invoicesPage.changeStatusTo('1er Recordatorio')

      // Confirmación visible (toast role="status").
      await expect(invoicesPage.successToast('1er Recordatorio')).toBeVisible()
      await invoicesPage.closeDialog()

      // Persistencia: el listado de "1er Recordatorio" tiene una factura más.
      await invoicesPage.filterByStatus('1er Recordatorio')
      await invoicesPage.expectAllRowsHaveStatus('1er Recordatorio')
      await expect(invoicesPage.rows()).toHaveCount(before + 1)
    })

    test('2.3 — estado terminal no ofrece control de transición', async ({ invoicesPage }) => {
      await invoicesPage.goto()
      await invoicesPage.openDetailForStatus('Pagado')

      await expect(invoicesPage.dialog().getByText(/no admite cambios de estado/)).toBeVisible()
      await expect(invoicesPage.dialog().getByLabel('Nuevo estado')).toHaveCount(0)
    })
  })
