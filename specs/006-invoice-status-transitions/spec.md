# Feature Specification: invoice-status-transitions

**Feature Branch**: `006-invoice-status-transitions`

**Created**: 2026-06-24

**Status**: Activo

**Input**: User description: "### Spec 1.2: InvoiceStatus Transitions..."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Transición Automática de Recordatorios (Priority: P1)

Como sistema, quiero cambiar el estado de las facturas automáticamente con el paso de los días para gestionar el ciclo de cobro.

**Why this priority**: Es el flujo principal de valor del sistema de facturación para asegurar la recuperación de cartera de manera oportuna.

**Independent Test**: Puede ser probado simulando el paso del tiempo (o ejecutando manualmente el proceso de validación) sobre facturas en distintos estados, verificando el cambio de estado correcto.

**Acceptance Scenarios**:

1. **Given** una factura en estado `pending`, **When** transcurre el tiempo configurado (la cantidad de días es definida por el administrador en la vista de configuración), **Then** el estado de la factura cambia a `primerrecordatorio`.
2. **Given** una factura en estado `primerrecordatorio`, **When** transcurre el tiempo configurado, **Then** el estado de la factura cambia a `segundorecordatorio`.
3. **Given** una factura en estado `segundorecordatorio`, **When** transcurre el tiempo configurado sin haberse registrado un pago, **Then** el estado de la factura cambia a `desactivado`.

---

### User Story 2 - Transición a Pagado (Priority: P1)

Como sistema o usuario, quiero poder marcar una factura como pagada desde cualquier estado activo, para detener los recordatorios.

**Why this priority**: Es esencial permitir la confirmación de pagos en cualquier momento del ciclo de vida de la factura.

**Independent Test**: Puede ser probado cambiando el estado de una factura aleatoria a `pagado` y verificando que es aceptado y no se envían más recordatorios.

**Acceptance Scenarios**:

1. **Given** una factura en cualquier estado previo (`pending`, `primerrecordatorio`, `segundorecordatorio`, `desactivado`), **When** se registra un pago (manual o automático), **Then** el estado de la factura cambia a `pagado`.

### Edge Cases

- ¿Qué sucede si se intenta pasar de `pending` a `segundorecordatorio` directamente? (Debería ser rechazado por reglas de dominio).
- ¿Qué sucede si se intenta cambiar el estado de una factura que ya se encuentra en estado `pagado`? (No debería permitirse ninguna transición adicional).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema DEBE permitir la transición de `pending` a `primerrecordatorio` exclusivamente.
- **FR-002**: El sistema DEBE permitir la transición de `primerrecordatorio` a `segundorecordatorio` exclusivamente.
- **FR-003**: El sistema DEBE permitir la transición de `segundorecordatorio` a `desactivado`.
- **FR-004**: El sistema DEBE permitir la transición a `pagado` desde cualquier estado válido anterior.
- **FR-005**: El sistema DEBE rechazar y registrar como error cualquier transición de estado no definida explícitamente en estas reglas.
- **FR-006**: El sistema DEBE proveer un tab en la vista de configuración para que los administradores definan la cantidad de días de espera para cada transición de estado.

### Key Entities

- **Invoice**: Entidad principal que posee un estado (`Status`) el cual sufre las transiciones descritas, y una fecha de creación/vencimiento que dicta el tiempo transcurrido.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de las facturas que cumplan el tiempo de transición cambian de estado correctamente al ejecutarse las reglas de dominio.
- **SC-002**: Se registran 0% de transiciones inválidas de estado en un periodo operativo normal (todas las transiciones indebidas son bloqueadas).

## Assumptions

- Las transiciones basadas en tiempo se evalúan a través de un proceso en background (worker/job) que corre periódicamente o al momento de consultar.
- Existe un servicio o lógica de dominio que valida las reglas de transición antes de aplicar cualquier cambio a la persistencia.
