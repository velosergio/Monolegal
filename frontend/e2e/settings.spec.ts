import { request as playwrightRequest } from '@playwright/test'
import { API_BASE_URL, resetData } from './fixtures/reset-data'
import { expect, test } from './fixtures/test'

/**
 * Spec 017 — Jornada de `/configuracion` (US1–US4). Cubre la parte automatizable y
 * sin efectos colaterales: render y validaciones de cliente del proveedor (US1),
 * plantillas con variable no admitida y vista previa (US2), validación de la prueba
 * de envío (US3) y las herramientas globales en sus caminos seguros (US4: "nada que
 * procesar" y confirmación obligatoria). No se persiste configuración ni se envían
 * correos reales (en Dev el emisor es NoOp), de modo que las pruebas son deterministas
 * y no contaminan el estado compartido.
 *
 * La validación puramente visual (claro/oscuro, responsive, lector de pantalla) y la
 * persistencia real del proveedor siguen siendo verificación manual del quickstart.
 */

/** Abre un Select (Radix) por su etiqueta y elige la opción indicada. */
async function selectOption(
  // biome-ignore lint/suspicious/noExplicitAny: el tipo Page se infiere del fixture.
  page: any,
  triggerLabel: string,
  optionLabel: string
): Promise<void> {
  await page.getByLabel(triggerLabel).click()
  await page.getByRole('option', { name: optionLabel, exact: true }).click()
}

test.describe('017 — Configuración (US1–US4)', () => {
  test.beforeEach(async ({ page }) => {
    await page.goto('/configuracion')
    await expect(page.getByRole('heading', { name: 'Configuración' })).toBeVisible()
  })

  test('US1 — proveedor: estado de credencial, cambio de proveedor y validación de correo', async ({
    page,
  }) => {
    const provider = page.getByRole('heading', { name: 'Proveedor de email' }).locator('..')
    await expect(provider).toBeVisible()

    // Estado de credencial visible (sin exponer el secreto).
    await expect(page.getByText('Estado de la credencial:')).toBeVisible()

    // Por defecto SMTP: se ven sus campos. Cambiar a Resend revela el dominio y oculta el host.
    await expect(page.getByLabel('Host SMTP')).toBeVisible()
    await selectOption(page, 'Proveedor activo', 'Resend')
    await expect(page.getByLabel('Dominio remitente (Resend)')).toBeVisible()
    await expect(page.getByLabel('Host SMTP')).toHaveCount(0)

    // Correo remitente inválido + Guardar → validación de cliente, sin persistir.
    await selectOption(page, 'Proveedor activo', 'SMTP')
    await page.getByLabel('Correo remitente').fill('no-es-un-email')
    await page.getByRole('button', { name: 'Guardar', exact: true }).first().click()
    await expect(page.getByText('Introduce un correo remitente válido.')).toBeVisible()
  })

  test('US2 — plantillas: variable no admitida rechazada y vista previa válida', async ({
    page,
  }) => {
    const templates = page.getByRole('heading', { name: 'Plantillas de email' }).locator('..')
    await expect(templates).toBeVisible()
    // El editor muestra asunto y cuerpo efectivos.
    await expect(page.getByLabel('Asunto')).toBeVisible()
    const body = page.getByLabel('Cuerpo')
    await expect(body).toBeVisible()

    // Variable no admitida → rechazo con mensaje (validación de cliente, sin guardar).
    await body.fill('Hola {{factura.inexistente}}')
    await page.getByRole('button', { name: 'Guardar', exact: true }).last().click()
    await expect(page.getByText(/Variable no admitida:.*factura\.inexistente/)).toBeVisible()

    // Vista previa con contenido válido → aparece el panel con el cuerpo renderizado.
    await body.fill('Estimado cliente, su factura está pendiente.')
    await page.getByRole('button', { name: 'Vista previa' }).click()
    await expect(
      page
        .getByRole('paragraph')
        .filter({ hasText: 'Estimado cliente, su factura está pendiente.' })
    ).toBeVisible()
  })

  test('US3 — prueba de envío: destino inválido bloquea el envío', async ({ page }) => {
    await expect(page.getByRole('heading', { name: 'Prueba de envío' })).toBeVisible()
    await page.getByLabel('Correo de destino').fill('destino-invalido')
    await page.getByRole('button', { name: 'Enviar prueba' }).click()
    await expect(page.getByText('Introduce un correo de destino válido.')).toBeVisible()
  })

  test('US4 — herramientas: reenvío sin candidatos y confirmación obligatoria de saneamiento', async ({
    page,
  }) => {
    // Estado conocido: tras sembrar no hay notificaciones fallidas pendientes de reenvío.
    const context = await playwrightRequest.newContext({ baseURL: API_BASE_URL })
    await resetData(context)
    await context.dispose()
    await page.reload()

    await expect(
      page.getByRole('heading', { name: 'Herramientas de administración' })
    ).toBeVisible()

    // Reenviar fallidas sin candidatos → mensaje "nada que procesar" (no error).
    await page.getByRole('button', { name: 'Reenviar fallidas' }).click()
    await expect(page.getByText('No hay notificaciones fallidas que reenviar.')).toBeVisible()

    // Sanear exige confirmación obligatoria; cancelar no muta el estado.
    await page.getByRole('button', { name: 'Sanear atascadas' }).click()
    const dialog = page.getByRole('dialog')
    await expect(dialog.getByText('Sanear notificaciones atascadas')).toBeVisible()
    await dialog.getByRole('button', { name: 'Cancelar' }).click()
    await expect(dialog).toBeHidden()
  })
})
