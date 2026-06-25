# Contrato — Documento OpenAPI

**Recurso**: `GET /openapi/v1.json`

**Disponibilidad**: solo entorno `Development` (gate `IsDevelopment()`).

**Generado por**: `Microsoft.AspNetCore.OpenApi` (`AddOpenApi` + `MapOpenApi`), ya presente en el código.

## Petición

```
GET /openapi/v1.json
```

Sin parámetros ni cuerpo.

## Respuesta

- **200 OK** (`application/json`): documento OpenAPI 3.x válido.
- **404 Not Found**: si se solicita en un entorno donde la documentación está deshabilitada (p. ej. Production).

## Invariantes verificables (FR-002, FR-005, FR-009, FR-010)

El documento devuelto MUST cumplir:

1. `openapi` presente y versión 3.x.
2. `info.title` = `"Monolegal API"` (o el configurado) e `info.version` = `"v1"`.
3. `paths` contiene, como mínimo, las rutas:
   - `/api/invoices` (GET)
   - `/api/invoices/{id}` (GET)
   - `/api/invoices/transition/{id}` (POST)
   - `/api/invoices/stats` (GET)
4. Cada operación anterior expone `operationId`, `summary`, `description`, parámetros y `responses` con los códigos de estado de su contrato (ver `data-model.md` §2).
5. `components.schemas` contiene: `InvoiceListItemDto`, `InvoiceDetailDto`, `TransitionRequest`, `InvoiceStatsDto` y el esquema de respuesta paginada.
6. `components.securitySchemes` contiene un esquema `Bearer` (`type: http`, `scheme: bearer`, `bearerFormat: JWT`).
7. El esquema del enum `InvoiceStatus` enumera los valores en minúscula: `pending`, `primerrecordatorio`, `segundorecordatorio`, `desactivado`, `pagado`.

## Ejemplo (fragmento ilustrativo, no normativo)

```json
{
  "openapi": "3.0.4",
  "info": { "title": "Monolegal API", "version": "v1" },
  "paths": {
    "/api/invoices/{id}": {
      "get": {
        "tags": ["Invoices"],
        "operationId": "GetInvoiceById",
        "summary": "Obtener el detalle de una factura",
        "responses": {
          "200": { "description": "Factura encontrada" },
          "404": { "description": "Factura no encontrada" }
        }
      }
    }
  },
  "components": {
    "securitySchemes": {
      "Bearer": { "type": "http", "scheme": "bearer", "bearerFormat": "JWT" }
    }
  }
}
```
