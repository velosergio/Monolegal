# Contrato — Notificaciones por Correo en Transiciones (013)

Contratos de comportamiento que esta feature expone/extiende. No es código de implementación; define firmas, pre/postcondiciones y el esquema del log estructurado para guiar tests e implementación.

## 1. `IEmailService` (Application/Abstractions) — EXTENDIDO

Contrato existente (spec 011) ampliado con el aviso de desactivación.

```csharp
public interface IEmailService
{
    Task SendReminderAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default);
    Task SendPaymentConfirmationAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default);
    // NUEVO (D5):
    Task SendDeactivationNoticeAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default);
}
```

**Precondiciones**: `clientEmail` no nulo/no vacío; `invoice` no nulo. **Postcondición**: la tarea completa si el proveedor aceptó el envío; **un fallo se señala mediante excepción** (el contrato no define tipo de resultado de error). **Cancelación**: respeta `cancellationToken` (apagado del worker).

**Implementaciones**:
- `SmtpEmailService` (Infrastructure, MailKit) — producción; compone el correo con la plantilla del tipo y lo envía por SMTP.
- `NoOpEmailService` (Infrastructure) — Development/CI; no envía, registra un log estructurado y completa con éxito.

## 2. `IClientEmailResolver` (Application/Abstractions) — NUEVO

```csharp
public interface IClientEmailResolver
{
    Task<string?> ResolveEmailAsync(string clientId, CancellationToken cancellationToken = default);
}
```

**Postcondición**: devuelve el correo del cliente o `null`/vacío si no hay correo disponible. **No lanza** por "cliente sin correo" (eso se traduce en `Skipped`).

## 3. `IInvoiceTransitionNotifier` (Application/Abstractions) — NUEVO

```csharp
public interface IInvoiceTransitionNotifier
{
    Task NotifyTransitionAsync(Invoice invoice, InvoiceStatus previousStatus, CancellationToken cancellationToken = default);
}
```

### Comportamiento (impl. `InvoiceTransitionNotifier`)

Dado `invoice` cuyo `Status` ya refleja el **nuevo** estado y `previousStatus` el anterior:

1. **Seleccionar tipo** según `invoice.Status`:
   - `PrimerRecordatorio`/`SegundoRecordatorio` → `Reminder`
   - `Pagado` → `PaymentConfirmation`
   - `Desactivado` → `DeactivationNotice`
   - cualquier otro → sin plantilla.
2. **Sin plantilla** ⇒ `invoice.RecordNotificationResult(type?, Skipped, now)` (motivo "estado sin notificación aplicable"); log y `return`.
3. **Resolver correo** vía `IClientEmailResolver`. Si es `null`/vacío ⇒ `RecordNotificationResult(type, Skipped, now, "sin correo de destinatario")`; log y `return`.
4. **Enviar** invocando el método de `IEmailService` correspondiente al tipo, con `cancellationToken`.
   - **Éxito** ⇒ `RecordNotificationResult(type, Sent, now)`; si `type == Reminder` ⇒ además `invoice.RecordReminderSent()`; log `Sent`.
   - **Excepción (no cancelación)** ⇒ `RecordNotificationResult(type, Failed, now, mensaje)`; **no** incrementa contadores; log `Failed` con motivo. **No** relanza (el fallo de correo no propaga error al llamador).
   - **`OperationCanceledException`** ⇒ se propaga (apagado ordenado); no se registra como `Failed`.
5. **No persiste**: muta `invoice` en memoria. El **llamador** ejecuta un único `UpdateAsync(invoice)` después de `NotifyTransitionAsync`.

### Postcondiciones (invariantes verificables)

| Caso | `Status` | `LastNotificationOutcome` | `RemindersCount` |
|------|----------|---------------------------|------------------|
| Recordatorio enviado | sin cambios (ya transicionado) | `Sent` | +1 |
| Confirmación de pago enviada | sin cambios | `Sent` | sin cambios |
| Aviso de desactivación enviado | sin cambios | `Sent` | sin cambios |
| Envío fallido | sin cambios (no se revierte) | `Failed` | sin cambios |
| Estado sin plantilla / sin correo | sin cambios | `Skipped` | sin cambios |

## 4. Integración con los llamadores

### Worker (`InvoiceTransitionsWorker.RunCycleAsync`)
Por cada factura cuya `TryApplyTransition` devuelva `true`:
1. capturar `previousStatus` (ya se hace),
2. `await _notifier.NotifyTransitionAsync(invoice, previousStatus, ct)`,
3. `await _invoiceRepository.UpdateAsync(invoice, ct)` (una sola escritura con estado + resultado),
4. el `try/catch` por factura ya existente mantiene el aislamiento del lote.

### Endpoint `POST /api/invoices/{id}/pay`
Tras `transitionService.ApplyPayment(invoice)` y **antes** de `UpdateAsync`: `await notifier.NotifyTransitionAsync(invoice, previousStatus, ct)`. La respuesta 200 refleja la transición aunque el correo haya fallado (resultado registrado).

### Endpoint `POST /api/invoices/transition/{id}`
Tras `transitionService.ApplyManualTransition(invoice, newStatus)` y antes de `UpdateAsync`: `await notifier.NotifyTransitionAsync(invoice, previousStatus, ct)`.

## 5. Esquema del log estructurado (Serilog, 3.4)

Un evento por factura procesada, en JSON (CompactJsonFormatter). Propiedades mínimas:

```json
{
  "@t": "2026-06-25T19:05:00.123Z",
  "@mt": "Notificación de transición. InvoiceId={InvoiceId} De={PreviousStatus} A={NewStatus} Tipo={NotificationType} Resultado={NotificationOutcome}",
  "InvoiceId": "ab12...",
  "PreviousStatus": "PrimerRecordatorio",
  "NewStatus": "SegundoRecordatorio",
  "NotificationType": "Reminder",
  "NotificationOutcome": "Sent",
  "Error": null
}
```

- **`NotificationOutcome = Failed`** ⇒ `Error` contiene el motivo resumido y el evento se emite a nivel `Error`/`Warning`.
- El sink de archivo persiste estos eventos (rolling diario); sustituible por sink de nube vía configuración (D6).

## 6. Configuración (variables de entorno)

| Variable | Uso | Default |
|----------|-----|---------|
| `Email__Host`, `Email__Port`, `Email__Username`, `Email__Password`, `Email__From`, `Email__UseStartTls` | Emisor SMTP (producción) | — (obligatorio en prod) |
| `Logging__File__Path` | Ruta del sink JSON persistente | p. ej. `logs/monolegal-.json` |

En **Development** se usa `NoOpEmailService`; las variables `Email__*` son opcionales.
