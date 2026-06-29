# Data Model — Vista de Envíos (spec 019)

Fase 1. El estado de envío sigue **embebido en `Invoice`** (sin colección nueva). El único cambio de esquema es un campo entero nuevo.

## Entidad: Invoice (cambios)

Campos existentes relevantes (sin cambios): `Id`, `ClientId`, `Status`, `CreatedAt`, `LastNotificationType`, `LastNotificationOutcome`, `LastNotificationAt`, `LastNotificationError`.

### Campo nuevo

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `NotificationRetryCount` | `int` | `0` | Reintentos del **aviso vigente** (intentos posteriores al primero). Persistido. Se reinicia a 0 al entrar en un nuevo estado notificable. |

### Reglas de transición del campo

- **Reset**: en `Invoice.UpdateStatus(newStatus, ...)`, si `newStatus` es un **estado notificable** (`PrimerRecordatorio`, `SegundoRecordatorio`, `Pagado`, `Desactivado`), `NotificationRetryCount = 0`. (Inicia el conteo del nuevo aviso.)
- **Primer intento (worker)**: la primera notificación de la transición se registra con `RecordNotificationResult(...)` y **no** incrementa el contador (queda en 0).
- **Reintento**: cada reenvío manual (`POST /resend`) o masivo (`resend-failed`) llama a `RecordNotificationRetry()` → `NotificationRetryCount++`, con éxito o fallo.
- **Cancelación**: marcar como omitido **no** modifica el contador.

### Métodos de dominio

- `void RecordNotificationRetry()` (NUEVO): `NotificationRetryCount++; UpdateAuditDate();`
- `RecordNotificationResult(type, outcome, at, error?)` (existente, sin cambios de firma): registra resultado; no toca el contador.
- `UpdateStatus(newStatus, source)` (existente): se añade el reset condicional de `NotificationRetryCount` cuando el destino es notificable.

### Estados notificables (criterio único, ya en el código)

`PrimerRecordatorio` y `SegundoRecordatorio` → `Reminder`; `Pagado` → `PaymentConfirmation`; `Desactivado` → `DeactivationNotice`. (Mismo `TryMapNotificationType` de `InvoiceTransitionNotifier`/`EmailAdminService`.)

## Derivación: SendStatus (estado de envío de la vista)

No es un campo persistido; se deriva en el DTO de salida:

| `LastNotificationOutcome` | `SendStatus` (API/UI) |
|---------------------------|------------------------|
| `None` | `pending` (pendiente) |
| `Sent` | `sent` (enviado) |
| `Failed` | `failed` (fallido) |
| `Skipped` | `skipped` (omitido) |

`retrying` (reintentando) **no** existe en el servidor: lo añade el frontend mientras una mutación de reenvío está en curso.

## DTO: ShipmentListItemDto (API)

```text
ShipmentListItemDto {
  id: string                 # Invoice.Id
  clientId: string           # Invoice.ClientId
  clientName: string         # resuelto desde Client (fallback: clientId)
  clientEmail: string | null # resuelto desde Client / resolución de correo (null si no resoluble)
  status: string             # Invoice.Status (estado de factura, para contexto)
  sendStatus: string         # derivado (pending/sent/failed/skipped)
  lastAttemptAt: string|null # Invoice.LastNotificationAt (ISO-8601)
  retryCount: int            # Invoice.NotificationRetryCount
  lastError: string | null   # Invoice.LastNotificationError (solo si failed)
}
```

Respuesta de listado: `PagedResponse<ShipmentListItemDto>` (reutiliza el record genérico existente `PagedResponse<T>(Data, Total, PageSize)`).

## Repositorio: IInvoiceRepository (método nuevo)

```text
Task<(IReadOnlyList<Invoice> Items, long Total)> GetShipmentsPagedAsync(
    NotificationOutcome? sendStatus,      # null = todos
    IReadOnlyCollection<string>? clientIds, # null = sin filtro por cliente (búsqueda vacía)
    int page, int pageSize,
    CancellationToken ct = default)
```

- Filtra siempre por **estados notificables** (los 4 anteriores) — alcance Q3.
- `sendStatus` ⇒ `LastNotificationOutcome == sendStatus` cuando no es null.
- `clientIds` ⇒ `ClientId ∈ clientIds` cuando no es null (resultado de la búsqueda por nombre/correo, resuelta en la capa de aplicación/endpoint vía `IClientRepository`).
- Orden: `LastNotificationAt` desc, desempate `CreatedAt` desc.
- Devuelve la página y el total de coincidencias (independiente de la página).

## Índice (MongoDB)

Compuesto sugerido: `{ Status: 1, LastNotificationOutcome: 1, LastNotificationAt: -1 }` para el filtro por estado notificable + sendStatus + orden. Verificar con `explain` (sin COLLSCAN). Reutiliza el índice de `LastNotificationOutcome` existente (spec 017) cuando aplique.

## Validación

- `sendStatus`: si presente, debe ser uno de `pending|sent|failed|skipped` (400 si no).
- `page` ≥ 1; `pageSize` 1..50 (defaults 1/10) — misma regla que `ListInvoicesQueryValidator`.
- `search`: trim; vacío ⇒ ausente; longitud máx. 100 (consistente con `ListInvoices`).
