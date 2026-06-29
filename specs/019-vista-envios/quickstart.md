# Quickstart — Vista de Envíos (spec 019)

Guía de validación end-to-end. Asume el repo levantado con Docker Compose (frontend, backend, worker, MongoDB) o los servicios en local.

## Prerrequisitos

- Backend `.NET 10` y frontend (Vite) corriendo; MongoDB con datos sembrados (specs 008).
- Al menos algunas facturas en estados notificables (recordatorios/pagado/desactivado) con resultados de notificación variados (`Sent`/`Failed`/`None`). El reenvío real requiere credenciales de correo configuradas por entorno (spec 017); sin ellas, los envíos resultan `failed` y aún validan el flujo de UI.

## Construir y probar (backend)

```bash
# Desde la raíz del repo
dotnet test backend/Tests        # dominio, aplicación, infraestructura, api
```

Resultados esperados (nuevos):
- Dominio: `NotificationRetryCount` se reinicia al entrar en estado notificable; `RecordNotificationRetry` incrementa; `RecordNotificationResult` no toca el contador.
- Aplicación/Api: contratos de `GET /api/invoices/shipments`, `POST /api/invoices/{id}/resend`, `POST /api/invoices/{id}/cancel-notification` (ver `contracts/`).

## Construir y probar (frontend)

```bash
cd frontend
npm test                         # Vitest + Testing Library (feature shipments)
npx @biomejs/biome check .       # linting/formatting
# React Doctor según el flujo del proyecto (objetivo 100/100 honesto)
```

## Validación manual (UI)

1. Abrir `/envios`.
   - Esperado: tabla con columnas ID, Cliente, Email, Estado de envío, Último intento, Reintentos; skeletons durante la carga; insignias de color **con etiqueta textual** por estado.
2. Filtrar por estado "Fallido".
   - Esperado: solo facturas `failed` (ver `contracts/get-shipments.md`).
3. Buscar por nombre de cliente y por correo.
   - Esperado: el listado se reduce a coincidencias; combinar con filtro mantiene AND; sin coincidencias ⇒ empty state "sin coincidencias" (distinto de "no hay envíos").
4. Reenviar una factura fallida (acción de fila).
   - Esperado: badge transitorio "reintentando" mientras la mutación está en curso; toast de éxito/error; la fila se refresca con el nuevo `sendStatus`, `lastAttemptAt` y `retryCount` +1 (sin recargar).
5. Cancelar envío de una factura **pendiente**.
   - Esperado: diálogo de confirmación; tras confirmar, `sendStatus` pasa a "omitido"; toast; sobre una factura no pendiente la acción está deshabilitada (o devuelve 409).
6. "Reintentar fallidos" (acción global).
   - Esperado: toast con conteo afectado (reutiliza `POST /api/settings/email/tools/resend-failed`); listado refrescado; "sin elementos" se informa, no es error.
7. A11y: recorrer toda la vista solo con teclado (foco visible); con "reducir movimiento" activo, las animaciones se atenúan.

## Referencias

- Contratos: `contracts/get-shipments.md`, `contracts/resend-invoice.md`, `contracts/cancel-notification.md`
- Modelo de datos: `data-model.md`
- Decisiones técnicas: `research.md`
