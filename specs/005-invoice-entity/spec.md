# Especificación de Funcionalidad: Entidad Invoice (Dominio)

**Rama de Funcionalidad**: `005-invoice-entity`

**Creado**: 2026-06-24

**Estado**: Activo

**Entrada**: User description: "## 🏗️ Fase 1: Domain & Data Layer ... "

## Escenarios de Usuario y Pruebas *(obligatorio)*

### Historia de Usuario 1 - Creación de Entidad Invoice (Prioridad: P1)

Como sistema de gestión, necesito que la entidad `Invoice` esté definida en la capa de dominio con todos los campos requeridos para poder almacenar, rastrear y modificar el estado de las facturas.

**Por qué esta prioridad**: Es la estructura de datos fundacional para todo el módulo de facturación; sin ella no se pueden crear lógicas de negocio ni endpoints.

**Prueba Independiente**: Se puede probar unitariamente verificando la creación de la entidad en memoria, la inicialización de sus propiedades por defecto y validando reglas de dominio.

**Escenarios de Aceptación**:

1. **Dado** un requerimiento para crear una factura, **Cuando** se instancia la entidad `Invoice`, **Entonces** se debe asignar correctamente el `ClientId` y el `Amount`.
2. **Dado** una nueva factura inicializada, **Cuando** se verifica su estado inicial, **Entonces** `CreatedAt` y `UpdatedAt` deben ser la fecha/hora actual, y `RemindersCount` debe ser 0.

---

### Casos Límite

- ¿Qué ocurre al inicializar la entidad con un monto (`Amount`) negativo o nulo?
- ¿Qué ocurre al intentar modificar el `Status` a un valor no contemplado en el enumerador?

## Requisitos *(obligatorio)*

### Requisitos Funcionales

- **RF-001**: El sistema DEBE definir el modelo base para `Invoice` con las propiedades estructurales solicitadas.
- **RF-002**: La entidad DEBE soportar los siguientes campos y tipos: `Id` (Identificador único), `ClientId` (Texto), `Amount` (Moneda/Decimal), `Status` (Estado Enumerado), `CreatedAt` (Fecha y Hora), `UpdatedAt` (Fecha y Hora), `RemindersCount` (Entero), `LastReminderSentAt` (Fecha y Hora opcional).
- **RF-003**: El enumerador `InvoiceStatus` DEBE incluir los siguientes estados del ciclo de vida de la factura: `Draft`, `Pending`, `Paid`, `Overdue`, `Cancelled`.
- **RF-004**: El sistema DEBE mantener consistencia en la auditoría, actualizando `UpdatedAt` cuando propiedades relevantes de la factura cambien.

### Entidades Clave

- **Invoice**: Entidad de dominio primaria. Registra detalles del cobro a un cliente, el monto y los recordatorios enviados.

## Criterios de Éxito *(obligatorio)*

### Resultados Medibles

- **CE-001**: 100% de las propiedades definidas en el modelo cuentan con validaciones de tipo correctas e independientes de implementaciones externas.
- **CE-002**: Las pruebas unitarias de la entidad alcanzan un 100% de cobertura verificando la asignación de campos obligatorios y opcionales.
- **CE-003**: Los errores por inicialización con datos inválidos son reportados consistentemente en menos del 100% de los casos antes de persistir datos.

## Suposiciones

- El identificador `Id` asume persistencia compatible con bases de datos documentales o sistemas que generen identificadores únicos alfanuméricos.
- La lógica de incremento de `RemindersCount` se implementará mediante métodos de la propia entidad en el dominio, no por modificación directa de la propiedad.
- Se asume que el monto (`Amount`) debe ser siempre mayor o igual a cero.
