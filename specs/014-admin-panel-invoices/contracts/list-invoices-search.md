# Contrato — GET /api/invoices (extendido con búsqueda por cliente)

Extiende el contrato existente (`specs/009-invoice-api-endpoints/contracts/list-invoices.md`) añadiendo el parámetro **`search`** y el campo **`lastStatusTransitionAt`** en cada ítem. Habilita FR-012 (búsqueda global) y la columna "Última Acción" (FR-007/FR-008).

## Petición

`GET /api/invoices?status={estado}&search={texto}&page={n}&pageSize={n}`

| Parámetro | Tipo | Requerido | Default | Reglas |
|-----------|------|-----------|---------|--------|
| `status` | string | No | — | ∈ `pending`, `primerrecordatorio`, `segundorecordatorio`, `desactivado`, `pagado`. Inválido → `400`. |
| `search` | string | No | — | **NUEVO**. Se aplica `Trim()`; vacío o solo espacios ⇒ se ignora (sin filtro). Longitud efectiva ≤ 100; si excede → `400`. Coincidencia *contains* **case-insensitive** sobre `clientId`. |
| `page` | int | No | `1` | `≥ 1`. Presente e inválido → `400`. |
| `pageSize` | int | No | `10` | `1 ≤ pageSize ≤ 50`. Presente e inválido o `>50` → `400`. |

`status` y `search` se combinan con **AND**: el resultado son las facturas que cumplen el estado (si se indica) **y** cuyo `clientId` coincide con la búsqueda (si se indica).

## Respuesta `200 OK`

```json
{
  "data": [
    {
      "id": "a1b2...",
      "clientId": "C-123",
      "amount": 100.50,
      "status": "primerrecordatorio",
      "createdAt": "2026-01-01T10:00:00Z",
      "lastStatusTransitionAt": "2026-01-15T09:30:00Z"
    }
  ],
  "total": 8,
  "pageSize": 10
}
```

- `data`: como máximo `pageSize` ítems, ordenados por `createdAt` descendente.
- `lastStatusTransitionAt`: **NUEVO**. ISO-8601 UTC; fecha de la última transición de estado ("Última Acción").
- `total`: número completo de coincidencias de `status` + `search` (no solo de la página).
- `pageSize`: tamaño de página efectivo.

## Casos

| Caso | Resultado |
|------|-----------|
| Sin coincidencias (estado/búsqueda) o página fuera de rango | `200` con `data: []` y `total` real |
| `search` vacío o solo espacios | Tratado como ausente (sin filtro de búsqueda) |
| `search` con metacaracteres regex (`.`, `*`, `(`, …) | Se tratan como literales (escapado server-side); sin error |
| `search` con longitud > 100 | `400` |
| `status` no válido | `400` |
| `page`/`pageSize` presentes e inválidos | `400` |
| `pageSize > 50` | `400` |

## Notas de implementación (Arquitectura Limpia)

- **Api** (`ListInvoices.cs`): acepta `string? search`, lo pasa a `ListInvoicesQuery` y propaga a `GetPagedAsync`.
- **Application** (`ListInvoicesQueryValidator.cs`): `ListInvoicesQuery` gana `Search`; normalización `Trim()` + vacío⇒null; regla de longitud ≤ 100.
- **Domain** (`IInvoiceRepository.GetPagedAsync`): nueva firma con `string? clientSearch`.
- **Infrastructure** (`MongoInvoiceRepository.GetPagedAsync`): combina filtro de estado con `Builders<Invoice>.Filter.Regex(x => x.ClientId, new BsonRegularExpression(Regex.Escape(clientSearch), "i"))`; `Total` recalculado con el mismo filtro.

## Requisitos cubiertos

FR-007, FR-008, FR-011, FR-012, FR-013, FR-014, FR-015 (vía `data: []`).
