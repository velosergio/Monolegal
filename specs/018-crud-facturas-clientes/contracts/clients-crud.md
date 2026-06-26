# Contrato API — CRUD de Clientes

Endpoints nuevos sobre `/api/clients` (colección `Clients` nueva). Convenciones idénticas a las de facturas: minimal API, `ValidationProblem` (400) para validación, `PagedResponse<T>` reutilizado, Serilog por operación, Admin-only.

## DTOs

```jsonc
// ClientDto (salida)
{
  "id": "a1b2c3...",
  "name": "Acme S.A.",
  "email": "contacto@acme.com",
  "phone": "+57 300 000 0000",   // puede ser null
  "address": "Calle 1 #2-3",     // puede ser null
  "createdAt": "2026-06-26T...",
  "updatedAt": "2026-06-26T..."
}

// CreateClientRequest (cuerpo POST)
{
  "name": "Acme S.A.",           // requerido
  "email": "contacto@acme.com",  // requerido, único
  "phone": "...",                // opcional
  "address": "..."               // opcional
}

// UpdateClientRequest (cuerpo PUT) — misma forma que Create
```

## GET /api/clients — Listar clientes (paginado + búsqueda)

- **Auth**: Admin.
- **Query params**: `search` (opcional, contains case-insensitive sobre name/email, trim, máx 100), `page` (default 1), `pageSize` (default 10, máx 50).
- **Validación** (`ListClientsQueryValidator`, espejo de `ListInvoicesQueryValidator`): page ≥1; pageSize 1..50.
- **Reglas**: orden por `Name` ascendente; búsqueda server-side (research D9).
- **Respuesta**: `200 OK` + `PagedResponse<ClientDto>` (`{ data, total, pageSize }`).
- **Mapeo**: `MapListClients` en `Endpoints/Clients/ListClients.cs`.

## GET /api/clients/{id} — Detalle de cliente

- **Respuestas**: `200 OK` + `ClientDto`; `404 Not Found`.
- **Mapeo**: `MapGetClientById`.

## POST /api/clients — Crear cliente

- **Auth**: Admin.
- **Body**: `CreateClientRequest`.
- **Validación** (`CreateClientValidator`): `name` no vacío; `email` formato válido; `email` único (consulta `GetByEmailAsync` + índice único como red de seguridad).
- **Respuestas**:
  - `201 Created` + `ClientDto` (header `Location`).
  - `400 Bad Request` — `ValidationProblem` (incluye email duplicado).
- **Mapeo**: `MapCreateClient`.

## PUT /api/clients/{id} — Editar cliente

- **Body**: `UpdateClientRequest`.
- **Validación**: igual que Create; la unicidad de email excluye al propio cliente (`email` puede mantenerse).
- **Respuestas**: `200 OK` + `ClientDto`; `400` (incl. email duplicado por otro cliente); `404`.
- **Mapeo**: `MapUpdateClient`.

## DELETE /api/clients/{id} — Eliminar cliente

- **Reglas** (RF-018, research D7): si el cliente tiene ≥1 factura asociada (`Invoice.ClientId == id`), NO se elimina.
- **Respuestas**:
  - `204 No Content` — eliminado.
  - `404 Not Found` — no existe.
  - `409 Conflict` — tiene facturas asociadas; cuerpo con mensaje explicativo (CE-004).
- **Mapeo**: `MapDeleteClient`.

## Notas de integración frontend

- Tras éxito de create/update/delete: invalidar `['clients']` → listado se refresca (RF-021).
- Toast de éxito/error en cada operación (RF-020). El error `409` de borrado se traduce a un mensaje claro ("No se puede eliminar: el cliente tiene facturas asociadas").
- El selector de cliente del formulario de factura consume `GET /api/clients` (búsqueda) para asociar `clientId`.
