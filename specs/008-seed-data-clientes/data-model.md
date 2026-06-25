# Data Model: Seed Data - 3 Clientes Mínimo

**Feature**: `008-seed-data-clientes` | **Fecha**: 2026-06-25

Esta feature **no introduce entidades nuevas de dominio**. Reutiliza `Invoice` (`005-invoice-entity`) y `InvoiceStatus` (`006-invoice-status-transitions`). Define un **dataset fijo** y las estructuras de orquestación del seeder.

## Entidades reutilizadas

### Invoice (sin cambios)

| Campo | Tipo | Notas para la siembra |
|-------|------|------------------------|
| `Id` | string (GUID "N") | Generado por la entidad. |
| `ClientId` | string | Uno de los 3 IDs de cliente sembrados. |
| `Amount` | decimal | Valor > 0 (invariante de la entidad). |
| `Status` | `InvoiceStatus` | Asignado vía `UpdateStatus(...)`. |
| `CreatedAt` / `UpdatedAt` | DateTime | Gestionados por la entidad. |
| `RemindersCount` | int | Coherente con el estado (ver D4). |
| `LastReminderSentAt` | DateTime? | Coherente con el estado. |
| `LastStatusTransitionAt` | DateTime | Gestionado por `UpdateStatus`. |

**Estados válidos** (`InvoiceStatus`): `Pending`, `PrimerRecordatorio`, `SegundoRecordatorio`, `Desactivado`, `Pagado` (más legacy `Draft`/`Overdue`/`Cancelled`, no usados por el seeder).

## "Cliente" (concepto, no entidad)

Un cliente es un `ClientId` distinto y estable. El seeder define tres constantes:

| Etiqueta | ClientId (constante estable) |
|----------|------------------------------|
| Cliente A | `seed-cliente-a` |
| Cliente B | `seed-cliente-b` |
| Cliente C | `seed-cliente-c` |

## Dataset fijo a sembrar (8 facturas)

> Cumple RF-003 (3/2/3), RF-004 (Cliente A variado), RF-005 (≥1 `primerrecordatorio` y ≥1 `segundorecordatorio`), RF-007 (recordatorios coherentes).

| # | Cliente | Estado | Amount | RemindersCount esperado |
|---|---------|--------|--------|--------------------------|
| 1 | A | `Pending` | 150.00 | 0 |
| 2 | A | `PrimerRecordatorio` | 320.00 | 1 |
| 3 | A | `Pagado` | 90.00 | 0 |
| 4 | B | `SegundoRecordatorio` | 540.00 | 2 |
| 5 | B | `Desactivado` | 75.00 | 2 |
| 6 | C | `Pending` | 210.00 | 0 |
| 7 | C | `PrimerRecordatorio` | 410.00 | 1 |
| 8 | C | `SegundoRecordatorio` | 1200.00 | 2 |

**Verificación de cobertura del dataset**:
- Cliente A: 3 facturas, estados `{Pending, PrimerRecordatorio, Pagado}` → 3 estados distintos (≥2 ⇒ "variados" ✓).
- Cliente B: 2 facturas ✓. Cliente C: 3 facturas ✓. Total = 8 ✓.
- `PrimerRecordatorio` presente (filas 2, 7) ✓. `SegundoRecordatorio` presente (filas 4, 8) ✓.

## Reglas de coherencia de auditoría (RF-007)

| Estado objetivo | Construcción | RemindersCount | LastReminderSentAt |
|-----------------|--------------|----------------|--------------------|
| `Pending` | `UpdateStatus(Pending)` | 0 | null |
| `PrimerRecordatorio` | `UpdateStatus(PrimerRecordatorio)` + `RecordReminderSent()` ×1 | 1 | set |
| `SegundoRecordatorio` | `UpdateStatus(SegundoRecordatorio)` + `RecordReminderSent()` ×2 | 2 | set |
| `Desactivado` | `UpdateStatus(...)` hasta `Desactivado` + `RecordReminderSent()` ×2 | 2 | set |
| `Pagado` | `UpdateStatus(Pagado)` | 0 | null |

## Estructuras de orquestación (nuevas)

### `SeedClientPlan` / `SeedInvoicePlan` (records de Application)

Representan la definición declarativa del dataset antes de materializarse en entidades:

- `SeedInvoicePlan { string ClientId; decimal Amount; InvoiceStatus Status }`
- `SeedDataDefinition` expone `IReadOnlyList<SeedInvoicePlan> Invoices` con las 8 filas anteriores y los 3 `ClientId` constantes.

### `SeedResult` (record de Application)

Resultado observable de una ejecución del seeder:

| Campo | Tipo | Significado |
|-------|------|-------------|
| `Seeded` | bool | `true` si sembró; `false` si omitió. |
| `Reason` | string | p. ej. "base vacía → sembrado" / "datos existentes → omitido". |
| `ClientsCreated` | int | 0 u 3. |
| `InvoicesCreated` | int | 0 u 8. |

## Validaciones derivadas de requisitos

- **RF-006**: todos los estados pertenecen a `InvoiceStatus` válido (garantizado por el tipo enum).
- **RF-008/RF-009**: el seeder consulta `IInvoiceRepository.CountAsync`; sólo siembra si `== 0`.
- **CE-002/CE-004**: la tabla del dataset es la fuente de verdad de las aserciones de los tests.
