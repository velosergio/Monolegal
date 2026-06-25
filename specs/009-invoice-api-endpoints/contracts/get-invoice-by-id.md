# Contrato — GET /api/invoices/{id}

Detalle completo de una factura. (Spec 2.2)

## Petición

`GET /api/invoices/{id}`

| Parámetro | Tipo | Notas |
|-----------|------|-------|
| `id` | string (ruta) | Identificador de la factura. |

## Respuesta `200 OK`

```json
{
  "id": "a1b2...",
  "clientId": "C-123",
  "amount": 100.50,
  "status": "primerrecordatorio",
  "createdAt": "2026-01-01T10:00:00Z",
  "updatedAt": "2026-01-05T09:00:00Z",
  "remindersCount": 1,
  "lastReminderSentAt": "2026-01-05T09:00:00Z",
  "lastStatusTransitionAt": "2026-01-05T09:00:00Z"
}
```

## Casos

| Caso | Resultado |
|------|-----------|
| Factura existente | `200` con objeto completo |
| Identificador inexistente | `404` |
| Identificador con formato inválido | `404` (uniforme, Q4) |

## Requisitos cubiertos

FR-008, FR-009
