# Contrato — GET /api/invoices/shipments

Listado paginado de envíos por factura. Alcance: solo facturas en estados notificables.

## Request

`GET /api/invoices/shipments?sendStatus={pending|sent|failed|skipped}&search={texto}&page={n}&pageSize={n}`

| Query param | Tipo | Default | Reglas |
|-------------|------|---------|--------|
| `sendStatus` | string | (ausente = todos) | uno de `pending|sent|failed|skipped` |
| `search` | string | (ausente) | trim; coincidencia parcial case-insensitive contra nombre **o** correo de cliente; máx. 100 |
| `page` | int | 1 | ≥ 1 |
| `pageSize` | int | 10 | 1..50 |

## Response 200

```json
{
  "data": [
    {
      "id": "a1b2c3",
      "clientId": "cli-1",
      "clientName": "ACME S.A.",
      "clientEmail": "pagos@acme.com",
      "status": "primerRecordatorio",
      "sendStatus": "failed",
      "lastAttemptAt": "2026-06-28T14:03:00Z",
      "retryCount": 2,
      "lastError": "SMTP timeout"
    }
  ],
  "total": 37,
  "pageSize": 10
}
```

- `clientEmail` puede ser `null` (correo no resoluble).
- `lastAttemptAt` puede ser `null` (estado `pending`).
- `lastError` solo no nulo cuando `sendStatus == "failed"`.

## Response 400

`ValidationProblem` cuando `sendStatus` no es válido o `page`/`pageSize` fuera de rango (mismo formato que `GET /api/invoices`).

## Notas de implementación

- Búsqueda en dos pasos: `IClientRepository` resuelve los `clientId` por nombre/correo; el repo filtra facturas por esos ids + estados notificables + `sendStatus`.
- Email por fila: resolución por `clientId` distinto de la página (anti N+1, patrón de `ListInvoices`).
- Orden: `LastNotificationAt` desc, desempate `CreatedAt` desc.
- Serilog: log estructurado (sendStatus, search, page, total, returned).

## Tests de contrato

- Sin parámetros → 200, solo facturas notificables, paginado por defecto.
- `sendStatus=failed` → solo `failed`.
- `search` por nombre y por correo → reduce a coincidencias.
- `sendStatus` inválido → 400.
- `pageSize=999` → 400.
- Página vacía (sin coincidencias) → `data: []`, `total: 0`.
