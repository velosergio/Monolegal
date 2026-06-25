# Contrato — POST /api/invoices/transition/{id}

Transición manual del estado de una factura. (Spec 2.3)

## Petición

`POST /api/invoices/transition/{id}`

```json
{ "newStatus": "segundorecordatorio" }
```

| Campo | Tipo | Requerido | Reglas |
|-------|------|-----------|--------|
| `newStatus` | string | Sí | ∈ estados válidos. Ausente o inválido → `400`. |

## Matriz de transiciones permitidas (spec 006)

| Estado actual | Destinos permitidos |
|---------------|---------------------|
| `pending` | `primerrecordatorio`, `pagado` |
| `primerrecordatorio` | `segundorecordatorio`, `pagado` |
| `segundorecordatorio` | `desactivado`, `pagado` |
| `desactivado` | `pagado` |
| `pagado` | (ninguno) |

## Respuesta `200 OK`

Devuelve la factura actualizada (mismo formato que el detalle de `GET /api/invoices/{id}`), con `status` y `lastStatusTransitionAt` actualizados.

## Casos

| Caso | Resultado |
|------|-----------|
| Transición permitida | `200` con factura actualizada (estado persistido) |
| Transición no permitida por la matriz | `400`, sin modificar la factura |
| `newStatus` ausente o no válido | `400` |
| Identificador inexistente o con formato inválido | `404` (Q4) |

## Requisitos cubiertos

FR-010, FR-011, FR-012, FR-013, FR-014, FR-017, FR-018
