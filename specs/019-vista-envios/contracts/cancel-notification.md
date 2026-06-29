# Contrato — POST /api/invoices/{id}/cancel-notification

"Cancelar envío": marca como **omitida** (`skipped`) la notificación de una factura **pendiente**, para que el worker no la procese. Conserva el registro. Requiere confirmación explícita en el cliente.

## Request

`POST /api/invoices/{id}/cancel-notification` (sin cuerpo)

## Response 200

```json
{
  "id": "a1b2c3",
  "sendStatus": "skipped",
  "lastAttemptAt": "2026-06-29T10:20:00Z",
  "retryCount": 0,
  "lastError": null
}
```

## Response 404

Factura inexistente.

## Response 409

La factura **no está pendiente** (`LastNotificationOutcome != None`) o su estado no es notificable: no hay envío pendiente que cancelar.

```json
{ "error": "La factura no tiene un envío pendiente que cancelar." }
```

## Comportamiento

1. Carga la factura (404 si no existe).
2. Si no es notificable o `LastNotificationOutcome != None` ⇒ 409.
3. `RecordNotificationResult(type, Skipped, now, "cancelado por el administrador")`.
4. `UpdateAsync(invoice)`.
5. Devuelve el ítem de envío actualizado (`sendStatus: skipped`).

No modifica `NotificationRetryCount`. Serilog: invoiceId, status, "cancelado".

## Tests de contrato

- Factura pendiente notificable ⇒ 200, `sendStatus: skipped`.
- Factura ya enviada/fallida ⇒ 409.
- Factura en estado no notificable ⇒ 409.
- Id inexistente ⇒ 404.
