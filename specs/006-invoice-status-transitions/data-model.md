# Data Model: invoice-status-transitions

## Entities

### `Invoice` (Actualización)
- **Status**: (Enum) `pending`, `primerrecordatorio`, `segundorecordatorio`, `desactivado`, `pagado`.
- **LastStatusTransitionAt**: DateTime? (Para calcular cuándo fue la última transición y ver si aplica la siguiente). Alternativamente, se usa `CreatedAt` para el primer cálculo.

### `SystemSettings` (Nueva)
- **Id**: ObjectId
- **InvoiceTransitions**: Objeto anidado
  - **PendingToFirstReminderDays**: int
  - **FirstToSecondReminderDays**: int
  - **SecondToDeactivatedDays**: int
- **UpdatedAt**: DateTime

## Validation Rules

- La entidad `Invoice` no puede pasar de `pending` a `segundorecordatorio` (solo estados consecutivos de acuerdo al flujo definido).
- Desde cualquier estado (`pending`, `primerrecordatorio`, `segundorecordatorio`, `desactivado`) se puede pasar a `pagado`.
- No se puede mutar el estado de una factura que ya es `pagado`.

## State Transitions

- `pending` + `PendingToFirstReminderDays` -> `primerrecordatorio`
- `primerrecordatorio` + `FirstToSecondReminderDays` -> `segundorecordatorio`
- `segundorecordatorio` + `SecondToDeactivatedDays` -> `desactivado`
- Cualquier estado (excepto pagado) + Pago -> `pagado`
