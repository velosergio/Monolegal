# Modelo de Datos: Entidad Invoice

**Relacionado con**: [plan.md](./plan.md) | [spec.md](./spec.md)

## Entidad: `Invoice`

Representa una factura dentro del dominio del sistema. Mantiene su propio estado e invariantes.

### Propiedades y Tipos (C#)

| Propiedad | Tipo C# | Visibilidad | Descripción y Reglas de Negocio |
|-----------|---------|-------------|---------------------------------|
| `Id` | `string` | `public get, private set` | Identificador único. Para interoperabilidad con MongoDB, almacenará la representación de un `ObjectId` en cadena de texto. |
| `ClientId` | `string` | `public get, private set` | Identificador del cliente al que pertenece la factura. No puede ser nulo o vacío. |
| `Amount` | `decimal` | `public get, private set` | Monto de la factura. Debe ser estrictamente mayor a 0 (`Amount > 0`). |
| `Status` | `InvoiceStatus` | `public get, private set` | Estado actual del ciclo de la factura. Inicia por defecto en `Draft` o `Pending`. |
| `CreatedAt` | `DateTime` | `public get, private set` | Fecha de creación (UTC). Se asigna automáticamente en el constructor a `DateTime.UtcNow`. |
| `UpdatedAt` | `DateTime` | `public get, private set` | Fecha de última modificación (UTC). Se actualiza automáticamente cada vez que un método mutador modifica el estado de la entidad. |
| `RemindersCount` | `int` | `public get, private set` | Cantidad de recordatorios de cobro enviados. Inicia en 0. |
| `LastReminderSentAt` | `DateTime?` | `public get, private set` | Fecha y hora en la que se envió el último recordatorio. Inicia nulo. |

### Enumerador: `InvoiceStatus`

| Valor | Descripción |
|-------|-------------|
| `Draft` | La factura está en borrador y aún no es procesable. (0) |
| `Pending` | La factura ha sido emitida y está pendiente de pago. (1) |
| `Paid` | El monto de la factura fue pagado completamente. (2) |
| `Overdue` | La factura no fue pagada dentro del tiempo límite establecido. (3) |
| `Cancelled`| La factura ha sido anulada o cancelada. (4) |

### Invariantes y Validaciones de Dominio

1. **Constructor Válido**: Al instanciar, `ClientId` no debe ser nulo o espacios en blanco. Si lo es, debe arrojar una excepción (e.g. `ArgumentException`).
2. **Monto Positivo**: El monto `Amount` provisto en el constructor debe ser `> 0`. De lo contrario, arroja una excepción de validación de dominio.
3. **Control de Auditoría**: Cualquier método que altere propiedades (como `ChangeStatus()` o `RecordReminderSent()`) debe automáticamente actualizar la propiedad `UpdatedAt` a `DateTime.UtcNow`.
4. **Incremento de Recordatorios**: `RemindersCount` solo debe poder incrementarse a través de un método encapsulado (ej. `RecordReminderSent()`), el cual al invocarse también asignará la fecha actual a `LastReminderSentAt`.
