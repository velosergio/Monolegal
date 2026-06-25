# Data Model — Email Service Interface (Spec 011)

Esta spec define un **contrato**, no un modelo de persistencia. No introduce entidades nuevas ni cambios de esquema. A continuación se describen los datos que **cruzan** el contrato `IEmailService`.

## Contrato: `IEmailService`

Abstracción de la capa `Application` (`Backend.Application.Abstractions`) que declara las capacidades de notificación por correo del sistema.

| Operación | Parámetros | Retorno | Propósito |
|-----------|-----------|---------|-----------|
| `SendReminderAsync` | `string clientEmail`, `Invoice invoice`, `CancellationToken cancellationToken = default` | `Task` | Enviar un correo de recordatorio de cobro para una factura activa. |
| `SendPaymentConfirmationAsync` | `string clientEmail`, `Invoice invoice`, `CancellationToken cancellationToken = default` | `Task` | Enviar un correo de confirmación cuando una factura pasa a estado pagado. |

### Parámetros de entrada

- **`clientEmail`** (`string`): Dirección de correo del destinatario. El contrato espera un valor no nulo/no vacío; la validación de formato es responsabilidad de la implementación (Spec 3.3) y del invocador.
- **`invoice`** (`Monolegal.Domain.Entities.Invoice`): Entidad de dominio existente (spec 005) usada como fuente de datos para componer el correo. No se modifica dentro del contrato.
- **`cancellationToken`** (`CancellationToken`): Cancelación cooperativa; relevante cuando el worker se apaga.

## Entidad referenciada: `Invoice` (sin cambios)

Definida en `backend/Domain/Entities/Invoice.cs` (spec 005). El contrato la consume como dato de solo lectura. Campos relevantes para componer correos:

| Campo | Tipo | Uso en la composición del correo (Spec 3.3) |
|-------|------|---------------------------------------------|
| `Id` | `string` | Referencia/identificador de la factura en el mensaje. |
| `ClientId` | `string` | Identificación del cliente destinatario. |
| `Amount` | `decimal` | Monto adeudado/pagado a mostrar. |
| `Status` | `InvoiceStatus` | Determina la plantilla/contenido del correo. |
| `RemindersCount` | `int` | Contexto del número de recordatorio (1°, 2°). |
| `LastReminderSentAt` | `DateTime?` | Contexto temporal del último recordatorio. |

> Nota: La actualización de `RemindersCount` / `LastReminderSentAt` tras un envío exitoso (vía `Invoice.RecordReminderSent()`) es responsabilidad del flujo consumidor (Spec 3.3), **no** del contrato `IEmailService`.

## Reglas y restricciones del contrato

- **R-001**: Ambas operaciones son asíncronas (`Task`) — no bloquean al invocador (RF-004).
- **R-002**: El contrato depende solo de tipos de la BCL y de la entidad de dominio `Invoice`; sin dependencias de proveedor de correo (RF-001, RF-006).
- **R-003**: Los fallos de envío se señalan mediante excepciones; el contrato no define un tipo de resultado de error (ver research D4).
- **R-004**: El contrato es sustituible por una implementación falsa en pruebas (Liskov/DIP — CE-002).

## Persistencia

N/A. Esta spec no crea colecciones, índices ni documentos en MongoDB. La persistencia del resultado de envío (éxito/error) corresponde a la Spec 3.3.
