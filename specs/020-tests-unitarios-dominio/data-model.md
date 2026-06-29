# Data Model — Sujetos de prueba del dominio (spec 020)

Fase 1. Esta feature no define entidades nuevas: el "modelo de datos" es el conjunto de **sujetos bajo prueba (SUT)** de la capa de dominio y sus invariantes verificables. Sirve de mapa para el inventario de casos (`contracts/test-inventory.md`).

## Invoice (raíz de agregado)

**Invariantes de creación** (FR-004, FR-005):
- `clientId` no vacío → si vacío/blanco lanza `ArgumentException`.
- `amount > 0` (constructor de compatibilidad) → si ≤ 0 lanza `ArgumentException`.
- `Create(clientId, items, dueDate)`: `items` no nula y no vacía → si vacía lanza `ArgumentException`.
- `Amount == Σ Items.Subtotal` (RF-011): el monto siempre se deriva.
- Estado inicial `Pending`; `StatusHistory` vacío; `NotificationRetryCount == 0`.

**Comportamiento de estado** (FR-001, FR-002, FR-006):
- `UpdateStatus(newStatus, source)` appendea un `StatusChange` (única vía de cambio) y reinicia `NotificationRetryCount` al entrar en estado notificable.
- `IsTerminal` ⇔ `Status ∈ {Pagado, Desactivado}`.
- `UpdateDetails(...)` lanza `InvalidOperationException` si `IsTerminal`.
- `RecordNotificationResult(...)`, `RecordNotificationRetry()`, `RecordReminderSent()` actualizan auditoría sin alterar el estado.

## InvoiceStatus (enum, conjunto cerrado)

Estados activos: `Pending(1)`, `Pagado(2)`, `PrimerRecordatorio(10)`, `SegundoRecordatorio(11)`, `Desactivado(12)`.
Las pruebas afirman el conjunto exacto (detecta reintroducción de estados legacy retirados en spec 015).

## InvoiceTransitionService (servicio de dominio)

**Matriz manual** (`ApplyManualTransition`) — fuente de verdad de US1:

| Origen | Destinos permitidos | Prohibidos (rechazo explícito) |
|--------|--------------------|--------------------------------|
| `Pending` | `PrimerRecordatorio`, `Pagado` | `SegundoRecordatorio`, `Desactivado`, `Pending` |
| `PrimerRecordatorio` | `SegundoRecordatorio`, `Pagado` | `Pending`, `Desactivado`, `PrimerRecordatorio` |
| `SegundoRecordatorio` | `Desactivado`, `Pagado` | `Pending`, `PrimerRecordatorio`, `SegundoRecordatorio` |
| `Desactivado` | `Pagado` | `Pending`, `PrimerRecordatorio`, `SegundoRecordatorio`, `Desactivado` |
| `Pagado` | — (ninguno) | todos |

**Transición automática por tiempo** (`TryApplyTransition(invoice, config, now)`):
- `Pending → PrimerRecordatorio` tras `PendingToFirstReminderDays`.
- `PrimerRecordatorio → SegundoRecordatorio` tras `FirstToSecondReminderDays`.
- `SegundoRecordatorio → Desactivado` tras `SecondToDeactivatedDays`.
- `Pagado`/`Desactivado` → `false` (no transiciona).
- Plazo no cumplido → `false`; cumplido → aplica y devuelve `true`.
- `invoice`/`config` nulos → `ArgumentNullException`.

**Pago** (`ApplyPayment`): desde cualquier estado activo → `Pagado`; si ya `Pagado` lanza `InvalidOperationException`.

## InvoiceItem (value object)

Invariantes: `Description` no vacía; `Quantity > 0`; `UnitPrice > 0`; `Subtotal == Quantity × UnitPrice`. Cualquier violación lanza `ArgumentException`. (Ya 100% — se conserva.)

## Client (entidad)

Invariantes: `Name` y `Email` obligatorios; email normalizado a minúsculas + trim; `Phone`/`Address` opcionales (blanco → null). `CreateForSeed` fija Id explícito. (Ya 100% — se conserva.)

## StatusChange (value object inmutable)

Registro `From/To/At/Source`. (Ya 100% — se conserva.)

## SystemSettings / EmailSettings / SmtpSettings / EmailTemplate (configuración)

Comportamiento a cubrir (hueco): `UpdateTransitions`, `UpdateEmailSettings(null)` → `ArgumentNullException`, `UpdateTemplate`/`ResetTemplate` (presente vs ausente y efecto sobre `UpdatedAt`), propiedades de `SmtpSettings`/`ResendSettings`.

## EmailTemplateRenderer / EmailTemplateVariables (Domain/Email — hueco principal, 0%)

**EmailTemplateVariables** (catálogo cerrado): `All`/`AllowedSet` consistentes; `IsAllowed(name)` true para admitidos, false para no admitidos; conjunto exacto de 9 variables.

**EmailTemplateRenderer**:
- `Render`: marcador admitido + valor presente → sustituye; admitido + ausente → cadena vacía; no admitido → se deja intacto; plantilla nula → `""`; plantilla vacía → `""`; tolerancia de espacios `{{  var  }}`.
- `ExtractVariables`: devuelve nombres referenciados (admitidos o no), sin duplicados; vacío para plantilla nula/sin marcadores.
- `FindInvalidVariables`: lista sólo no admitidos; vacío ⇒ plantilla válida.
