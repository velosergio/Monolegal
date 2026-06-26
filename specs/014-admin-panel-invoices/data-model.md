# Data Model — Panel de Administración (Layout Base + Listado de Facturas)

**Feature**: `014-admin-panel-invoices` | **Fase**: 1 (Design)

Esta feature es mayormente de presentación (solo lectura). No introduce colecciones ni entidades de dominio nuevas. Modela: (1) la **forma de los datos** que consume la UI, (2) la **extensión acotada** del DTO/contrato de listado, y (3) el **estado de vista** del cliente.

---

## 1. Entidad de dominio (sin cambios estructurales)

### Invoice (Domain)

Ya existe. Campos relevantes para esta vista (no se modifican):

| Campo | Tipo | Uso en la vista |
|-------|------|-----------------|
| `Id` | string | Columna ID (abreviada) |
| `ClientId` | string | Columna Cliente + objetivo de la búsqueda |
| `Amount` | decimal | Columna Monto (formato moneda) |
| `Status` | InvoiceStatus (enum) | Columna Estado (badge) + filtro |
| `CreatedAt` | DateTime (UTC) | Orden (desc) del listado |
| `LastStatusTransitionAt` | DateTime (UTC) | Columna "Última Acción" |

`InvoiceStatus` (espejo backend ↔ frontend): `Draft=0, Pending=1, Pagado=2, Overdue=3, Cancelled=4, PrimerRecordatorio=10, SegundoRecordatorio=11, Desactivado=12`. Estados con etiqueta de filtro expuestos por la API: `pending, primerrecordatorio, segundorecordatorio, desactivado, pagado`.

---

## 2. Contrato de transporte (extensión acotada)

### InvoiceListItemDto (Api) — EDITADO

Se **añade** `LastStatusTransitionAt`; el resto se conserva.

| Campo | Tipo | Cambio |
|-------|------|--------|
| `id` | string | — |
| `clientId` | string | — |
| `amount` | number (decimal) | — |
| `status` | InvoiceStatus (serializado como entero o string según convención existente) | — |
| `createdAt` | string ISO-8601 UTC | — |
| `lastStatusTransitionAt` | string ISO-8601 UTC | **NUEVO** — alimenta "Última Acción" |

### PagedResponse<T> (Api) — sin cambios

```jsonc
{ "data": [ /* InvoiceListItemDto */ ], "total": 123, "pageSize": 10 }
```

### Parámetros de consulta de GET /api/invoices — EDITADO

| Parámetro | Tipo | Req. | Default | Reglas |
|-----------|------|------|---------|--------|
| `status` | string | No | — | ∈ estados válidos; inválido → 400 |
| `search` | string | No | — | **NUEVO**. `Trim()`; vacío/whitespace ⇒ ignorado; longitud ≤ 100 (si excede → 400). Coincidencia *contains* case-insensitive sobre `clientId` |
| `page` | int | No | 1 | ≥ 1; inválido → 400 |
| `pageSize` | int | No | 10 | 1 ≤ pageSize ≤ 50; inválido → 400 |

### Firma del repositorio — EDITADO

```csharp
Task<(IReadOnlyList<Invoice> Items, long Total)> GetPagedAsync(
    InvoiceStatus? status, string? clientSearch, int page, int pageSize,
    CancellationToken cancellationToken = default);
```

- Si `clientSearch` es no nulo/no vacío: filtro Mongo = `status (si aplica)` **AND** `ClientId` *regex* case-insensitive (escapado).
- `Total` = recuento de coincidencias del filtro+búsqueda (independiente de la página).

---

## 3. Tipos de presentación (Frontend — `features/invoices/types.ts`)

### Invoice (UI) — alineado con el DTO

```ts
export interface Invoice {
  id: string
  clientId: string
  amount: number
  status: InvoiceStatus
  createdAt: string                // ISO-8601 UTC
  lastStatusTransitionAt: string   // ISO-8601 UTC — "Última Acción"
}
```

### PagedInvoices (UI) — respuesta paginada

```ts
export interface PagedInvoices {
  data: Invoice[]
  total: number
  pageSize: number
}
```

### InvoicesViewState (estado de vista) — NUEVO

Estado de presentación que determina qué subconjunto se solicita. Vive en el cliente (hook `useInvoicesViewState`).

| Campo | Tipo | Reglas / transiciones |
|-------|------|------------------------|
| `status` | `InvoiceStatus \| 'all'` | `'all'` ⇒ sin filtro. Cambiar ⇒ `page = 1` |
| `search` | string | Texto crudo del input; se *debounce* antes de consultar. Cambiar (tras debounce) ⇒ `page = 1` |
| `page` | number (≥1) | Avanza/retrocede con paginación. Si queda fuera de rango tras nuevo total ⇒ reposicionar a 1 |
| `pageSize` | number | Fijo en 10 para esta feature |

**Derivados**: `totalPages = ceil(total / pageSize)`; `canPrev = page > 1`; `canNext = page < totalPages`.

**Reglas de integridad de la vista**:
- Cambio de `status` o de `search` (debounced) ⇒ reinicia `page` a 1 (FR-014).
- Listado vacío o sin coincidencias ⇒ estado vacío (FR-015), conservando visibles filtro y búsqueda.
- Error de carga ⇒ mensaje legible + reintento; no rompe el shell (FR-016).

---

## 4. Mapa de estados de la UI (máquina simple)

```text
            ┌─────────────┐   datos          ┌──────────────┐
  montaje → │   loading   │ ───────────────▶ │   success    │
            │ (skeletons) │                  │ (tabla+anim) │
            └─────┬───────┘                  └──────┬───────┘
                  │ error                           │ data.length === 0
                  ▼                                 ▼
            ┌─────────────┐                  ┌──────────────┐
            │    error    │                  │    empty     │
            │ (msg+retry) │                  │ (mensaje)    │
            └─────────────┘                  └──────────────┘
   (cambios de status/search/page → vuelven a loading manteniendo
    la página previa visible vía keepPreviousData, sin parpadeo)
```

No hay transiciones de estado de dominio en esta feature (solo lectura). La acción "Pagar" existente, si se conserva en la tabla, reutiliza su mutación y la invalidación de la query `['invoices']`.
