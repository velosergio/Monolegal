# Contrato — `GET /api/invoices/stats` (reuso, sin cambios)

Estadísticas agregadas de la cartera. **Ya existe**; el dashboard lo consume sin cambios de contrato.

Habilita: FR-019, FR-021, FR-022, FR-023.

## Petición

```
GET /api/invoices/stats
```

## Respuesta `200 OK`

```jsonc
{
  "totalInvoices": 128,
  "byStatus": {
    "pending": 40,
    "primerrecordatorio": 25,
    "segundorecordatorio": 18,
    "desactivado": 30,
    "pagado": 15
  },
  "byClient": {
    "cliente-001": 12,
    "cliente-002": 9
    // ... cardinalidad variable
  }
}
```

| Campo | Tipo | Notas |
|-------|------|-------|
| `totalInvoices` | `number` | Total de facturas. |
| `byStatus` | `Record<string,number>` | Clave = estado en minúscula. Solo aparecen estados con ≥1 factura. |
| `byClient` | `Record<string,number>` | Clave = `clientId`. Cardinalidad potencialmente alta. |

## Uso por el frontend (research D6, D7)

- `useInvoiceStats` (`useQuery ['invoice-stats']`).
- **Último refresh**: `dataUpdatedAt` del query, formateado en español (FR-021).
- **Por cliente**: derivado de UI → top-N por cantidad + "Otros" (FR-023, Edge Case de cardinalidad alta). Total de clientes distintos = `Object.keys(byClient).length`.
- **Por estado**: mapeo a etiquetas legibles (`INVOICE_STATUS_LABELS`) y colores de `StatusBadge`. Estados sin entrada → 0.
- **Sin datos** (`totalInvoices === 0`): ceros legibles + estado vacío en gráficos.

## Pruebas

- Reutiliza las pruebas existentes del endpoint; el dashboard añade pruebas de componente (Vitest) sobre la forma del DTO consumido (tarjetas, gráficos, último refresh, vacío, error).
