# Contrato API — CRUD de Facturas

Endpoints nuevos sobre `/api/invoices`. Complementan los existentes (`GET` listado/detalle/stats, `POST /transition/{id}`, `POST /pay/{id}`). Convenciones: minimal API, enums serializados en minúscula (`JsonStringEnumConverter`), errores de validación vía `ValidationProblem` (400), Serilog por operación. Acceso Admin-only.

## DTOs

```jsonc
// InvoiceItemDto (entrada y salida)
{
  "description": "Asesoría legal mensual",
  "quantity": 2,
  "unitPrice": 150.00,
  "subtotal": 300.00            // SOLO salida (derivado); ignorado si llega en entrada
}

// CreateInvoiceRequest (cuerpo POST)
{
  "clientId": "a1b2c3...",       // requerido; debe existir en Clients
  "dueDate": "2026-08-01T00:00:00Z", // requerido; fecha válida
  "items": [ /* InvoiceItemDto[], ≥1 */ ]
  // NO incluye "amount": es derivado
}

// UpdateInvoiceRequest (cuerpo PUT) — misma forma que Create (sin status, sin amount)
{
  "clientId": "a1b2c3...",
  "dueDate": "2026-08-01T00:00:00Z",
  "items": [ /* ≥1 */ ]
}

// InvoiceDetailDto (salida) — AMPLIADO con items, dueDate (amount sigue presente, derivado)
```

## POST /api/invoices — Crear factura

- **Auth**: Admin.
- **Body**: `CreateInvoiceRequest`.
- **Validación** (`CreateInvoiceValidator`): `clientId` no vacío; `items` ≥1; cada item con `description` no vacío, `quantity > 0`, `unitPrice > 0`; `dueDate` fecha válida.
- **Reglas**: el `clientId` debe existir en `Clients` (si no → 400/404 con mensaje). `Amount` se calcula como Σ subtotales. `Status` inicial `Pending`.
- **Respuestas**:
  - `201 Created` + `InvoiceDetailDto` (header `Location: /api/invoices/{id}`).
  - `400 Bad Request` — `ValidationProblem` (cuerpo inválido) o cliente inexistente.
- **Mapeo**: `MapCreateInvoice` en `Endpoints/Invoices/CreateInvoice.cs`.

## PUT /api/invoices/{id} — Editar factura

- **Auth**: Admin.
- **Body**: `UpdateInvoiceRequest`.
- **Validación**: igual que Create.
- **Reglas**:
  - `404` si la factura no existe.
  - `409 Conflict` (o `400`) si la factura está en estado terminal (`pagado`/`desactivado`) — edición bloqueada (RF-004a). Mensaje explicativo.
  - `400/404` si el nuevo `clientId` no existe.
  - El `Status` no se modifica; `Amount` se recalcula; `UpdatedAt` se actualiza.
- **Respuestas**: `200 OK` + `InvoiceDetailDto`; `400`; `404`; `409`.
- **Mapeo**: `MapUpdateInvoice` en `Endpoints/Invoices/UpdateInvoice.cs`.

## DELETE /api/invoices/{id} — Eliminar factura

- **Auth**: Admin.
- **Reglas**: hard delete permanente, permitido en **cualquier** estado (RF-005/RF-010). La confirmación es responsabilidad del frontend (modal).
- **Respuestas**:
  - `204 No Content` — eliminada.
  - `404 Not Found` — no existe.
- **Mapeo**: `MapDeleteInvoice` en `Endpoints/Invoices/DeleteInvoice.cs`.

## Notas de integración frontend

- Tras éxito de create/update/delete: invalidar `['invoices']` y `['invoice-stats']` (y detalle si aplica) → tabla + dashboard se refrescan (RF-008).
- Toast de éxito/error en cada operación (RF-007). Ante error, el formulario conserva los datos (RF-009).
