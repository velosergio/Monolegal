# Data Model — Worker: Transiciones de Estado (Phase 1)

El worker no introduce entidades persistidas nuevas; orquesta entidades existentes y añade una clase de configuración (no persistida en BD). Se describen los datos relevantes para el ciclo del worker.

## Entidades existentes (reutilizadas)

### Invoice (`Monolegal.Domain.Entities.Invoice`)

Campos relevantes para la transición automática:

| Campo | Tipo | Rol en el worker |
|-------|------|------------------|
| `Id` | `string` | Identificador para logging por transición (FR-009). |
| `Status` | `InvoiceStatus` | Estado evaluado; determina elegibilidad y siguiente estado. |
| `LastStatusTransitionAt` | `DateTime` | Base del cálculo de días transcurridos (D5). |
| `RemindersCount` | `int` | No usado por la transición (pertenece al flujo de correo, spec 3.3). |
| `LastReminderSentAt` | `DateTime?` | No usado por la transición (spec 3.3). |
| `UpdatedAt` | `DateTime` | Auditoría; se actualiza al transicionar. |

Comportamiento: `UpdateStatus(newStatus)` cambia `Status`, refresca `LastStatusTransitionAt` y `UpdatedAt`.

### InvoiceStatus (`Monolegal.Domain.Enums.InvoiceStatus`)

Estados del flujo automático: `Pending → PrimerRecordatorio → SegundoRecordatorio → Desactivado`. Estados terminales/no elegibles para transición por tiempo: `Pagado`, `Desactivado` (y `Draft/Overdue/Cancelled` legacy, no incluidos en `GetTransitionableAsync`).

### InvoiceTransitionsConfig (dentro de `SystemSettings`)

Umbrales de días **administrables** (configuración de negocio), leídos por ciclo:

| Campo | Tipo | Default | Transición que gobierna |
|-------|------|---------|-------------------------|
| `PendingToFirstReminderDays` | `int` | 3 | `Pending → PrimerRecordatorio` |
| `FirstToSecondReminderDays` | `int` | 3 | `PrimerRecordatorio → SegundoRecordatorio` |
| `SecondToDeactivatedDays` | `int` | 3 | `SegundoRecordatorio → Desactivado` |

Fuente: `ISystemSettingsRepository.GetSettingsAsync()` (persistido en MongoDB). El worker **no** modifica esta configuración; solo la lee.

## Configuración nueva (no persistida en BD)

### InvoiceTransitionsWorkerOptions (`Infrastructure/Workers`)

Configuración **operativa** del worker, enlazada desde `IConfiguration` (sección `InvoiceTransitionsWorker`, sobreescribible por variable de entorno):

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `IntervalMinutes` | `int` | 60 | Frecuencia entre ciclos. Si no se configura, se usa el default y se registra. (FR-001/FR-002) |
| `RunOnStartup` | `bool` | true | Si el primer ciclo se ejecuta inmediatamente al arrancar. |

Validación: `IntervalMinutes` debe ser > 0; si se configura un valor inválido, se cae al default y se registra una advertencia.

## Resultado de un ciclo (en memoria / logging)

No es una entidad persistida; es el resumen estructurado emitido por Serilog al final de cada ciclo (FR-008):

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Timestamp` | `DateTimeOffset` | Inicio del ciclo. |
| `Evaluated` | `int` | Facturas candidatas evaluadas. |
| `Transitioned` | `int` | Facturas que cambiaron de estado. |
| `Errors` | `int` | Facturas que fallaron al procesarse (aisladas, el lote continúa). |
| `DurationMs` | `double` | Duración total del ciclo. |

## Relaciones y flujo de datos

```text
ISystemSettingsRepository ──(InvoiceTransitionsConfig: umbrales de días)──┐
                                                                          ▼
IInvoiceRepository.GetTransitionableAsync ──(candidatos)──► InvoiceTransitionService.TryApplyTransition(invoice, config, now)
                                                                          │ (si true)
                                                                          ▼
                                                   IInvoiceRepository.UpdateAsync(invoice)
                                                                          │
                                                                          ▼
                                                   Serilog: log por transición + resumen de ciclo
```
