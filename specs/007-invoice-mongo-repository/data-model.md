# Modelo de Datos: Repositorio MongoDB de Facturas

**Feature**: 007-invoice-mongo-repository | **Date**: 2026-06-24

Esta funcionalidad no introduce nuevas entidades; reutiliza la entidad `Invoice` (spec 005) y define cómo se persiste y consulta en MongoDB.

## Entidad: Invoice (colección `Invoices`)

| Campo | Tipo | Persistencia / Notas |
|-------|------|----------------------|
| `Id` | string (GUID "N") | `_id` del documento; clave primaria |
| `ClientId` | string | **Indexado** (`ClientId_asc`); filtro de `GetByClientIdAsync` |
| `Amount` | decimal | Monto de la factura (> 0) |
| `Status` | InvoiceStatus (enum) | **Indexado** (`Status_asc`); filtro de `GetByStatusAsync` y `GetTransitionableAsync` |
| `CreatedAt` | DateTime (UTC) | Marca de creación; inmutable |
| `UpdatedAt` | DateTime (UTC) | Actualizado en cada cambio de auditoría |
| `RemindersCount` | int | Contador de recordatorios; no tocado por `UpdateStatusAsync` |
| `LastReminderSentAt` | DateTime? (UTC) | Último recordatorio; nullable |
| `LastStatusTransitionAt` | DateTime (UTC) | **Indexado** (`LastStatusTransitionAt_asc`); actualizado en cada cambio de estado |

### Reglas de validación (del dominio)

- `ClientId` no nulo/vacío y `Amount > 0` se validan en el constructor de `Invoice` (capa Domain). El repositorio **no** revalida: persiste entidades de dominio ya válidas.

## Estados (InvoiceStatus)

Conjunto cerrado: `Draft (0)`, `Pending (1)`, `Pagado (2)`, `Overdue (3)`, `Cancelled (4)`, `PrimerRecordatorio (10)`, `SegundoRecordatorio (11)`, `Desactivado (12)`.

Las transiciones de estado en sí son responsabilidad del dominio/servicio (spec 006). El repositorio solo persiste el estado resultante.

## Índices

| Nombre | Campo(s) | Tipo | Propósito |
|--------|----------|------|-----------|
| `Status_asc` | `Status` | Simple, no único, ascendente | Acelerar `GetByStatusAsync` y filtro del worker (FR-005) |
| `ClientId_asc` | `ClientId` | Simple, no único, ascendente | Acelerar `GetByClientIdAsync` (FR-006) |
| `LastStatusTransitionAt_asc` | `LastStatusTransitionAt` | Simple, no único, ascendente | Ordenar/filtrar por momento de transición (worker) |

Creación idempotente al arranque vía `MongoIndexBuilder.EnsureIndexesAsync` (FR-010).

## Operaciones del repositorio → mapeo a MongoDB

| Operación del contrato | Traducción MongoDB | Requisito |
|------------------------|--------------------|-----------|
| `AddAsync(invoice)` (Insert) | `InsertOneAsync` | FR-004 |
| `GetByIdAsync(id)` | `Find(_id == id).FirstOrDefault` | (soporte) |
| `GetByClientIdAsync(clientId)` | `Find(ClientId == clientId).ToList` | FR-002 |
| `GetByStatusAsync(status)` | `Find(Status == status).ToList` | FR-001 |
| `GetTransitionableAsync()` | `Find(Status in {Pending, Primer, Segundo})` | (soporte worker) |
| `UpdateAsync(invoice)` | `ReplaceOneAsync(_id)` | (reemplazo completo) |
| `UpdateStatusAsync(id, status)` | `UpdateOneAsync(_id).Set(Status, UpdatedAt, LastStatusTransitionAt)` | FR-003, FR-007, FR-009 |

Las consultas sin coincidencias devuelven colecciones vacías (FR-008). `UpdateStatusAsync` sobre `id` inexistente es no-op (`MatchedCount = 0`, FR-009).
