# Phase 1 — Data Model: CRUD de Facturas y Clientes

Modelo de datos derivado de la spec (Entidades Clave + Requisitos) y de las decisiones de `research.md`. Mapea a colecciones MongoDB (`Invoices` ampliada, `Clients` nueva).

---

## Entidad: Client (NUEVA)

Colección MongoDB: `Clients`.

| Campo | Tipo | Requerido | Reglas |
|-------|------|-----------|--------|
| `Id` | string (GUID "N") | sí (generado) | Identidad. Referenciado por `Invoice.ClientId`. |
| `Name` | string | sí | No vacío; trim; longitud razonable (p. ej. ≤200). |
| `Email` | string | sí | Formato email válido; **único** entre clientes (normalizado a minúsculas). |
| `Phone` | string? | no | Opcional; si se provee, trim. |
| `Address` | string? | no | Opcional; si se provee, trim. |
| `CreatedAt` | DateTime (UTC) | sí (generado) | Marca de auditoría. |
| `UpdatedAt` | DateTime (UTC) | sí (generado) | Se actualiza en cada edición. |

**Reglas de negocio**:
- RF-015 / RF-015a: `Name` no vacío, `Email` formato válido y único. Crear/editar con email duplicado → rechazo (validación + índice único, research D5).
- RF-018: no se puede eliminar un cliente con ≥1 factura asociada (`Invoice.ClientId == Client.Id`).

**Índices** (research D5/D9):
- `Email_unique` — único, sobre `Email` (collation case-insensitive o normalización previa).
- `Name_asc` — ascendente, para orden del listado.

**Invariantes**:
- `Email` siempre presente y normalizado antes de persistir.
- `UpdatedAt >= CreatedAt`.

---

## Value Object: InvoiceItem (NUEVO, embebido)

Lista embebida en el documento `Invoice` (campo `Items`).

| Campo | Tipo | Requerido | Reglas |
|-------|------|-----------|--------|
| `Description` | string | sí | No vacío; trim. |
| `Quantity` | decimal | sí | > 0. |
| `UnitPrice` | decimal | sí | > 0. |
| `Subtotal` | decimal | derivado | = `Quantity × UnitPrice` (no se persiste o se persiste como espejo recalculado; no editable). |

**Invariantes**:
- Inmutable una vez construido (value object).
- `Subtotal` nunca se ingresa: siempre derivado.

---

## Entidad: Invoice (AMPLIADA)

Colección MongoDB: `Invoices` (existente). Campos preexistentes se conservan; se añaden los marcados **NUEVO**.

| Campo | Tipo | Requerido | Reglas |
|-------|------|-----------|--------|
| `Id` | string (GUID "N") | sí | Existente. |
| `ClientId` | string | sí | Existente. Debe referenciar un `Client.Id` existente (validado en creación/edición). |
| `Amount` | decimal | derivado | **CAMBIO**: ya no se captura; = `sum(Items[].Subtotal)`. Mayor que cero. |
| `Items` | List\<InvoiceItem\> | sí (≥1) | **NUEVO**. Al menos una línea válida. |
| `DueDate` | DateTime (UTC) | sí | **NUEVO**. Fecha de vencimiento válida (legacy: backfill). |
| `Status` | InvoiceStatus | sí | Existente. No editable desde el formulario (gestión por transiciones). Inicial `Pending`. |
| `CreatedAt` | DateTime (UTC) | sí | Existente. |
| `UpdatedAt` | DateTime (UTC) | sí | Existente. Se actualiza en cada edición. |
| `RemindersCount` | int | sí | Existente. |
| `LastReminderSentAt` | DateTime? | no | Existente. |
| `LastStatusTransitionAt` | DateTime | sí | Existente. |
| `StatusHistory` | List\<StatusChange\> | sí | Existente (spec 015). |
| `LastNotification*` | varios | no | Existentes (spec 013). |

**Estados** (`InvoiceStatus`, sin cambios): `Pending`, `PrimerRecordatorio`, `SegundoRecordatorio`, `Pagado`, `Desactivado`.

**Estados terminales** (a efectos de edición): `Pagado`, `Desactivado`.

**Reglas de negocio**:
- RF-001/RF-002: crear requiere `ClientId` existente, ≥1 item válido (descripción + cantidad>0 + precio>0) y `DueDate` válida. `Amount` se calcula.
- RF-003/RF-004/RF-004a: editar permite cambiar `ClientId`, `Items`, `DueDate` **solo** si la factura NO está en estado terminal; el `Status` nunca se edita aquí.
- RF-005/RF-010: eliminar es permanente y permitido en cualquier estado, previa confirmación.
- RF-011: `Amount = sum(Items.Subtotal)`; derivado, no editable.

**Invariantes**:
- `Amount == sum(Items.Subtotal)` siempre tras crear/editar.
- `Items.Count >= 1`.
- En estado terminal, los campos editables quedan congelados.

---

## Cambios en interfaces de dominio

### IInvoiceRepository (AMPLIAR)
- `Task DeleteAsync(string id, CancellationToken)` — hard delete de una factura (research D7).
- (Reutiliza `GetByClientIdAsync` para el guard de borrado de cliente; opcionalmente `Task<long> CountByClientIdAsync(string clientId, ...)` para eficiencia.)

### IClientRepository (NUEVO)
- `Task<Client?> GetByIdAsync(string id, CancellationToken)`
- `Task<Client?> GetByEmailAsync(string email, CancellationToken)` — para validar unicidad.
- `Task<(IReadOnlyList<Client> Items, long Total)> GetPagedAsync(string? search, int page, int pageSize, CancellationToken)` — búsqueda por Name/Email, orden por Name (research D9).
- `Task AddAsync(Client client, CancellationToken)`
- `Task UpdateAsync(Client client, CancellationToken)`
- `Task DeleteAsync(string id, CancellationToken)` — hard delete (tras guard en aplicación).
- `Task<long> CountAsync(CancellationToken)` — para seeder/idempotencia.
- `Task<long> DeleteAllAsync(CancellationToken)` — para la "zona de peligro" (coherencia con `IInvoiceRepository`).

---

## Migración (research D6)

`InvoiceItemsBackfillMigration` (HostedService idempotente):
- Documentos `Invoice` sin `Items`: crear `[{ Description: "Concepto", Quantity: 1, UnitPrice: Amount }]`, preservando `Amount`.
- Documentos sin `DueDate`: asignar `CreatedAt + 30 días`.
- Idempotente: solo afecta documentos que carecen de los campos nuevos.

---

## Relaciones

```text
Client (1) ──< (N) Invoice
   Id  ◄────────── ClientId   (referencia por string; integridad validada en aplicación)

Invoice (1) ──embeds── (N) InvoiceItem   (lista embebida; Amount = Σ Subtotal)
```
