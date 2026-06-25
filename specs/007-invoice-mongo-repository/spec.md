# Especificación de Funcionalidad: Repositorio MongoDB de Facturas

**Feature Branch**: `007-invoice-mongo-repository`

**Created**: 2026-06-24

**Status**: Activo

**Input**: Descripción del usuario (Spec 1.3): "GIVEN entidad Invoice en dominio, WHEN se implementa repositorio, THEN debe soportar GetByStatusAsync(InvoiceStatus), GetByClientIdAsync(clientId), UpdateStatusAsync(id, newStatus), InsertAsync(invoice), e índices en Status y ClientId."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consultar facturas por estado (Priority: P1)

Como sistema (worker de transiciones / panel administrativo), necesito obtener todas las facturas que se encuentran en un estado determinado, para poder procesarlas en lote o mostrarlas filtradas al usuario.

**Why this priority**: Es la consulta central que habilita el flujo crítico de transiciones de estado y el filtrado en la interfaz administrativa. Sin ella, el resto de capacidades de gestión de facturas carecen de fuente de datos.

**Independent Test**: Se puede probar de forma aislada insertando facturas en distintos estados y verificando que la consulta por un estado dado devuelve exactamente las facturas de ese estado y ninguna otra.

**Acceptance Scenarios**:

1. **Given** existen facturas en estados mixtos (p. ej. Pendiente, PrimerRecordatorio, Pagada), **When** se solicitan las facturas con estado "Pendiente", **Then** se devuelven únicamente las facturas en estado "Pendiente".
2. **Given** no existe ninguna factura en un estado consultado, **When** se solicita ese estado, **Then** se devuelve una colección vacía (no un error).

---

### User Story 2 - Consultar facturas por cliente (Priority: P1)

Como sistema, necesito obtener todas las facturas asociadas a un cliente identificado por su `clientId`, para presentar el historial de facturación de ese cliente y soportar operaciones específicas por cliente.

**Why this priority**: La trazabilidad por cliente es un requisito de negocio fundamental; la mayoría de vistas y reportes parten del cliente.

**Independent Test**: Se puede probar insertando facturas de varios clientes y verificando que la consulta por un `clientId` devuelve solo las facturas de ese cliente.

**Acceptance Scenarios**:

1. **Given** existen facturas de varios clientes, **When** se solicitan las facturas del cliente "C-123", **Then** se devuelven únicamente las facturas cuyo `clientId` es "C-123".
2. **Given** un `clientId` sin facturas asociadas, **When** se consulta ese cliente, **Then** se devuelve una colección vacía.

---

### User Story 3 - Cambiar el estado de una factura (Priority: P1)

Como sistema, necesito actualizar el estado de una factura identificada por su `id` a un nuevo estado, sin tener que reescribir el documento completo, para registrar de forma fiable las transiciones de ciclo de vida.

**Why this priority**: Las transiciones de estado son el corazón del proceso de cobro automatizado; deben ser precisas y registrar cuándo ocurrieron.

**Independent Test**: Se puede probar insertando una factura, cambiando su estado y verificando que, al releerla, refleja el nuevo estado y la marca temporal de actualización.

**Acceptance Scenarios**:

1. **Given** una factura existente en estado "Pendiente", **When** se cambia su estado a "PrimerRecordatorio", **Then** la factura queda en estado "PrimerRecordatorio" y se registra el momento del cambio.
2. **Given** un `id` que no corresponde a ninguna factura, **When** se intenta cambiar su estado, **Then** ningún documento es modificado y la operación no produce efectos secundarios.

---

### User Story 4 - Crear una nueva factura (Priority: P1)

Como sistema, necesito persistir una nueva factura, para que quede disponible para consultas y transiciones posteriores.

**Why this priority**: Es el punto de entrada de datos; sin creación no hay facturas que consultar ni transicionar.

**Independent Test**: Se puede probar creando una factura y verificando que posteriormente es recuperable por su identificador, por estado y por cliente.

**Acceptance Scenarios**:

1. **Given** una factura válida nueva, **When** se persiste, **Then** queda almacenada y recuperable por su identificador.
2. **Given** una factura recién creada, **When** se consulta por su estado y por su cliente, **Then** aparece en ambos resultados.

---

### User Story 5 - Rendimiento de consultas frecuentes (Priority: P2)

Como operador del sistema, necesito que las consultas por estado y por cliente sigan siendo rápidas a medida que crece el volumen de facturas, para mantener una experiencia fluida en el panel y en el worker.

**Why this priority**: Garantiza escalabilidad; es un soporte de las historias P1 pero no bloquea su funcionalidad básica con volúmenes pequeños.

**Independent Test**: Se puede verificar comprobando que existen índices sobre los campos de estado y cliente y que las consultas correspondientes los aprovechan.

**Acceptance Scenarios**:

1. **Given** un volumen elevado de facturas, **When** se consulta por estado o por cliente, **Then** el tiempo de respuesta se mantiene dentro del presupuesto definido gracias a los índices sobre esos campos.

---

### Edge Cases

- ¿Qué ocurre cuando se consulta por un estado o cliente sin coincidencias? → Se devuelve una colección vacía, nunca un error.
- ¿Qué ocurre cuando se intenta cambiar el estado de una factura inexistente? → No se modifica ningún documento y no se generan efectos secundarios.
- ¿Qué ocurre si los índices ya existen al inicializar el sistema? → La creación de índices es idempotente y no falla por duplicado.
- ¿Cómo se comporta el sistema si la conexión a la base de datos no está disponible durante una operación? → La operación propaga el fallo de infraestructura sin corromper datos parcialmente.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema MUST permitir obtener todas las facturas que se encuentran en un estado dado, devolviendo una colección (posiblemente vacía).
- **FR-002**: El sistema MUST permitir obtener todas las facturas asociadas a un identificador de cliente dado, devolviendo una colección (posiblemente vacía).
- **FR-003**: El sistema MUST permitir cambiar el estado de una factura identificada por su `id`, actualizando únicamente los campos relacionados con el estado y su marca temporal, sin reescribir el resto del documento.
- **FR-004**: El sistema MUST permitir crear (insertar) una nueva factura de forma que quede disponible para consultas posteriores.
- **FR-005**: El sistema MUST mantener un índice sobre el campo de estado de la factura para acelerar las consultas por estado.
- **FR-006**: El sistema MUST mantener un índice sobre el campo de identificador de cliente para acelerar las consultas por cliente.
- **FR-007**: El sistema MUST registrar el momento en que se produce un cambio de estado de una factura.
- **FR-008**: El sistema MUST devolver colecciones vacías (no errores) cuando una consulta por estado o por cliente no tenga coincidencias.
- **FR-009**: El sistema MUST garantizar que un intento de cambio de estado sobre un `id` inexistente no modifique ningún documento.
- **FR-010**: El sistema MUST garantizar que la creación de los índices sea idempotente, sin fallar si los índices ya existen.
- **FR-011**: El acceso a datos de facturas MUST permanecer encapsulado en la capa de infraestructura, exponiendo el contrato a través de una abstracción del dominio, de modo que un cambio del motor de persistencia no se propague a capas superiores.

### Key Entities *(include if feature involves data)*

- **Factura (Invoice)**: Representa una factura del sistema. Atributos relevantes para esta funcionalidad: identificador único, identificador de cliente (`clientId`), estado del ciclo de vida (`status`), y marcas temporales de creación, actualización y última transición de estado.
- **Estado de Factura (InvoiceStatus)**: Conjunto cerrado de estados del ciclo de vida de la factura (p. ej. Pendiente, PrimerRecordatorio, SegundoRecordatorio, Pagada, y demás estados terminales) sobre los que operan las consultas y transiciones.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de las consultas por estado devuelven exclusivamente facturas en el estado solicitado, verificado mediante pruebas de contrato del repositorio.
- **SC-002**: El 100% de las consultas por cliente devuelven exclusivamente facturas del cliente solicitado.
- **SC-003**: Tras un cambio de estado, el 100% de las facturas afectadas reflejan el nuevo estado y una marca temporal de transición actualizada al releerlas.
- **SC-004**: Un intento de cambio de estado sobre un identificador inexistente modifica 0 documentos.
- **SC-005**: Las consultas por estado y por cliente se resuelven en ≤200 ms bajo carga normal, apoyándose en índices sobre esos campos.
- **SC-006**: La inicialización del sistema crea los índices requeridos sobre estado y cliente sin error, incluso ejecutándose repetidamente.
- **SC-007**: Toda factura recién creada es recuperable por identificador, por estado y por cliente inmediatamente tras su persistencia.

## Assumptions

- La entidad `Invoice` y el enum `InvoiceStatus` ya existen en la capa de dominio (entregados en specs previas 005 y 006); esta funcionalidad solo añade el acceso a datos.
- El contrato del repositorio se expone mediante la interfaz `IInvoiceRepository` del dominio; la implementación concreta reside en la capa de infraestructura, conforme a Arquitectura Limpia (Principio I).
- La operación de creación descrita como `InsertAsync(invoice)` en el input se materializa sobre el método de inserción del contrato del repositorio; si el contrato existente usa el nombre `AddAsync`, se conserva ese nombre por coherencia con el código ya implementado, manteniendo la semántica de inserción.
- Los índices sobre `Status` y `ClientId` son índices simples no únicos; un mismo cliente puede tener múltiples facturas y múltiples facturas pueden compartir estado.
- La creación de índices se realiza una vez durante la inicialización de la aplicación (arranque), de forma idempotente.
- Las colecciones devueltas no aplican paginación en este nivel; la paginación, cuando sea necesaria, se resolverá en capas superiores conforme al presupuesto de rendimiento de la constitución.
- El presupuesto de rendimiento de ≤200 ms se evalúa bajo condiciones de carga normal definidas por la constitución del proyecto.
