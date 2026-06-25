# Modelo de Datos — Endpoints API de Facturas (Phase 1)

Esta feature **no introduce nuevas entidades de dominio**: reutiliza `Invoice` e `InvoiceStatus` (specs 005/006). Define DTOs de transporte HTTP, contratos de repositorio nuevos y la extensión del servicio de transición.

## Entidad reutilizada: `Invoice`

(`backend/Domain/Entities/Invoice.cs` — sin cambios)

| Campo | Tipo | Notas |
|-------|------|-------|
| `Id` | `string` | Identificador (GUID "N"). |
| `ClientId` | `string` | Cliente asociado; clave de agrupación en `byClient`. |
| `Amount` | `decimal` | Importe (>0). |
| `Status` | `InvoiceStatus` | Estado del ciclo de vida. |
| `CreatedAt` | `DateTime` | Fecha de creación; clave de orden del listado (desc). |
| `UpdatedAt` | `DateTime` | Auditoría. |
| `RemindersCount` | `int` | — |
| `LastReminderSentAt` | `DateTime?` | — |
| `LastStatusTransitionAt` | `DateTime` | Momento de la última transición. |

## Enum reutilizado: `InvoiceStatus`

(`backend/Domain/Enums/InvoiceStatus.cs` — sin cambios)

Representación en el contrato HTTP (D1): cadena en minúscula del nombre del miembro.

| Miembro enum | Cadena de API |
|--------------|---------------|
| `Pending` | `pending` |
| `PrimerRecordatorio` | `primerrecordatorio` |
| `SegundoRecordatorio` | `segundorecordatorio` |
| `Desactivado` | `desactivado` |
| `Pagado` | `pagado` |
| `Draft` | `draft` (legacy) |
| `Overdue` | `overdue` (legacy) |
| `Cancelled` | `cancelled` (legacy) |

> Estados válidos aceptados como filtro/destino de transición en esta feature: `pending`, `primerrecordatorio`, `segundorecordatorio`, `desactivado`, `pagado`. Los legacy no son destinos de transición.

## DTOs de transporte (Api)

```text
InvoiceListItemDto       { id, clientId, amount, status, createdAt }
PagedResponse<T>         { data: T[], total: long, pageSize: int }
InvoiceDetailDto         { id, clientId, amount, status, createdAt, updatedAt,
                           remindersCount, lastReminderSentAt, lastStatusTransitionAt }
TransitionRequest        { newStatus: string }
InvoiceStatsDto          { totalInvoices: long,
                           byStatus: Dictionary<string,long>,
                           byClient: Dictionary<string,long> }
```

Reglas de mapeo:
- `status` siempre se emite/recibe como cadena de API (tabla anterior).
- `PagedResponse.pageSize` refleja el tamaño de página efectivo aplicado.
- `byStatus` usa la cadena de API del estado como clave; `byClient` usa `clientId` como clave.

## Contrato de repositorio (Domain) — métodos nuevos en `IInvoiceRepository`

```text
Task<(IReadOnlyList<Invoice> Items, long Total)> GetPagedAsync(
    InvoiceStatus? status, int page, int pageSize, CancellationToken ct = default);

Task<IReadOnlyDictionary<InvoiceStatus, long>> CountByStatusAsync(
    CancellationToken ct = default);

Task<IReadOnlyDictionary<string, long>> CountByClientAsync(
    CancellationToken ct = default);
```

- `GetPagedAsync`: filtro opcional por `status`; orden `CreatedAt` descendente; `Skip/Limit` para ítems; `Total` = conteo de coincidencias del filtro (no de la página).
- `CountByStatusAsync` / `CountByClientAsync`: agregación `$group` sobre toda la colección. Total global se obtiene con `CountAsync()` (ya existente).

> El método `CountAsync()` (sin filtro) y los demás métodos existentes se conservan sin cambios.

## Extensión de servicio de dominio — `InvoiceTransitionService`

```text
void ApplyManualTransition(Invoice invoice, InvoiceStatus newStatus);
```

- Valida `newStatus` contra la matriz de transiciones permitidas (research.md D4).
- Si `newStatus == Pagado` → delega en `ApplyPayment`.
- Transición no permitida → `InvalidOperationException` (el endpoint la traduce a `400`).
- Transición válida → `invoice.UpdateStatus(newStatus)` (actualiza `LastStatusTransitionAt` y `UpdatedAt`).

## Validadores (Application/Validation)

| Validador | Reglas |
|-----------|--------|
| `ListInvoicesQueryValidator` | `page ≥ 1`; `1 ≤ pageSize ≤ 50`; `status`, si presente, ∈ estados válidos. Defaults (`page=1`, `pageSize=10`) se aplican antes de validar solo cuando el parámetro está ausente. |
| `TransitionInvoiceRequestValidator` | `newStatus` requerido y ∈ estados válidos. |

## Trazabilidad requisitos → modelo

| Requisito | Elemento de modelo |
|-----------|--------------------|
| FR-001, FR-004 | `GetPagedAsync` (Items + Total), `PagedResponse` |
| FR-002 | filtro `status` en `GetPagedAsync` + `ListInvoicesQueryValidator` |
| FR-003, FR-003a, FR-006 | `ListInvoicesQueryValidator` (page/pageSize, tope 50, defaults) |
| FR-005, FR-005a | `InvoiceListItemDto`, orden `CreatedAt` desc en `GetPagedAsync` |
| FR-008, FR-009 | `GetByIdAsync` + `InvoiceDetailDto` (404 incl. id inválido) |
| FR-010–FR-014 | `ApplyManualTransition`, `TransitionRequest`, matriz D4 |
| FR-015, FR-016 | `CountByStatusAsync`, `CountByClientAsync`, `CountAsync`, `InvoiceStatsDto` |
| FR-017, FR-018 | validadores + mapeo `InvoiceStatusApi` |
