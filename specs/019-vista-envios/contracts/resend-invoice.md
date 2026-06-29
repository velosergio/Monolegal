# Contrato — POST /api/invoices/{id}/resend

Reenvía la notificación correspondiente al estado actual de la factura. Incrementa el contador de reintentos. Fail-soft (un fallo de envío se registra como `failed`, no como error HTTP 500).

## Request

`POST /api/invoices/{id}/resend` (sin cuerpo)

## Response 200

```json
{
  "id": "a1b2c3",
  "sendStatus": "sent",
  "lastAttemptAt": "2026-06-29T10:15:00Z",
  "retryCount": 3,
  "lastError": null
}
```

- `sendStatus` refleja el resultado del reintento (`sent` o `failed`).
- `retryCount` ya incrementado.
- Si el estado no es notificable, el resultado puede ser `skipped` (sin plantilla / sin destinatario), con 200.

## Response 404

Factura inexistente.

## Comportamiento

1. Carga la factura (404 si no existe).
2. `RecordNotificationRetry()` → incrementa `NotificationRetryCount`.
3. `IInvoiceTransitionNotifier.NotifyTransitionAsync(invoice, invoice.Status)` (misma lógica que el worker / `resend-failed`).
4. `UpdateAsync(invoice)` (única escritura: contador + resultado).
5. Devuelve el ítem de envío actualizado.

Serilog: invoiceId, status, outcome, retryCount, duración. Errores de envío → `LastNotificationError` poblado y `sendStatus: failed` (200, no 500).

## Tests de contrato

- Factura fallida → reenvío exitoso ⇒ `sendStatus: sent`, `retryCount` +1.
- Reenvío que vuelve a fallar ⇒ `sendStatus: failed`, `lastError` poblado, `retryCount` +1, HTTP 200.
- Factura sin correo resoluble ⇒ `sendStatus: skipped`.
- Id inexistente ⇒ 404.
