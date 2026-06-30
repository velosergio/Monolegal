import { defineConfig, devices } from '@playwright/test'

/**
 * Configuración de la suite E2E (spec 023, Spec 5.4 del roadmap).
 *
 * - `testDir: 'e2e'` mantiene las pruebas end-to-end separadas de las de
 *   componentes (Vitest, en `tests/`). Vitest excluye `e2e/` (ver vitest.config.ts).
 * - `webServer` levanta el frontend (Vite dev en :5173); su proxy `/api` apunta al
 *   backend ASP.NET (Development) en :5155. El backend se asume levantado y sano como
 *   precondición (research.md D2); en local `reuseExistingServer` reaprovecha el dev
 *   server ya en marcha.
 * - Artefactos de diagnóstico solo en fallo/reintento (research.md D8).
 */
export default defineConfig({
  testDir: './e2e',
  // Aislamiento conservador: los specs que mutan estado del backend compartido se
  // serializan mediante `test.describe.serial` (research.md D4). No usamos paralelismo
  // total entre archivos para evitar interferencias sobre la única base de datos.
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI
    ? [['list'], ['html', { open: 'never' }]]
    : [['list'], ['html', { open: 'never' }]],
  timeout: 30_000,
  expect: { timeout: 10_000 },
  use: {
    baseURL: 'http://localhost:5173',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: 'npm run dev',
    url: 'http://localhost:5173',
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
    stdout: 'ignore',
    stderr: 'pipe',
  },
})
