# Contrato API — Herramientas globales de administración de envíos

Feature: 017-configuracion-resend-tools · Tag OpenAPI: `Settings`

Operaciones **globales/masivas** (clarificación Q3=A). Operan sobre el estado de notificación embebido en `Invoice` (ver data-model §4). Admin-only. Observabilidad Serilog en todas.

---

## POST `/api/settings/email/tools/resend-failed`

Reenvía las notificaciones de todas las facturas con `LastNotificationOutcome == Failed`, reintentando la notificación correspondiente al estado actual de cada factura.

**Request body**: vacío.

**200 OK**

```json
{ "attempted": 12, "resent": 9, "failed": 3 }
```

- `attempted`: facturas en estado `Failed` consideradas.
- `resent`: reenvíos exitosos (`Failed → Sent`).
- `failed`: reintentos que volvieron a fallar (`Failed → Failed`, motivo/fecha actualizados).
- Si `attempted == 0` ⇒ `{ "attempted": 0, "resent": 0, "failed": 0 }` (no es error; FR-022).

**Notas**: operación idempotente en el sentido de que solo actúa sobre `Failed`; los `Sent` no se reenvían. Procesa en lotes acotados; aísla fallos por-factura (fail-soft, como el worker).

---

## POST `/api/settings/email/tools/sanitize`

Sanea las notificaciones atascadas: facturas en estado **notificable** (`primerRecordatorio`, `segundoRecordatorio`, `pagado`, `desactivado`) con `LastNotificationOutcome == None`, marcándolas como `Failed` (motivo "saneado: notificación no registrada"), conservando el registro. **No** reintenta ni borra (clarificación Q2=A).

**Request body**: vacío.

**200 OK**

```json
{ "sanitized": 4 }
```

- `sanitized`: número de facturas marcadas `None → Failed`.
- Si no hay candidatos ⇒ `{ "sanitized": 0 }` (no es error; FR-022).

> El frontend MUST pedir **confirmación explícita** antes de invocar este endpoint (FR-021); el backend ejecuta directamente la acción confirmada.

---

## Semántica de composición

```
sanitize:        None (estado notificable) ──▶ Failed
resend-failed:   Failed ──▶ Sent | Failed
```

Flujo típico de recuperación: `sanitize` (saca atascados del limbo) → `resend-failed` (reintenta todo lo fallido).

---

## Mapeo a requisitos

| Endpoint | Requisitos |
|----------|-----------|
| POST /tools/resend-failed | FR-020, FR-022, FR-023, SC-006 |
| POST /tools/sanitize | FR-021, FR-022, FR-023, SC-006 |
