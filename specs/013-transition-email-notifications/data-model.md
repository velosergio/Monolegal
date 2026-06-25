# Data Model — Envío de Correos y Registro en Transiciones (013)

Cambios de modelo necesarios para registrar el resultado de la notificación **sobre la propia factura** (clarificación: persistencia = en la factura) y para tipar la notificación. No se introducen colecciones nuevas.

## Enumeraciones nuevas (Domain/Enums)

### `NotificationType`

Tipo de notificación enviada según el nuevo estado de la factura.

| Valor | Significado | Estado(s) destino |
|-------|-------------|-------------------|
| `Reminder` | Recordatorio de cobro | `PrimerRecordatorio`, `SegundoRecordatorio` |
| `PaymentConfirmation` | Confirmación de pago | `Pagado` |
| `DeactivationNotice` | Aviso de desactivación / última notificación | `Desactivado` |

### `NotificationOutcome`

Resultado del último intento de notificación.

| Valor | Significado |
|-------|-------------|
| `None` | Aún no se ha intentado notificar / estado sin plantilla aplicable |
| `Sent` | Envío realizado con éxito |
| `Skipped` | No se intentó el envío (sin plantilla para el estado, o sin correo de destinatario) |
| `Failed` | Se intentó el envío y falló (proveedor caído, correo inválido, excepción) |

## Entidad `Invoice` (EDITADA)

### Campos existentes relevantes (sin cambios de definición)

| Campo | Tipo | Rol en esta feature |
|-------|------|---------------------|
| `Id` | `string` | Identificador para logs y registro de resultado |
| `ClientId` | `string` | Entrada de `IClientEmailResolver` para obtener el correo |
| `Status` | `InvoiceStatus` | Nuevo estado que determina la plantilla |
| `RemindersCount` | `int` | Se incrementa **sólo** en recordatorios enviados con éxito |
| `LastReminderSentAt` | `DateTime?` | Se actualiza **sólo** en recordatorios enviados con éxito |
| `LastStatusTransitionAt` | `DateTime` | Base de elegibilidad; cambia al transicionar (idempotencia/reintento) |
| `UpdatedAt` | `DateTime` | Auditoría; se refresca al registrar el resultado |

### Campos nuevos (último resultado de notificación)

| Campo | Tipo | Reglas |
|-------|------|--------|
| `LastNotificationType` | `NotificationType?` | Tipo del último intento; `null` si nunca se intentó |
| `LastNotificationOutcome` | `NotificationOutcome` | Default `None`; refleja el último intento |
| `LastNotificationAt` | `DateTime?` | Momento del último intento (éxito, fallo u omisión registrada) |
| `LastNotificationError` | `string?` | Motivo resumido cuando `Outcome = Failed`; `null` en otros casos |

> Mapeo de persistencia: `MongoInvoiceRepository.UpdateAsync` usa `ReplaceOneAsync` con el POCO completo; los campos nuevos (propiedades públicas con setter privado) se auto-mapean y persisten sin cambios de configuración. Los enums se almacenan como entero por convención del driver.

### Métodos nuevos / afectados (Domain)

- **`RecordNotificationResult(NotificationType type, NotificationOutcome outcome, DateTime at, string? error = null)`** (NUEVO): fija `LastNotificationType`, `LastNotificationOutcome`, `LastNotificationAt`, `LastNotificationError` y refresca `UpdatedAt`. No toca `Status` ni los contadores de recordatorio.
- **`RecordReminderSent()`** (EXISTENTE, reutilizado): se invoca **adicionalmente** sólo cuando `type = Reminder` y `outcome = Sent`, para incrementar `RemindersCount` y fijar `LastReminderSentAt`.

### Invariantes

- `RemindersCount` y `LastReminderSentAt` **sólo** cambian ante un recordatorio con `outcome = Sent` (RF-005/RF-006).
- `LastNotificationError` es no nulo **si y sólo si** `LastNotificationOutcome = Failed`.
- Un fallo de notificación **no** modifica `Status` ni revierte la transición (RF-008/RF-015).
- Para estados sin plantilla, `LastNotificationOutcome = Skipped` y no se invoca al proveedor (RF-009).

## Mapa de transición → notificación (referencia)

| Transición (origen → destino) | Disparador | Tipo | Método del proveedor |
|-------------------------------|------------|------|----------------------|
| `Pending` → `PrimerRecordatorio` | Worker | `Reminder` | `SendReminderAsync` |
| `PrimerRecordatorio` → `SegundoRecordatorio` | Worker | `Reminder` | `SendReminderAsync` |
| `SegundoRecordatorio` → `Desactivado` | Worker | `DeactivationNotice` | `SendDeactivationNoticeAsync` |
| `*` → `Pagado` | Manual (API `pay`/`transition`) | `PaymentConfirmation` | `SendPaymentConfirmationAsync` |
| `Pending` → `PrimerRecordatorio` (manual) | Manual (API `transition`) | `Reminder` | `SendReminderAsync` |
| otras transiciones manuales permitidas a estados con plantilla | Manual | según destino | según destino |

> Nota: el worker, por las reglas de dominio actuales (`InvoiceTransitionService.TryApplyTransition`), aplica `Pending→PrimerRecordatorio`, `PrimerRecordatorio→SegundoRecordatorio` y `SegundoRecordatorio→Desactivado`. La transición a `Pagado` ocurre por acción manual.

## Configuración asociada (no es entidad de dominio)

| Configuración | Origen | Uso |
|---------------|--------|-----|
| `EmailOptions` (Host, Port, Username, Password, From, UseStartTls) | Variables de entorno (`Email__*`) | Emisor SMTP (Infrastructure) |
| Resolución de correo por cliente | Configuración / convención (resolver inicial) | `IClientEmailResolver` |
| Ruta/sink de logs JSON | Configuración (`Logging__File__Path`) | Sink Serilog persistente |
