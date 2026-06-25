# Contrato: `IEmailService`

**Capa**: Application — `Backend.Application.Abstractions`
**Archivo**: `backend/Application/Abstractions/IEmailService.cs`
**Tipo**: Interfaz (abstracción). Sin implementación en esta spec (ver Spec 3.3).

## Propósito

Desacoplar a los consumidores (worker de transiciones, futuros endpoints) del proveedor de correo concreto, exponiendo las capacidades de notificación del sistema como un contrato pequeño y cohesivo (ISP).

## Definición (forma esperada)

```csharp
using System.Threading;
using System.Threading.Tasks;
using Monolegal.Domain.Entities;

namespace Backend.Application.Abstractions;

/// <summary>
/// Contrato de notificaciones por correo del sistema. Desacopla a los consumidores
/// del proveedor de correo concreto (SMTP/API), cuya implementación vive en la capa
/// Infrastructure (ver specs/011 + Spec 3.3 del roadmap).
/// Los fallos de envío se señalan mediante excepciones.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Envía un correo de recordatorio de cobro para una factura activa.
    /// </summary>
    /// <param name="clientEmail">Dirección de correo del destinatario (no nula/no vacía).</param>
    /// <param name="invoice">Factura asociada usada para componer el contenido.</param>
    /// <param name="cancellationToken">Cancelación cooperativa.</param>
    Task SendReminderAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Envía un correo de confirmación cuando una factura pasa a estado pagado.
    /// </summary>
    /// <param name="clientEmail">Dirección de correo del destinatario (no nula/no vacía).</param>
    /// <param name="invoice">Factura pagada usada para componer el contenido.</param>
    /// <param name="cancellationToken">Cancelación cooperativa.</param>
    Task SendPaymentConfirmationAsync(string clientEmail, Invoice invoice, CancellationToken cancellationToken = default);
}
```

## Operaciones

### `SendReminderAsync`

| Aspecto | Detalle |
|---------|---------|
| Entrada | `clientEmail: string`, `invoice: Invoice`, `cancellationToken: CancellationToken = default` |
| Salida | `Task` (completa sin valor en caso de éxito) |
| Precondición | `clientEmail` no nulo/no vacío; `invoice` no nulo |
| Éxito | La tarea se completa sin excepción |
| Fallo | La tarea falla con excepción (validación o error del proveedor) — responsabilidad de la implementación |
| Cubre | RF-002, RF-004, US1 |

### `SendPaymentConfirmationAsync`

| Aspecto | Detalle |
|---------|---------|
| Entrada | `clientEmail: string`, `invoice: Invoice`, `cancellationToken: CancellationToken = default` |
| Salida | `Task` (completa sin valor en caso de éxito) |
| Precondición | `clientEmail` no nulo/no vacío; `invoice` no nulo (idealmente en estado pagado) |
| Éxito | La tarea se completa sin excepción |
| Fallo | La tarea falla con excepción — responsabilidad de la implementación |
| Cubre | RF-003, RF-004, US2 |

## Invariantes del contrato

- **C-001**: Ambas operaciones devuelven `Task` (asíncronas, no bloqueantes) — RF-004.
- **C-002**: El contrato no referencia ningún proveedor de correo concreto; solo BCL + `Invoice` — RF-001, RF-006.
- **C-003**: El contrato es registrable/inyectable vía DI por constructor (el registro concreto se hace en la Spec 3.3) — RF-005.
- **C-004**: El contrato es sustituible por un doble de prueba (fake/mock) sin romper a los consumidores — Liskov/DIP, CE-002.

## Fuera de alcance (diferido a Spec 3.3)

- Implementación concreta (proveedor SMTP/API, plantillas de correo).
- Registro en el contenedor de DI.
- Persistencia del resultado de envío (éxito/error) y logging estructurado del envío.
- Actualización de `RemindersCount` / `LastReminderSentAt` tras el envío.
