# Especificación de Funcionalidad: Documentación Swagger/OpenAPI

**Feature Branch**: `010-swagger-openapi-docs`

**Created**: 2026-06-25

**Status**: Activo

**Input**: Descripción del usuario (Spec 2.2): Documentación interactiva Swagger/OpenAPI accesible en `/swagger`. Dado que los endpoints están implementados, al acceder a `/swagger` se deben mostrar todos los endpoints documentados, los modelos y DTO visibles, y la funcionalidad "Try it out" operativa.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Descubrir y explorar la API desde la documentación interactiva (Priority: P1)

Como desarrollador (frontend o integrador) o como administrador técnico, necesito acceder a una página de documentación interactiva de la API para conocer qué endpoints existen, qué hacen y cómo invocarlos, sin tener que leer el código fuente ni una colección externa.

**Why this priority**: Es el valor central de esta funcionalidad. Sin la página de documentación accesible, no existe el resto de la entrega. Habilita el consumo y la integración de la API por parte del frontend y de cualquier consumidor técnico.

**Independent Test**: Se puede probar de forma aislada accediendo a la ruta de documentación (`/swagger`) en un entorno con la API en ejecución y verificando que la página carga correctamente y lista la totalidad de los endpoints públicos de la API.

**Acceptance Scenarios**:

1. **Given** la API está en ejecución con sus endpoints implementados, **When** se accede a `/swagger`, **Then** se muestra la página de documentación interactiva sin errores.
2. **Given** la página de documentación está cargada, **When** se inspecciona el listado de operaciones, **Then** aparecen documentados todos los endpoints de facturas (`GET /api/invoices`, `GET /api/invoices/{id}`, `POST /api/invoices/transition/{id}`, `GET /api/invoices/stats`).
3. **Given** la página de documentación está cargada, **When** se revisa cada operación, **Then** cada una muestra su método HTTP, su ruta, una descripción de su propósito y sus parámetros (de ruta, de consulta o de cuerpo según corresponda).

---

### User Story 2 - Comprender los modelos y DTO de entrada y salida (Priority: P1)

Como consumidor de la API, necesito ver la estructura de los modelos y DTO (los objetos de petición y de respuesta) para saber qué datos enviar y qué datos esperar, incluyendo los códigos de estado y las respuestas posibles de cada operación.

**Why this priority**: Conocer los contratos de datos es imprescindible para integrar correctamente; un listado de endpoints sin sus esquemas de datos es insuficiente para construir consumidores fiables.

**Independent Test**: Se puede probar revisando la sección de esquemas/modelos de la página y verificando que se muestran las estructuras de los objetos de factura, del resultado paginado, de las estadísticas y del cuerpo de transición, con sus campos.

**Acceptance Scenarios**:

1. **Given** la página de documentación está cargada, **When** se consulta una operación que devuelve datos, **Then** se muestra el esquema de la respuesta con sus campos y tipos.
2. **Given** la página de documentación está cargada, **When** se consulta la operación de transición, **Then** se muestra el esquema del cuerpo de la petición (`newStatus`) que debe enviarse.
3. **Given** la página de documentación está cargada, **When** se revisan los modelos, **Then** los esquemas de factura, resultado paginado y estadísticas de facturas son visibles con sus campos.
4. **Given** una operación documentada, **When** se revisan sus respuestas, **Then** se listan los códigos de estado posibles (p. ej. `200`, `400`, `404`) y el significado de cada uno.

---

### User Story 3 - Probar los endpoints directamente desde la documentación (Priority: P2)

Como desarrollador, necesito ejecutar peticiones de prueba contra los endpoints reales directamente desde la página de documentación ("Try it out"), para validar el comportamiento sin herramientas externas.

**Why this priority**: Aporta valor de productividad y verificación rápida, pero depende de que el listado de endpoints y los esquemas (P1) ya estén presentes; no es bloqueante para entender la API.

**Independent Test**: Se puede probar abriendo una operación en la página, activando "Try it out", completando los parámetros requeridos, ejecutando la petición y verificando que se recibe y muestra una respuesta real de la API.

**Acceptance Scenarios**:

1. **Given** una operación en la página de documentación, **When** se activa "Try it out", **Then** los campos de parámetros se vuelven editables y aparece la acción para ejecutar la petición.
2. **Given** "Try it out" activado con parámetros válidos, **When** se ejecuta la petición, **Then** la página muestra la respuesta real de la API (código de estado y cuerpo).
3. **Given** una petición ejecutada desde la página, **When** se inspecciona el resultado, **Then** se muestra el comando equivalente (p. ej. la URL invocada) y la respuesta recibida.

---

### Edge Cases

- ¿Qué ocurre al acceder a `/swagger` cuando la API no expone ningún endpoint todavía? → La página carga igualmente y muestra un listado vacío de operaciones, sin error.
- ¿Qué ocurre si se ejecuta "Try it out" sobre un endpoint que requiere un parámetro obligatorio sin proporcionarlo? → La página indica el campo requerido o la API responde con su error de validación controlado (`400`), reflejado en la página.
- ¿Qué ocurre con la disponibilidad de la documentación en el entorno de producción? → La exposición de la documentación en producción se rige por la decisión documentada en Assumptions (ver supuestos sobre entornos).
- ¿Qué ocurre si un endpoint está protegido por autenticación? → La página debe permitir indicar el mecanismo de autenticación necesario para que "Try it out" funcione sobre endpoints protegidos.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema MUST exponer una página de documentación interactiva de la API accesible en la ruta `/swagger`.
- **FR-002**: La página de documentación MUST listar todos los endpoints públicos de la API, incluyendo los endpoints de facturas (`GET /api/invoices`, `GET /api/invoices/{id}`, `POST /api/invoices/transition/{id}`, `GET /api/invoices/stats`).
- **FR-003**: Para cada endpoint, la documentación MUST mostrar su método HTTP, su ruta y una descripción de su propósito.
- **FR-004**: Para cada endpoint, la documentación MUST mostrar sus parámetros aplicables (parámetros de ruta, parámetros de consulta y/o cuerpo de la petición).
- **FR-005**: La documentación MUST mostrar el esquema de los modelos y DTO de entrada y de salida (objeto de factura, resultado paginado, estadísticas de facturas y cuerpo de transición), con sus campos.
- **FR-006**: Para cada operación, la documentación MUST listar los códigos de estado de respuesta posibles (p. ej. `200`, `400`, `404`) con su significado.
- **FR-007**: La documentación MUST ofrecer la funcionalidad "Try it out" que permita editar los parámetros de una operación y ejecutar una petición real contra la API.
- **FR-008**: Al ejecutar una petición mediante "Try it out", la página MUST mostrar la respuesta real de la API (código de estado y cuerpo).
- **FR-009**: El documento de definición de la API (especificación OpenAPI) MUST estar disponible en un punto de acceso conocido y servir como fuente de la página de documentación.
- **FR-010**: La documentación MUST permanecer sincronizada con los endpoints implementados, reflejando automáticamente las operaciones, parámetros y modelos existentes en la API sin requerir mantenimiento manual de un documento separado.
- **FR-011**: La documentación MUST permitir indicar las credenciales o el mecanismo de autenticación requerido, de modo que "Try it out" pueda invocar endpoints protegidos.

### Key Entities *(include if feature involves data)*

- **Documento de Especificación de la API (OpenAPI)**: Descripción estructurada y legible por máquina de la totalidad de la API: sus operaciones, rutas, métodos, parámetros, esquemas de datos y respuestas. Es la fuente que alimenta la página de documentación interactiva.
- **Operación (Endpoint documentado)**: Representa un endpoint expuesto. Atributos relevantes: método HTTP, ruta, descripción, parámetros, cuerpo de petición y respuestas posibles con sus códigos de estado.
- **Esquema/Modelo (DTO)**: Representa la estructura de un objeto de petición o respuesta. Incluye los modelos ya definidos por la API (factura, resultado paginado, estadísticas de facturas, cuerpo de transición) con sus campos.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de los endpoints públicos implementados de la API aparecen documentados en la página interactiva.
- **SC-002**: El 100% de los endpoints documentados muestran método, ruta, descripción y sus parámetros aplicables.
- **SC-003**: El 100% de los modelos y DTO de entrada/salida usados por los endpoints son visibles como esquemas en la documentación.
- **SC-004**: Un consumidor puede ejecutar correctamente una petición de prueba contra un endpoint de lectura desde la página y obtener una respuesta real en el 100% de los endpoints de lectura.
- **SC-005**: Un desarrollador nuevo puede identificar y comprender cómo invocar cualquier endpoint usando únicamente la página de documentación, sin consultar el código fuente, en menos de 5 minutos.
- **SC-006**: La documentación refleja el 100% de los cambios en endpoints y modelos sin intervención manual sobre un documento separado (verificable añadiendo o modificando un endpoint y comprobando que aparece reflejado).

## Assumptions

- Los endpoints de facturas de la fase 2 (spec 009: listar, detalle, transición y estadísticas) ya están implementados; esta funcionalidad solo añade la capa de documentación interactiva sobre lo existente.
- La página de documentación se genera automáticamente a partir de la definición de la API y de los modelos ya existentes, evitando el mantenimiento manual de un documento de especificación separado.
- La ruta de acceso a la documentación es `/swagger`, conforme al input del roadmap.
- La documentación se habilita al menos en los entornos de desarrollo y pruebas. Su exposición en producción se considera una decisión de configuración/seguridad y, por defecto, se asume restringida o deshabilitada en producción salvo decisión contraria del equipo.
- Los endpoints están (o estarán) protegidos por autenticación JWT con rol Admin (conforme a la constitución); la documentación debe contemplar un mecanismo para autorizar las pruebas "Try it out" sobre endpoints protegidos, aunque la definición detallada de la seguridad se aborda en una spec independiente.
- Los nombres de campos y valores de estado mostrados en los esquemas coinciden con los definidos por el dominio y los endpoints existentes (specs 005, 006 y 009).
