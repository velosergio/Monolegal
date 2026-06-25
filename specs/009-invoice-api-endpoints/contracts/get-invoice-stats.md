# Contrato — GET /api/invoices/stats

Estadísticas agregadas para el dashboard. (Spec 2.4)

## Petición

`GET /api/invoices/stats`

## Respuesta `200 OK`

```json
{
  "totalInvoices": 8,
  "byStatus": {
    "pending": 1,
    "primerrecordatorio": 2,
    "segundorecordatorio": 3,
    "pagado": 2
  },
  "byClient": {
    "C-123": 3,
    "C-456": 2,
    "C-789": 3
  }
}
```

- `totalInvoices`: total de facturas en la colección.
- `byStatus`: conteo por estado (clave = cadena de API del estado).
- `byClient`: conteo por cliente (clave = `clientId`).
- **Invariante**: `Σ(byStatus) == totalInvoices`.

## Casos

| Caso | Resultado |
|------|-----------|
| Con facturas | `200` con agregados |
| Sin facturas | `200` con `totalInvoices: 0` y agregados vacíos `{}` |

## Requisitos cubiertos

FR-015, FR-016
