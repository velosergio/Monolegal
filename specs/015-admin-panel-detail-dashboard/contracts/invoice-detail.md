# Contrato — `GET /api/invoices/{id}` (detalle extendido)

Detalle completo de una factura, **extendido** con el historial de cambios de estado y los destinos de transición válidos. Alimenta todo el modal de detalle (campos + historial + cambio de estado) en un único fetch.

Habilita: FR-002, FR-003, FR-004, FR-008, FR-009, FR-012, FR-013, FR-014.

## Petición

```
GET /api/invoices/{id}
```

- `id` (path, requerido): identificador de la factura.

## Respuesta `200 OK`

```jsonc
{
  "id": "a1b2c3...",
  "clientId": "cliente-007",
  "amount": 1500000.0,
  "status": "primerrecordatorio",
  "createdAt": "2026-05-01T12:00:00Z",
  "updatedAt": "2026-06-10T09:30:00Z",
  "remindersCount": 1,
  "lastReminderSentAt": "2026-06-10T09:30:00Z",
  "lastStatusTransitionAt": "2026-06-10T09:30:00Z",
  "statusHistory": [
    { "from": "pending", "to": "primerrecordatorio", "at": "2026-06-10T09:30:00Z", "source": "automatic" }
  ],
  "allowedTransitions": ["segundorecordatorio", "pagado"]
}
```

### Campos nuevos

| Campo | Tipo | Notas |
|-------|------|-------|
| `statusHistory` | `StatusChangeDto[]` | Cronológico ascendente (orden de inserción). Tras la migración de backfill (FR-030) toda factura tiene al menos su evento de creación; la UI mantiene la derivación desde `createdAt` solo como respaldo defensivo si llegara vacío. |
| `allowedTransitions` | `string[]` | Estados destino válidos (minúscula) según la matriz de dominio. Vacío en estados terminales (`pagado`, y `desactivado` salvo `pagado`). |

`StatusChangeDto`: `{ from: string, to: string, at: string (ISO-8601 UTC), source: "automatic" | "manual" }`.

## Respuesta `404 Not Found`

`id` inexistente o con formato inválido (comportamiento uniforme existente).

## Reglas

- `allowedTransitions` se calcula con `InvoiceTransitionService.GetAllowedTransitions(status)`; el frontend **no** replica la matriz.
- Estados serializados en minúscula (convención `JsonStringEnumConverter` + `LowerCaseNamingPolicy`).
- Sin cambios de autenticación respecto al endpoint actual.

## Pruebas de contrato (xUnit)

- Una factura con transiciones devuelve `statusHistory` con `from/to/at/source` correctos y en orden.
- `allowedTransitions` coincide con la matriz para cada estado (incluye `pagado`; vacío en `pagado`).
- Factura previa sin historial → `statusHistory: []`.
- `id` inexistente → 404.
