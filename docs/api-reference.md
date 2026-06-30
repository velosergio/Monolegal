# Referencia de la API — Monolegal

> ⚙️ **Archivo generado** por `scripts/gen-api-docs.mjs` desde `docs/openapi.json`. No editar a mano: ejecuta `npm run docs:api` tras refrescar el snapshot.

**Versión del documento**: v1

**Autenticación**: Bearer/JWT — enviar la cabecera `Authorization: Bearer <token>` en los endpoints protegidos.

## Índice
- [Clients](#clients)
- [Invoices](#invoices)
- [Settings](#settings)
- [Workers](#workers)

## Clients

### `GET /api/clients`

**Listar clientes**

Lista paginada de clientes, con búsqueda opcional (query 'search') por nombre o email.

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `search` | query | no | string |
| `page` | query | no | integer |
| `pageSize` | query | no | integer |

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | PagedResponseOfClientDto |
| 400 | Bad Request | HttpValidationProblemDetails |

---

### `POST /api/clients`

**Crear cliente**

**Cuerpo de la petición** (`application/json`): `CreateClientRequest`

```json
{
  "name": "string",
  "email": "string",
  "phone": "string",
  "address": "string"
}
```

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 201 | Created | ClientDto |
| 400 | Bad Request | HttpValidationProblemDetails |

---

### `GET /api/clients/{id}`

**Detalle de cliente**

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `id` | path | sí | string |

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | ClientDto |
| 404 | Not Found | — |

---

### `PUT /api/clients/{id}`

**Editar cliente**

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `id` | path | sí | string |

**Cuerpo de la petición** (`application/json`): `UpdateClientRequest`

```json
{
  "name": "string",
  "email": "string",
  "phone": "string",
  "address": "string"
}
```

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | ClientDto |
| 400 | Bad Request | HttpValidationProblemDetails |
| 404 | Not Found | — |

---

### `DELETE /api/clients/{id}`

**Eliminar cliente**

Elimina un cliente sin facturas asociadas. Devuelve 409 si tiene facturas.

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `id` | path | sí | string |

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 204 | No Content | — |
| 404 | Not Found | — |
| 409 | Conflict | — |

---

## Invoices

### `GET /api/invoices`

**Listar facturas**

Devuelve una lista paginada de facturas, opcionalmente filtrada por estado (query param 'status') y por cliente (query param 'search', coincidencia case-insensitive sobre clientId, máximo 100 caracteres). Admite paginación con 'page' y 'pageSize' (máximo 50).

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `status` | query | no | string |
| `search` | query | no | string |
| `page` | query | no | integer |
| `pageSize` | query | no | integer |

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | PagedResponseOfInvoiceListItemDto |
| 400 | Bad Request | HttpValidationProblemDetails |

---

### `POST /api/invoices`

**Crear factura**

Crea una factura con cliente, items y fecha de vencimiento. El monto se deriva de los items.

**Cuerpo de la petición** (`application/json`): `CreateInvoiceRequest`

```json
{
  "clientId": "string",
  "dueDate": "2026-01-01T00:00:00Z",
  "items": [
    {
      "description": "string",
      "quantity": 0,
      "unitPrice": 0
    }
  ]
}
```

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 201 | Created | InvoiceDetailDto |
| 400 | Bad Request | HttpValidationProblemDetails |

---

### `GET /api/invoices/shipments`

**Listar envíos**

Devuelve una lista paginada de envíos por factura (sólo facturas en estados notificables), opcionalmente filtrada por estado de envío (query 'sendStatus': pending/sent/failed/skipped) y por cliente o correo (query 'search', coincidencia case-insensitive, máximo 100 caracteres). Admite 'page' y 'pageSize' (máximo 50).

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `sendStatus` | query | no | string |
| `search` | query | no | string |
| `page` | query | no | integer |
| `pageSize` | query | no | integer |

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | PagedResponseOfShipmentListItemDto |
| 400 | Bad Request | HttpValidationProblemDetails |

---

### `GET /api/invoices/stats`

**Obtener estadísticas de facturas**

Devuelve métricas agregadas para el dashboard: total de facturas, conteo por estado ('byStatus') y conteo por cliente ('byClient').

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | InvoiceStatsDto |

---

### `POST /api/invoices/transition/{id}`

**Transicionar el estado de una factura**

Aplica una transición manual de estado a la factura indicada. El cuerpo debe incluir 'newStatus'. Devuelve 400 si el cuerpo es inválido o la transición no está permitida, y 404 si la factura no existe.

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `id` | path | sí | string |

**Cuerpo de la petición** (`application/json`): `TransitionRequest`

```json
{
  "newStatus": "string"
}
```

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | InvoiceDetailDto |
| 400 | Bad Request | — |
| 404 | Not Found | — |

---

### `GET /api/invoices/{id}`

**Obtener el detalle de una factura**

Devuelve el objeto completo de la factura indicada por su identificador. Un identificador inexistente o con formato inválido devuelve 404 de forma uniforme.

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `id` | path | sí | string |

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | InvoiceDetailDto |
| 404 | Not Found | — |

---

### `PUT /api/invoices/{id}`

**Editar factura**

Edita cliente, items y vencimiento de una factura no terminal. Recalcula el monto.

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `id` | path | sí | string |

**Cuerpo de la petición** (`application/json`): `UpdateInvoiceRequest`

```json
{
  "clientId": "string",
  "dueDate": "2026-01-01T00:00:00Z",
  "items": [
    {
      "description": "string",
      "quantity": 0,
      "unitPrice": 0
    }
  ]
}
```

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | InvoiceDetailDto |
| 400 | Bad Request | HttpValidationProblemDetails |
| 404 | Not Found | — |
| 409 | Conflict | — |

---

### `DELETE /api/invoices/{id}`

**Eliminar factura**

Elimina permanentemente una factura (hard delete), en cualquier estado.

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `id` | path | sí | string |

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 204 | No Content | — |
| 404 | Not Found | — |

---

### `POST /api/invoices/{id}/cancel-notification`

**Cancelar envío de una factura**

Marca como omitida la notificación de una factura pendiente en estado notificable, para que el worker no la procese. Conserva el registro. Devuelve 409 si la factura no está pendiente y 404 si no existe.

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `id` | path | sí | string |

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | ShipmentListItemDto |
| 404 | Not Found | — |
| 409 | Conflict | — |

---

### `POST /api/invoices/{id}/pay`

**Marcar una factura como pagada**

Aplica la transición de pago a la factura indicada desde cualquier estado activo válido. Devuelve 404 si la factura no existe y 409 si el estado actual no permite el pago.

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `id` | path | sí | string |

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | — |
| 404 | Not Found | — |
| 409 | Conflict | — |

---

### `POST /api/invoices/{id}/resend`

**Reenviar notificación de una factura**

Reenvía la notificación correspondiente al estado actual de la factura e incrementa el contador de reintentos. Fail-soft: un fallo de envío se refleja como 'failed' (HTTP 200). Devuelve el ítem de envío actualizado.

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `id` | path | sí | string |

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | ShipmentListItemDto |
| 404 | Not Found | — |

---

## Settings

### `GET /api/settings/email`

**Obtener la configuración de email**

Devuelve la configuración no secreta del proveedor de email y el estado de su credencial. Nunca expone secretos.

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | — |

---

### `PUT /api/settings/email`

**Actualizar la configuración de email**

Reemplaza la configuración no secreta del proveedor de email. Devuelve 204. Los secretos siguen viniendo del entorno.

**Cuerpo de la petición** (`application/json`): `EmailSettingsRequest`

```json
{
  "activeProvider": "smtp",
  "fromAddress": "string",
  "fromName": "string",
  "smtp": null,
  "resend": null
}
```

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 204 | No Content | — |
| 400 | Bad Request | HttpValidationProblemDetails |

---

### `GET /api/settings/email/templates`

**Listar las plantillas de email**

Devuelve las plantillas efectivas por tipo y el catálogo de variables admitidas.

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | — |

---

### `PUT /api/settings/email/templates/{type}`

**Actualizar una plantilla de email**

Reemplaza el asunto y cuerpo de la plantilla del tipo indicado. Devuelve 204.

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `type` | path | sí | string |

**Cuerpo de la petición** (`application/json`): `EmailTemplateInput`

```json
{
  "subject": "string",
  "body": "string"
}
```

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 204 | No Content | — |
| 400 | Bad Request | — |
| 404 | Not Found | — |

---

### `POST /api/settings/email/templates/{type}/preview`

**Previsualizar una plantilla de email**

Renderiza la plantilla con datos de ejemplo. Aplica la misma validación de variables que la edición.

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `type` | path | sí | string |

**Cuerpo de la petición** (`application/json`): `EmailTemplateInput`

```json
{
  "subject": "string",
  "body": "string"
}
```

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | — |
| 400 | Bad Request | — |
| 404 | Not Found | — |

---

### `POST /api/settings/email/templates/{type}/reset`

**Restablecer una plantilla de email**

Elimina la personalización de la plantilla del tipo indicado, volviendo al default. Devuelve 204.

**Parámetros**

| Nombre | En | Requerido | Tipo |
|--------|----|-----------|------|
| `type` | path | sí | string |

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 204 | No Content | — |
| 404 | Not Found | — |

---

### `POST /api/settings/email/test`

**Enviar un correo de prueba**

Envía un correo de prueba con el proveedor y la plantilla reales. Un fallo de envío se reporta como resultado, no como error 5xx.

**Cuerpo de la petición** (`application/json`): `SendTestEmailInput`

```json
{
  "to": "string",
  "templateType": "string"
}
```

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | — |
| 400 | Bad Request | HttpValidationProblemDetails |

---

### `POST /api/settings/email/tools/resend-failed`

**Reenviar notificaciones fallidas**

Reintenta el envío de las notificaciones de todas las facturas con último resultado Failed. Devuelve conteos (attempted/resent/failed).

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | — |

---

### `POST /api/settings/email/tools/sanitize`

**Sanear notificaciones atascadas**

Marca como Failed las facturas en estado notificable con notificación no registrada (None), conservando el registro. No reintenta ni borra.

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | — |

---

### `POST /api/settings/email/validate`

**Validar la credencial del proveedor de email**

Valida la credencial del proveedor activo (o el indicado) sin enviar correo. Nunca expone el secreto.

**Cuerpo de la petición** (`application/json`): ``

```json
null
```

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | — |

---

### `GET /api/settings/invoice-transitions`

**Obtener la configuración de transiciones**

Devuelve la configuración actual de transiciones automáticas de facturas.

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | — |

---

### `PUT /api/settings/invoice-transitions`

**Actualizar la configuración de transiciones**

Reemplaza la configuración de transiciones automáticas de facturas. Devuelve 204 sin contenido.

**Cuerpo de la petición** (`application/json`): `InvoiceTransitionsConfig`

```json
{
  "pendingToFirstReminderDays": 0,
  "firstToSecondReminderDays": 0,
  "secondToDeactivatedDays": 0
}
```

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 204 | No Content | — |

---

### `POST /api/settings/maintenance/delete-all-data`

**Eliminar todos los datos**

Elimina todos los registros de negocio (facturas) conservando la base de datos y la configuración del sistema. Irreversible.

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | — |

---

### `POST /api/settings/maintenance/flush-database`

**Flush DB**

Vacía toda la base de datos (incluida la configuración), reconstruye índices y ejecuta nuevamente el sembrador de datos. Irreversible.

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | — |

---

## Workers

### `POST /api/workers/trigger-transitions`

**Disparar un ciclo de transiciones**

Dispara manualmente un ciclo de transiciones automáticas de estado de facturas. Devuelve un resumen con el número de facturas evaluadas y transicionadas.

**Respuestas**

| Código | Descripción | Cuerpo |
|--------|-------------|--------|
| 200 | OK | — |

---
