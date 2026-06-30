# Feature Specification: Tests de Integración de la API

**Feature Branch**: `021-integration-tests-api`

**Created**: 2026-06-29

**Status**: Activo

**Input**: User description: "Spec 5.2: Integration Tests - API. GIVEN endpoints implementados, WHEN se ejecutan tests, THEN: GET /api/invoices retorna 200, filtro por status funciona, POST transition valida estado permitido, 404 en ID no existente, WebApplicationFactory para setup."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Verificar el contrato de listado de facturas (Priority: P1)

Como integrante del equipo de desarrollo, necesito que el endpoint de listado de facturas (`GET /api/invoices`) esté cubierto por pruebas de integración que arranquen la aplicación real, de modo que cualquier regresión en el contrato HTTP (código de estado, estructura de respuesta o filtro por estado) sea detectada de inmediato antes de llegar a producción.

**Why this priority**: El listado es la puerta de entrada del panel administrativo y la operación de lectura más usada. Verificar de extremo a extremo que responde correctamente y respeta el filtro por estado protege el flujo principal del usuario y valida que la composición real de capas (Api → Application → Infrastructure → MongoDB) funciona en conjunto, algo que las pruebas unitarias no cubren.

**Independent Test**: Se puede probar de forma aislada levantando la aplicación con una base de datos efímera, sembrando facturas en estados mixtos y verificando que la respuesta tiene estado `200 OK`, la estructura esperada y, al aplicar el filtro de estado, únicamente las facturas en ese estado.

**Acceptance Scenarios**:

1. **Given** la aplicación arrancada con facturas almacenadas, **When** se solicita `GET /api/invoices`, **Then** la respuesta tiene estado `200 OK` y un cuerpo con la lista de facturas, el total de coincidencias y el tamaño de página.
2. **Given** facturas en estados mixtos, **When** se solicita `GET /api/invoices?status=primerrecordatorio`, **Then** la respuesta contiene exclusivamente facturas en estado `primerrecordatorio` y el total refleja únicamente esas coincidencias.
3. **Given** un valor de `status` que no corresponde a ningún estado del dominio, **When** se solicita el listado con ese filtro, **Then** la respuesta tiene estado `400 Bad Request`.
4. **Given** parámetros de paginación inválidos (cero, negativos o no numéricos), **When** se solicita el listado, **Then** la respuesta tiene estado `400 Bad Request`.

---

### User Story 2 - Verificar las respuestas de error por identificador inexistente (Priority: P1)

Como integrante del equipo de desarrollo, necesito que las operaciones por identificador (detalle y transición) estén cubiertas por pruebas de integración que confirmen la respuesta `404 Not Found` cuando el identificador no existe o tiene formato inválido, de modo que el manejo de errores del API permanezca consistente y nunca degrade en un error no controlado.

**Why this priority**: El manejo uniforme de "no encontrado" es un contrato explícito del API (spec 009) y un punto frecuente de regresión cuando cambia la capa de persistencia. Verificarlo de extremo a extremo garantiza que el operador recibe siempre una respuesta controlada y previsible.

**Independent Test**: Se puede probar levantando la aplicación con una base vacía y solicitando el detalle y la transición sobre un identificador inexistente y sobre un identificador con formato inválido, verificando que ambos casos devuelven `404`.

**Acceptance Scenarios**:

1. **Given** la aplicación arrancada sin la factura solicitada, **When** se solicita `GET /api/invoices/{id}` con un identificador inexistente, **Then** la respuesta tiene estado `404 Not Found`.
2. **Given** un identificador con formato inválido, **When** se solicita el detalle, **Then** la respuesta tiene estado `404 Not Found` (tratamiento uniforme con "no existe"), sin error no controlado.
3. **Given** la aplicación arrancada sin la factura solicitada, **When** se solicita `POST /api/invoices/transition/{id}` sobre un identificador inexistente, **Then** la respuesta tiene estado `404 Not Found`.

---

### User Story 3 - Verificar la validación de transiciones de estado (Priority: P1)

Como integrante del equipo de desarrollo, necesito que el endpoint de transición (`POST /api/invoices/transition/{id}`) esté cubierto por pruebas de integración que verifiquen tanto las transiciones permitidas como las prohibidas, de modo que las reglas de dominio se respeten también a través de la capa HTTP y una transición inválida nunca persista un cambio.

**Why this priority**: La transición es la única operación de escritura del conjunto principal de endpoints y la de mayor riesgo de negocio. Verificar de extremo a extremo que una transición permitida persiste y que una prohibida se rechaza sin alterar el estado garantiza la integridad del ciclo de vida de la factura.

**Independent Test**: Se puede probar sembrando una factura en un estado conocido, solicitando una transición permitida y verificando el cambio persistido (`200 OK`), y solicitando una transición no permitida y verificando el rechazo (`400`) sin modificación del estado.

**Acceptance Scenarios**:

1. **Given** una factura en un estado que permite la transición solicitada, **When** se solicita la transición con el nuevo estado válido, **Then** la respuesta tiene estado `200 OK`, devuelve la factura actualizada y el nuevo estado queda persistido.
2. **Given** una factura existente, **When** se solicita una transición no permitida por las reglas de dominio, **Then** la respuesta tiene estado `400 Bad Request` y el estado de la factura no cambia.
3. **Given** un cuerpo de petición que omite el nuevo estado o envía un estado inexistente, **When** se solicita la transición, **Then** la respuesta tiene estado `400 Bad Request`.

---

### User Story 4 - Disponer de una base de pruebas de integración reutilizable (Priority: P2)

Como responsable de la calidad del proyecto, necesito una infraestructura de pruebas de integración que arranque la aplicación real en memoria y la conecte a una base de datos aislada por ejecución, de modo que las pruebas sean deterministas, repetibles y no contaminen datos entre clases ni entre la suite y los entornos reales.

**Why this priority**: La infraestructura de arranque (host web en pruebas + base efímera) es habilitadora del resto de historias. Es P2 porque su valor se materializa a través de las pruebas de contrato (P1) que la consumen, pero su correcta implementación condiciona la fiabilidad de toda la suite.

**Independent Test**: Se puede verificar ejecutando la suite de integración dos veces seguidas y comprobando que el resultado es idéntico, que cada clase de prueba parte de un estado de datos limpio y que ninguna prueba depende del orden de ejecución.

**Acceptance Scenarios**:

1. **Given** la suite de integración del API, **When** se ejecuta, **Then** la aplicación se arranca a través de una fábrica de aplicación web en memoria, sin necesidad de desplegar el servicio manualmente.
2. **Given** dos clases de prueba distintas, **When** se ejecutan en la misma corrida, **Then** cada una opera sobre datos aislados y no observa los datos creados por la otra.
3. **Given** la suite completa, **When** se ejecuta de forma repetida, **Then** produce el mismo resultado (sin pruebas inestables dependientes del orden o de datos residuales).

---

### Edge Cases

- ¿Qué ocurre al solicitar una página fuera de rango (mayor al número de páginas disponibles)? El listado debe responder `200 OK` con una lista vacía y el total real de coincidencias.
- ¿Qué ocurre al solicitar `GET /api/invoices/stats` sin facturas en la base? Debe responder `200 OK` con total en cero y agregados vacíos, no un error.
- ¿Cómo se garantiza que las pruebas no requieran un MongoDB desplegado manualmente en cada máquina/CI? La dependencia de base de datos debe estar documentada y la suite debe fallar con un mensaje claro si la base no está disponible, en lugar de fallar de forma opaca.
- ¿Qué ocurre con la autenticación en las pruebas? La configuración de prueba debe permitir ejercitar los endpoints protegidos sin acoplar las pruebas a credenciales de producción.
- ¿Cómo se evita la contaminación de datos entre pruebas que comparten host? Cada clase/ejecución debe partir de un estado de datos limpio y aislado.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La suite DEBE arrancar la aplicación API real en memoria mediante una fábrica de aplicación web de pruebas, sin requerir un despliegue manual del servicio.
- **FR-002**: La suite DEBE ejercitar los endpoints a través de peticiones HTTP reales contra la aplicación arrancada, verificando el código de estado y el cuerpo de la respuesta como lo haría un cliente externo.
- **FR-003**: La suite DEBE verificar que `GET /api/invoices` responde `200 OK` y devuelve la estructura de listado esperada (lista de facturas, total de coincidencias y tamaño de página).
- **FR-004**: La suite DEBE verificar que el filtro por estado en `GET /api/invoices?status=...` devuelve únicamente facturas en el estado solicitado y un total coherente con ese filtro.
- **FR-005**: La suite DEBE verificar que un valor de `status` inválido y que parámetros de paginación inválidos (cero, negativos o no numéricos) producen `400 Bad Request`.
- **FR-006**: La suite DEBE verificar que `GET /api/invoices/{id}` devuelve `200 OK` con el objeto completo para un identificador existente y `404 Not Found` para un identificador inexistente.
- **FR-007**: La suite DEBE verificar que un identificador con formato inválido se trata de forma uniforme como `404 Not Found`, sin producir un error no controlado.
- **FR-008**: La suite DEBE verificar que `POST /api/invoices/transition/{id}` aplica una transición permitida, responde `200 OK` con la factura actualizada y persiste el nuevo estado.
- **FR-009**: La suite DEBE verificar que una transición no permitida responde `400 Bad Request` y deja el estado de la factura sin cambios.
- **FR-010**: La suite DEBE verificar que una petición de transición con cuerpo inválido (estado ausente o inexistente) responde `400 Bad Request`.
- **FR-011**: La suite DEBE verificar que `POST /api/invoices/transition/{id}` sobre un identificador inexistente responde `404 Not Found`.
- **FR-012**: La suite DEBE ejecutarse contra datos aislados por clase/ejecución, partiendo de un estado limpio y determinista para evitar contaminación entre pruebas.
- **FR-013**: La suite DEBE ser repetible y no depender del orden de ejecución de las pruebas ni de datos residuales de corridas anteriores.
- **FR-014**: La suite DEBE permitir ejercitar los endpoints protegidos por autenticación sin acoplar las pruebas a credenciales de producción.
- **FR-015**: La suite DEBE fallar con un mensaje claro y accionable cuando una dependencia requerida (p. ej. la base de datos) no esté disponible, en lugar de fallar de forma opaca.
- **FR-016**: La ejecución de la suite DEBE producir un resultado consumible por el pipeline de CI (PASS/FAIL por prueba) e integrarse al gate de calidad existente.
- **FR-017**: La suite NO DEBE contener pruebas omitidas o ignoradas (sin `[Ignore]`/skips), conforme al workflow de calidad de la constitución.

### Key Entities *(include if feature involves data)*

- **Endpoint de Facturas (API)**: Superficie HTTP bajo prueba (`GET /api/invoices`, `GET /api/invoices/{id}`, `POST /api/invoices/transition/{id}`, `GET /api/invoices/stats`); el sujeto principal de las pruebas de integración.
- **Factura (Invoice)**: Entidad sembrada como dato de prueba y devuelta por los endpoints; sus campos (`id`, `clientId`, `amount`, `status`, `createdAt`) son verificados en las aserciones de contrato.
- **Estado de Factura (InvoiceStatus)**: Conjunto cerrado de estados usado para sembrar datos, filtrar el listado y ejercitar transiciones permitidas/prohibidas.
- **Host de Aplicación de Pruebas**: Instancia en memoria de la aplicación que compone todas las capas reales y expone los endpoints a las pruebas mediante un cliente HTTP.
- **Base de Datos Aislada**: Almacenamiento efímero por clase/ejecución que garantiza el aislamiento de datos y el determinismo de las pruebas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de los endpoints del conjunto principal de facturas (listado, detalle, transición) tiene al menos una prueba de integración que verifica su caso de éxito.
- **SC-002**: El 100% de las rutas de error definidas en el contrato (estado/paginación inválidos → `400`, identificador inexistente o con formato inválido → `404`, transición prohibida → `400`) tiene al menos una prueba de integración que la verifica.
- **SC-003**: El 100% de las transiciones permitidas verificadas persiste el nuevo estado, y el 100% de las prohibidas verificadas deja el estado sin cambios.
- **SC-004**: La suite de integración se ejecuta de forma repetida con resultado idéntico (cero pruebas inestables) en al menos dos corridas consecutivas.
- **SC-005**: La suite de integración termina con cero pruebas fallidas y cero pruebas omitidas en la ejecución de CI.

## Assumptions

- Los endpoints del API de facturas ya existen e implementan el contrato definido en la spec 009; esta spec añade su cobertura de integración, no modifica el comportamiento del API.
- Ya existe una base de infraestructura de pruebas de integración en el proyecto (fábrica de aplicación web en memoria y fixture de base de datos efímera con nombre único por instancia); esta spec consolida y completa la cobertura de los endpoints de facturas sobre esa base.
- Las pruebas de integración requieren una instancia de MongoDB en ejecución, alineada con el patrón existente (`MONGODB_URI` con default de desarrollo); su provisión en CI se asume disponible.
- Los valores de estado se intercambian en la API usando las cadenas en minúscula del dominio (`pending`, `primerrecordatorio`, `segundorecordatorio`, `desactivado`, `pagado`), conforme a la spec 009.
- El alcance de esta spec se limita a las pruebas de integración de la capa API de facturas; la cobertura de componentes frontend (spec 5.3) y los flujos E2E (spec 5.4) se abordan en specs separadas.
- La suite se ejecuta en el mismo runtime objetivo del backend (.NET 10) y se integra al gate de CI existente, conforme al Principio IV de la constitución.
