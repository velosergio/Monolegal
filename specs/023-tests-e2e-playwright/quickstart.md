# Quickstart — Ejecutar y validar la suite E2E (Spec 5.4)

Guía para levantar el entorno y validar de extremo a extremo los flujos críticos con Playwright. No incluye el código de las pruebas (eso vive en `tasks.md` y la implementación); aquí están los pasos para correr y verificar la suite.

## Prerrequisitos

- **Node 22+** y dependencias del frontend instaladas (`frontend/`).
- **.NET 10 SDK** para el backend (`backend/Api`).
- **MongoDB** accesible para el backend (local o contenedor), según `appsettings.Development.json`.
- Navegadores de Playwright instalados: `npx playwright install` (una vez).

## Topología (entorno de pruebas)

| Servicio | URL | Notas |
|---|---|---|
| Frontend (Vite preview/dev) | http://localhost:5173 | `baseURL` de Playwright; proxy `/api` → backend |
| Backend (ASP.NET, Development) | http://localhost:5155 | Siembra idempotente al arrancar; expone flush |
| MongoDB | según config backend | Persistencia real |

## Pasos

### 1. Levantar el backend (Development)

```bash
cd backend/Api
dotnet run
# Escucha en http://localhost:5155; en Development siembra 3 clientes / 8 facturas si la BD está vacía
```

### 2. Instalar dependencia E2E (una vez)

```bash
cd frontend
npm install -D @playwright/test
npx playwright install
```

### 3. Restablecer datos a un estado conocido (precondición)

La suite lo hace por fixture, pero para validación manual:

```bash
curl -X POST http://localhost:5155/api/settings/maintenance/flush-database
# Respuesta: { deletedInvoices, seeded: true, clientsCreated: 3, invoicesCreated: 8 }
```

### 4. Ejecutar la suite E2E

Playwright levanta el frontend automáticamente vía `webServer` (build + preview, o dev). Desde `frontend/`:

```bash
npm run test:e2e            # playwright test sobre e2e/
npm run test:e2e -- --ui    # (opcional) modo UI interactivo
npx playwright show-report  # (opcional) ver el reporte HTML tras la corrida
```

## Resultados esperados

- **Flujo 1 (lista + filtro)**: la vista `/facturas` carga facturas; filtrar por "1er Recordatorio" deja solo filas con ese badge; volver a "Todos los estados" restaura el listado.
- **Flujo 2 (transición manual)**: en el detalle de una factura `Pendiente`, el select "Nuevo estado" ofrece solo destinos permitidos; al elegir "1er Recordatorio" y pulsar "Cambiar Estado" aparece el toast de confirmación y el nuevo estado; una factura `Pagado` no ofrece control de transición.
- **Flujo 3 (dashboard)**: tras la transición `Pendiente→1er Recordatorio`, el dashboard muestra "Pendiente" con un conteo menor en 1 y "1er Recordatorio" mayor en 1; el total no cambia.
- **Salida**: todas las pruebas en verde; código de salida `0`. Cualquier fallo devuelve código distinto de cero (apto para CI).

## Validación de calidad

- **Determinismo (SC-002)**: ejecutar `npm run test:e2e` **dos veces** seguidas (reseteando datos antes de cada corrida); ambas deben pasar sin diferencias ni flakiness.
- **Independencia de orden (SC-004)**: ejecutar un único spec aislado, p. ej. `npm run test:e2e -- manual-transition.spec.ts`, partiendo de reset+seed; debe pasar por sí solo.
- **Sin omisiones (Principio IV)**: verificar que no hay `.skip`/`.only` en los specs.
- **Sin tocar producción (SC-005)**: `git status` no debe mostrar cambios en `frontend/src/**` ni en `backend/**` (solo `frontend/e2e/**`, `playwright.config.ts`, `package.json`).

## Notas para CI (referencia, automatización completa → Spec 5.5)

- Orden sugerido en CI: levantar Mongo + backend (Docker Compose) → esperar readiness de `:5155` → `npm ci` en frontend → `npx playwright install --with-deps` → `npm run test:e2e`.
- Publicar el reporte HTML y las trazas/vídeos `on-failure` como artefactos del job.
- La orquestación de un único comando que corra backend+frontend+E2E pertenece a la **Spec 5.5 (Test Runner Unificado)**; esta feature deja la suite E2E lista y autocontenida para integrarse allí.
