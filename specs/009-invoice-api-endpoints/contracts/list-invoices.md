# Contrato — GET /api/invoices

Lista paginada de facturas, con filtro opcional por estado. (Spec 2.1)

## Petición

`GET /api/invoices?status={estado}&page={n}&pageSize={n}`

| Parámetro | Tipo | Requerido | Default | Reglas |
|-----------|------|-----------|---------|--------|
| `status` | string | No | — | ∈ `pending`, `primerrecordatorio`, `segundorecordatorio`, `desactivado`, `pagado`. Inválido → `400`. |
| `page` | int | No | `1` | `≥ 1`. Presente e inválido → `400`. |
| `pageSize` | int | No | `10` | `1 ≤ pageSize ≤ 50`. Presente e inválido o `>50` → `400`. |

## Respuesta `200 OK`

```json
{
  "data": [
    {
      "id": "a1b2...",
      "clientId": "C-123",
      "amount": 100.50,
      "status": "primerrecordatorio",
      "createdAt": "2026-01-01T10:00:00Z"
    }
  ],
  "total": 8,
  "pageSize": 10
}
```

- `data`: como máximo `pageSize` ítems, ordenados por `createdAt` descendente.
- `total`: número completo de coincidencias del filtro (no de la página).
- `pageSize`: tamaño de página efectivo.

## Casos

| Caso | Resultado |
|------|-----------|
| Sin facturas / página fuera de rango | `200` con `data: []` y `total` real |
| `status` no válido | `400` |
| `page`/`pageSize` presentes e inválidos (0, negativo, no numérico) | `400` |
| `pageSize > 50` | `400` |

## Requisitos cubiertos

FR-001, FR-002, FR-003, FR-003a, FR-004, FR-005, FR-005a, FR-006, FR-007, FR-017, FR-018
