# Especificación de Funcionalidad: Endpoints API de Facturas

**Feature Branch**: `009-invoice-api-endpoints`

**Created**: 2026-06-25

**Status**: Draft

**Input**: Descripción del usuario (Spec 2.x): Endpoints HTTP para listar facturas con filtro y paginación (`GET /api/invoices`), consultar detalle (`GET /api/invoices/{id}`), transicionar estado (`POST /api/invoices/transition/{id}`) y obtener estadísticas de dashboard (`GET /api/invoices/stats`).

## Clarifications

### Session 2026-06-25

- Q: ¿Cómo se manejan los parámetros de paginación inválidos (`page`/`pageSize` con cero, negativos o no numéricos)? → A: Rechazar con `400 Bad Request` y mensaje de error claro (sin aplicar defaults silenciosamente cuando el valor está presente pero es inválido).
- Q: ¿Cuál es el tope máximo de `pageSize`? → A: Máximo `pageSize = 50`; una solicitud con `pageSize` mayor se rechaza con `400 Bad Request` (protege el presupuesto de rendimiento de ≤200 ms y el principio de "sin queries sin límite").
- Q: ¿Cuál es el orden por defecto del listado? → A: `createdAt` descendente (facturas más recientes primero), garantizando paginación estable y determinística.
- Q: ¿Cómo se responde a un identificador con formato inválido en detalle/transición? → A: `404 Not Found` (uniforme con "no existe"); no se distingue formato malformado de identificador bien formado pero inexistente.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Listar facturas con filtro y paginación (Priority: P1)

Como administrador del panel, necesito obtener una lista paginada de facturas, opcionalmente filtrada por estado, para visualizar y gestionar la cartera de cobros de forma organizada.

**Why this priority**: Es la vista de entrada del panel administrativo de facturación; sin ella el usuario no puede ver ni operar sobre las facturas. Habilita el resto de las acciones (ver detalle, transicionar).

**Independent Test**: Se puede probar de forma aislada insertando facturas en estados mixtos y verificando que la consulta devuelve la página solicitada, el total correcto y, cuando se aplica el filtro de estado, únicamente facturas en ese estado.

**Acceptance Scenarios**:

1. **Given** existen facturas almacenadas, **When** se solicita `GET /api/invoices`, **Then** se devuelve estado `200 OK` con una estructura que contiene la lista de facturas (`data`), el total de coincidencias (`total`) y el tamaño de página (`pageSize`).
2. **Given** existen facturas en estados mixtos, **When** se solicita `GET /api/invoices?status=primerrecordatorio`, **Then** se devuelven únicamente las facturas en estado `primerrecordatorio` y el `total` refleja la cantidad de facturas que cumplen ese filtro.
3. **Given** existen más facturas que el tamaño de página, **When** se solicita `GET /api/invoices?page=1&pageSize=10`, **Then** se devuelven como máximo 10 facturas correspondientes a la primera página y `total` refleja el número completo de coincidencias (no solo las de la página).
4. **Given** cada factura de la lista, **When** se inspecciona su representación, **Then** incluye al menos `id`, `clientId`, `amount`, `status` y `createdAt`.

---

### User Story 2 - Consultar el detalle de una factura (Priority: P1)

Como administrador, necesito consultar la información completa de una factura por su identificador, para revisar sus datos antes de tomar una acción.

**Why this priority**: El detalle es la base para verificar una factura concreta y precede a cualquier transición manual de estado.

**Independent Test**: Se puede probar insertando una factura conocida y verificando que la consulta por su `id` devuelve el objeto completo, y que un `id` inexistente devuelve `404`.

**Acceptance Scenarios**:

1. **Given** un identificador de factura válido y existente, **When** se solicita `GET /api/invoices/{id}`, **Then** se devuelve estado `200 OK` con el objeto completo de la factura.
2. **Given** un identificador que no corresponde a ninguna factura, **When** se solicita `GET /api/invoices/{id}`, **Then** se devuelve estado `404 Not Found`.

---

### User Story 3 - Transicionar el estado de una factura (Priority: P1)

Como administrador, necesito cambiar el estado de una factura a un nuevo estado válido, para reflejar manualmente el avance del ciclo de cobro o el registro de un pago.

**Why this priority**: Es la acción de escritura central del panel; permite que el operador intervenga en el ciclo de vida de la factura respetando las reglas de transición del dominio (spec 006).

**Independent Test**: Se puede probar insertando una factura en un estado conocido, solicitando una transición válida y verificando el cambio persistido, y solicitando una transición no permitida y verificando el rechazo con `400`.

**Acceptance Scenarios**:

1. **Given** una factura existente en un estado que permite la transición solicitada, **When** se solicita `POST /api/invoices/transition/{id}` con `{ "newStatus": "segundorecordatorio" }`, **Then** el estado se actualiza en la base de datos y se devuelve estado `200 OK` con la factura actualizada.
2. **Given** una factura existente, **When** se solicita una transición no permitida por las reglas de dominio, **Then** se devuelve estado `400 Bad Request` y el estado de la factura no se modifica.
3. **Given** un identificador que no corresponde a ninguna factura, **When** se solicita una transición, **Then** se devuelve estado `404 Not Found`.

---

### User Story 4 - Obtener estadísticas para el dashboard (Priority: P2)

Como administrador, necesito obtener métricas agregadas de las facturas (total, conteo por estado y conteo por cliente), para tener una visión general del estado de la cartera en el dashboard.

**Why this priority**: Aporta valor de visión global, pero depende de la existencia de datos de facturas y no bloquea las operaciones básicas de listado, detalle y transición.

**Independent Test**: Se puede probar insertando un conjunto conocido de facturas en distintos estados y de distintos clientes, y verificando que los agregados devueltos coinciden con los conteos esperados.

**Acceptance Scenarios**:

1. **Given** existen facturas en la base de datos, **When** se solicita `GET /api/invoices/stats`, **Then** se devuelve estado `200 OK` con el total de facturas (`totalInvoices`), el conteo agrupado por estado (`byStatus`) y el conteo agrupado por cliente (`byClient`).
2. **Given** el conteo por estado, **When** se suman todos los valores de `byStatus`, **Then** la suma es igual a `totalInvoices`.
3. **Given** no existe ninguna factura, **When** se solicita `GET /api/invoices/stats`, **Then** se devuelve `200 OK` con `totalInvoices` en 0 y agregados vacíos (no un error).

---

### Edge Cases

- ¿Qué ocurre al solicitar una página fuera de rango (p. ej. `page` mayor al número de páginas disponibles)? → Se devuelve `200 OK` con una lista `data` vacía y el `total` real de coincidencias.
- ¿Qué ocurre si se envía un valor de `status` que no corresponde a ningún estado válido del dominio? → Se rechaza la petición con `400 Bad Request` (filtro inválido).
- ¿Qué ocurre si `page` o `pageSize` reciben valores inválidos (cero, negativos o no numéricos)? → Se rechaza con `400 Bad Request` y mensaje de error claro. Los defaults solo se aplican cuando el parámetro está ausente (FR-006), no cuando está presente con un valor inválido.
- ¿Qué ocurre si se solicita un `pageSize` mayor al tope permitido (`50`)? → Se rechaza con `400 Bad Request` (no se trunca silenciosamente al máximo).
- ¿Qué ocurre si el cuerpo de la transición omite `newStatus` o envía un estado inexistente? → Se devuelve `400 Bad Request`.
- ¿Qué ocurre cuando se consulta el detalle o se transiciona con un identificador con formato inválido (no es un identificador válido)? → Se devuelve `404 Not Found` de forma uniforme, sin distinguir entre formato malformado e identificador bien formado pero inexistente, nunca un error no controlado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema MUST exponer `GET /api/invoices` que devuelva una lista de facturas junto con el total de coincidencias y el tamaño de página utilizado.
- **FR-002**: El sistema MUST permitir filtrar el listado por estado mediante el parámetro de consulta `status`, devolviendo únicamente las facturas en el estado indicado.
- **FR-003**: El sistema MUST permitir paginar el listado mediante los parámetros de consulta `page` y `pageSize`, devolviendo como máximo `pageSize` facturas por página.
- **FR-003a**: El sistema MUST limitar `pageSize` a un máximo de `50`. Una solicitud con `pageSize` mayor a `50` MUST responder `400 Bad Request`.
- **FR-004**: El sistema MUST calcular `total` como el número completo de facturas que cumplen el filtro aplicado, independientemente de la página devuelta.
- **FR-005**: El sistema MUST incluir, para cada factura del listado, al menos los campos `id`, `clientId`, `amount`, `status` y `createdAt`.
- **FR-005a**: El sistema MUST ordenar el listado por `createdAt` de forma descendente (facturas más recientes primero) de manera determinística, para garantizar una paginación estable.
- **FR-006**: El sistema MUST aplicar valores por defecto de paginación (`page=1`, `pageSize=10`) únicamente cuando `page` o `pageSize` estén ausentes. Si están presentes pero son inválidos (cero, negativos o no numéricos), el sistema MUST responder `400 Bad Request` sin aplicar defaults.
- **FR-007**: El sistema MUST responder con estado `200 OK` ante consultas de listado válidas.
- **FR-008**: El sistema MUST exponer `GET /api/invoices/{id}` que devuelva el objeto completo de la factura solicitada con estado `200 OK` cuando exista.
- **FR-009**: El sistema MUST devolver `404 Not Found` cuando se solicite el detalle de una factura cuyo identificador no exista, incluyendo el caso de identificadores con formato inválido (tratados de forma uniforme como "no encontrado").
- **FR-010**: El sistema MUST exponer `POST /api/invoices/transition/{id}` que reciba el nuevo estado deseado (`newStatus`) en el cuerpo de la petición.
- **FR-011**: El sistema MUST validar la transición solicitada contra las reglas de transición del dominio (definidas en la spec 006) antes de persistirla.
- **FR-012**: El sistema MUST persistir el nuevo estado y devolver `200 OK` con la factura actualizada cuando la transición sea permitida.
- **FR-013**: El sistema MUST devolver `400 Bad Request` y no modificar la factura cuando la transición solicitada no esté permitida por las reglas de dominio.
- **FR-014**: El sistema MUST devolver `404 Not Found` cuando se solicite transicionar una factura cuyo identificador no exista, incluyendo identificadores con formato inválido (tratados de forma uniforme como "no encontrado").
- **FR-015**: El sistema MUST exponer `GET /api/invoices/stats` que devuelva el total de facturas (`totalInvoices`), el conteo agrupado por estado (`byStatus`) y el conteo agrupado por cliente (`byClient`).
- **FR-016**: El sistema MUST garantizar que la suma de los conteos de `byStatus` sea igual a `totalInvoices`.
- **FR-017**: El sistema MUST devolver `400 Bad Request` cuando el parámetro `status` o el campo `newStatus` contengan un valor que no corresponda a un estado válido del dominio.
- **FR-018**: El sistema MUST validar las entradas de los endpoints (parámetros de consulta y cuerpo de la petición) y responder con mensajes de error claros ante entradas inválidas.

### Key Entities *(include if feature involves data)*

- **Factura (Invoice)**: Entidad expuesta por los endpoints. Atributos relevantes para esta funcionalidad: identificador (`id`), identificador de cliente (`clientId`), importe (`amount`), estado del ciclo de vida (`status`) y fecha de creación (`createdAt`). El detalle expone el objeto completo de la factura.
- **Estado de Factura (InvoiceStatus)**: Conjunto cerrado de estados (`pending`, `primerrecordatorio`, `segundorecordatorio`, `desactivado`, `pagado`) usado para filtrar, transicionar y agregar.
- **Resultado Paginado**: Estructura de respuesta del listado compuesta por la colección de facturas (`data`), el total de coincidencias (`total`) y el tamaño de página (`pageSize`).
- **Estadísticas de Facturas**: Estructura de respuesta del dashboard compuesta por `totalInvoices`, `byStatus` (mapa estado → conteo) y `byClient` (mapa cliente → conteo).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de las consultas de listado con filtro de estado devuelven exclusivamente facturas en el estado solicitado.
- **SC-002**: El 100% de las respuestas de listado paginado devuelven como máximo `pageSize` facturas y un `total` que coincide con el número real de coincidencias del filtro.
- **SC-003**: El 100% de las consultas de detalle sobre identificadores existentes devuelven el objeto completo, y el 100% de las consultas sobre identificadores inexistentes devuelven `404`.
- **SC-004**: El 100% de las transiciones permitidas se persisten correctamente y devuelven la factura actualizada; el 100% de las transiciones no permitidas se rechazan con `400` sin modificar la factura.
- **SC-005**: En las estadísticas, la suma de los conteos por estado es igual al total de facturas en el 100% de los casos verificados.
- **SC-006**: Todos los endpoints responden en ≤200 ms bajo carga normal, conforme al presupuesto de rendimiento de la constitución.
- **SC-007**: El 100% de las peticiones con parámetros o cuerpo inválidos reciben una respuesta de error controlada (`400`), sin errores no manejados (`500`).

## Assumptions

- La entidad `Invoice`, el enum `InvoiceStatus` y las reglas de transición ya existen (specs 005 y 006), y el repositorio MongoDB con sus operaciones de consulta y actualización ya está implementado (spec 007); esta funcionalidad solo añade la capa de exposición HTTP (API) sobre lo existente.
- Los valores de estado se intercambian en la API usando las cadenas en minúscula del dominio (`pending`, `primerrecordatorio`, `segundorecordatorio`, `desactivado`, `pagado`), conforme a los ejemplos del input.
- Los valores por defecto de paginación son `page=1` y `pageSize=10` cuando no se especifican, alineados con el ejemplo del input.
- El listado, el detalle y las estadísticas son operaciones de solo lectura; la transición es la única operación de escritura de este conjunto de endpoints.
- La validación de transiciones se delega en la lógica de dominio existente; la API solo traduce un resultado inválido a `400` y uno exitoso a `200`.
- El conteo por cliente (`byClient`) usa el identificador de cliente (`clientId`) como clave de agrupación.
- La autenticación/autorización (JWT Admin-only, conforme a la constitución) aplica a estos endpoints, pero su definición detallada se aborda en una spec de seguridad independiente; aquí se asume protegido.
- La paginación es obligatoria a nivel de listado para respetar el principio de "sin queries sin límite" de la constitución.
