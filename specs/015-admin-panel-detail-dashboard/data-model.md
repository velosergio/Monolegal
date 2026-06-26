# Data Model — Detalle de Factura (Modal) y Dashboard (spec 015)

Modelo de datos de la feature: extensiones de dominio (historial + origen), DTOs de transporte y tipos del frontend. Las reglas derivan de los requisitos de [spec.md](./spec.md) y de las decisiones de [research.md](./research.md).

---

## 1. Dominio (backend)

### 1.1 Enum `StatusChangeSource` *(NUEVO — Domain/Enums)*

Origen de un cambio de estado.

| Miembro | Significado |
|---------|-------------|
| `Automatic` | Transición aplicada por `InvoiceTransitionsWorker` (tiempo). |
| `Manual` | Transición/pago solicitado por un administrador (endpoint). |

Serialización HTTP/BSON: cadena en minúscula, consistente con `InvoiceStatus` (`automatic` / `manual`).

### 1.2 Value object `StatusChange` *(NUEVO — Domain/Entities)*

Registro inmutable de una transición de estado.

| Campo | Tipo | Reglas |
|-------|------|--------|
| `From` | `InvoiceStatus` | Estado anterior a la transición. |
| `To` | `InvoiceStatus` | Estado nuevo (`From != To`). |
| `At` | `DateTime` (UTC) | Momento de la transición. |
| `Source` | `StatusChangeSource` | Origen del cambio. |

- Sin identidad propia; vive embebido en `Invoice.StatusHistory`.
- Inmutable una vez creado (constructor con los cuatro valores).

### 1.3 Entidad `Invoice` *(EDITADO — Domain/Entities)*

Campos existentes (sin cambios): `Id, ClientId, Amount, Status, CreatedAt, UpdatedAt, RemindersCount, LastReminderSentAt, LastStatusTransitionAt, LastNotification*`.

**Nuevo**:

| Campo | Tipo | Reglas |
|-------|------|--------|
| `StatusHistory` | `IReadOnlyList<StatusChange>` | Lista ordenada cronológicamente (orden de inserción = ascendente por `At`). Vacía para facturas previas a esta feature. |

**Cambio de comportamiento — `UpdateStatus`**:
- Firma nueva: `UpdateStatus(InvoiceStatus newStatus, StatusChangeSource source)`.
- Efecto: captura `from = Status`, asigna `Status = newStatus`, actualiza `LastStatusTransitionAt` y `UpdatedAt` (como hoy) y **appendea** `new StatusChange(from, newStatus, now, source)` a `StatusHistory`.
- Invariante: todo cambio de estado del dominio pasa por aquí, garantizando que el historial nunca se desincronice del estado actual (FR-029).
- Compatibilidad: los llamadores actuales (`TryApplyTransition`, `ApplyManualTransition`, `ApplyPayment`) pasan el `source` correspondiente (ver research D1).

**Cambio de estado inicial** (clarificación 2026-06-26, FR-031): el constructor inicia `Status = Pending` (ya no `Draft`). Los miembros legacy `Draft/Overdue/Cancelled` se retiran de `InvoiceStatus` (ver research D9); la migración remapea los documentos existentes que los tengan antes de retirarlos.

### 1.4 Servicio `InvoiceTransitionService` *(EDITADO — Domain/Services)*

**Nuevo método puro**:

```text
GetAllowedTransitions(InvoiceStatus current) : IReadOnlyCollection<InvoiceStatus>
```

Devuelve los destinos válidos según la matriz (única fuente de verdad; ver research D2):

| `current` | Destinos |
|-----------|----------|
| `Pending` | `PrimerRecordatorio`, `Pagado` |
| `PrimerRecordatorio` | `SegundoRecordatorio`, `Pagado` |
| `SegundoRecordatorio` | `Desactivado`, `Pagado` |
| `Desactivado` | `Pagado` |
| `Pagado` | (vacío) |

`ApplyManualTransition`/`ApplyPayment` se mantienen consistentes con este método (mismo origen de verdad).

---

## 2. Transporte (DTOs Api)

### 2.1 `StatusChangeDto` *(NUEVO — InvoiceDtos.cs)*

```text
StatusChangeDto(
  string From,     // estado en minúscula
  string To,       // estado en minúscula
  DateTime At,     // UTC
  string Source)   // "automatic" | "manual"
```

### 2.2 `InvoiceDetailDto` *(EDITADO — InvoiceDtos.cs)*

Campos existentes: `Id, ClientId, Amount, Status, CreatedAt, UpdatedAt, RemindersCount, LastReminderSentAt, LastStatusTransitionAt`.

**Nuevo**:

| Campo | Tipo | Origen |
|-------|------|--------|
| `StatusHistory` | `IReadOnlyList<StatusChangeDto>` | `Invoice.StatusHistory` mapeado. |
| `AllowedTransitions` | `IReadOnlyList<string>` | `InvoiceTransitionService.GetAllowedTransitions(invoice.Status)` en minúscula. |

- `FromEntity` ya no basta sola para `AllowedTransitions` (depende del servicio): el mapeo del endpoint compone `FromEntity(invoice)` + destinos calculados. `GetInvoiceById` y `TransitionInvoice` inyectan/usan `InvoiceTransitionService` para poblar `AllowedTransitions`.

### 2.3 `InvoiceStatsDto` *(SIN CAMBIOS — reuso)*

```text
InvoiceStatsDto(
  long TotalInvoices,
  IReadOnlyDictionary<string,long> ByStatus,   // clave = estado en minúscula
  IReadOnlyDictionary<string,long> ByClient)   // clave = clientId
```

El dashboard lo consume tal cual (sin cambios de contrato).

---

## 3. Persistencia (Infrastructure)

- `StatusHistory` se serializa como array embebido en el documento `Invoices` vía el POCO `Invoice` (sin colección/índice nuevos).
- `StatusChangeSource` se serializa como string (alineado con la convención global de enums; verificar el class map/serializer en `Infrastructure` para emitir minúsculas consistentes con `InvoiceStatus`).
- **Migración única e idempotente** (clarificación 2026-06-26, FR-030/FR-031): un proceso administrativo/seeder de arranque que, en este orden, (1) **remapea** los documentos en estados legacy (`Draft/Overdue/Cancelled`) a un estado activo válido —mapeo firme Borrador→Pending, Vencida→Pending, Cancelada→Desactivado— y (2) **siembra** un evento de creación en cada factura sin `StatusHistory`. Reejecutarla no duplica eventos ni revierte estados. La UI mantiene la derivación del evento de creación como respaldo defensivo.
- Invariante de escritura: las rutas de transición persisten con `UpdateAsync` (reemplazo completo), conservando el historial acumulado. **`UpdateStatusAsync` se elimina** (interfaz, impl, fakes y tests): el cambio de estado tiene una única vía siempre historiada (FR-029).

---

## 4. Tipos del frontend (`features/invoices/types.ts` y `features/dashboard/types.ts`)

### 4.1 `StatusChange` *(NUEVO)*

```text
interface StatusChange {
  from: InvoiceStatus
  to: InvoiceStatus
  at: string          // ISO-8601 UTC
  source: 'automatic' | 'manual' | (string & {})
}
```

### 4.2 `InvoiceDetail` *(NUEVO — extiende la forma del listado)*

```text
interface InvoiceDetail {
  id: string
  clientId: string
  amount: number
  status: InvoiceStatus
  createdAt: string
  updatedAt: string
  remindersCount: number
  lastReminderSentAt: string | null
  lastStatusTransitionAt: string
  statusHistory: StatusChange[]
  allowedTransitions: InvoiceStatus[]
}
```

- Se reutilizan `INVOICE_STATUS_LABELS`/`statusLabel` ya existentes para etiquetas; estados desconocidos → etiqueta neutra con valor en bruto.
- `allowedTransitions` alimenta directamente el `ChangeStatusControl` (sin lógica de matriz en el cliente).

### 4.3 `InvoiceStats` *(NUEVO — features/dashboard/types.ts)*

```text
interface InvoiceStats {
  totalInvoices: number
  byStatus: Record<string, number>   // clave = estado
  byClient: Record<string, number>   // clave = clientId
}
```

- Derivados de UI (no del servidor): `topClients(byClient, n)` → top-N + "Otros"; conteos por estado mapeados a etiquetas legibles y colores de `StatusBadge`.

---

## 5. Reglas de validación y estados

- **Cambio de estado**: el frontend solo ofrece `allowedTransitions`; el backend revalida contra la matriz de dominio (defensa en profundidad) y devuelve 400 si la transición ya no es válida.
- **Historial vacío**: si `statusHistory.length === 0`, el modal muestra el evento de creación derivado de `createdAt` (FR-010), nunca una sección vacía.
- **Estado terminal** (`allowedTransitions` vacío): el control de cambio de estado se oculta o deshabilita con indicación (FR-013).
- **Estados desconocidos** (compatibilidad futura): se renderizan con etiqueta neutra; no aparecen como destino (no están en la matriz). Los estados legacy (Borrador/Vencida/Cancelada) se retiran del sistema por migración (FR-031), por lo que no deben aparecer en datos tras ella.
- **Dashboard sin datos** (`totalInvoices === 0`): ceros legibles + estado vacío en gráficos (FR-023).
