# Feature Specification: Tests Unitarios del Dominio

**Feature Branch**: `020-tests-unitarios-dominio`

**Created**: 2026-06-29

**Status**: Activo

**Input**: User description: "Spec 5.1: Unit Tests - Domain. GIVEN entidades de dominio, WHEN se ejecutan tests, THEN cobertura: InvoiceStatus transitions valid/invalid (xUnit), Invoice creation con validaciones, Shouldly para legibilidad, mínimo 85% cobertura."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Verificar transiciones de estado válidas e inválidas (Priority: P1)

Como integrante del equipo de desarrollo, necesito que las reglas de transición de estado de una factura estén cubiertas por pruebas automatizadas, de modo que cualquier cambio que rompa la matriz de transiciones permitidas (Pending → PrimerRecordatorio → SegundoRecordatorio → Desactivado, y Pagado desde cualquier estado activo) sea detectado de inmediato antes de llegar a producción.

**Why this priority**: Las transiciones de estado son el núcleo del comportamiento del dominio de facturación. Un error en la matriz de transiciones provoca facturas en estados inconsistentes, notificaciones incorrectas y pérdida de confianza del negocio. Es la regla de negocio de mayor riesgo y mayor valor de protección.

**Independent Test**: Se puede probar completamente ejecutando la suite de pruebas del dominio y verificando que existen casos que afirman tanto las transiciones permitidas (resultado exitoso) como las prohibidas (rechazo explícito) para cada estado de origen, sin depender de base de datos, red ni capas externas.

**Acceptance Scenarios**:

1. **Given** una factura en cada estado de origen del conjunto activo, **When** se solicita una transición permitida por la matriz de negocio, **Then** la transición se aplica y el nuevo estado queda registrado en el historial.
2. **Given** una factura en cualquier estado, **When** se solicita una transición no permitida por la matriz, **Then** la operación es rechazada de forma explícita (error de regla de negocio) y el estado de la factura no cambia.
3. **Given** una factura en estado terminal (Pagado), **When** se intenta una nueva transición de pago, **Then** la operación es rechazada explícitamente.
4. **Given** una factura cuyo tiempo configurado de permanencia en un estado aún no se ha cumplido, **When** se evalúa la transición automática por tiempo, **Then** no se aplica ninguna transición.
5. **Given** una factura cuyo tiempo configurado de permanencia se ha cumplido, **When** se evalúa la transición automática por tiempo, **Then** se aplica la transición al siguiente estado.

---

### User Story 2 - Verificar la creación y validación de facturas (Priority: P1)

Como integrante del equipo de desarrollo, necesito que la creación de facturas y sus invariantes (cliente obligatorio, monto positivo, al menos una línea de detalle, monto igual a la suma de subtotales) estén cubiertas por pruebas, de modo que ninguna factura inválida pueda construirse en el dominio.

**Why this priority**: La creación válida de una factura garantiza la integridad de todos los procesos posteriores (cobro, transiciones, notificaciones). Una factura mal formada es un defecto que se propaga silenciosamente. Comparte la prioridad máxima con las transiciones por ser un invariante fundamental del dominio.

**Independent Test**: Se puede probar de forma aislada construyendo facturas con datos válidos e inválidos y verificando que las válidas se crean correctamente y las inválidas se rechazan con un error claro, sin dependencias externas.

**Acceptance Scenarios**:

1. **Given** datos de factura válidos (cliente, líneas de detalle, vencimiento), **When** se crea la factura, **Then** la factura queda en el estado inicial esperado, con monto igual a la suma de subtotales y un historial vacío.
2. **Given** un identificador de cliente vacío o ausente, **When** se intenta crear la factura, **Then** la creación es rechazada con un error de validación.
3. **Given** un monto menor o igual a cero, **When** se intenta crear la factura, **Then** la creación es rechazada con un error de validación.
4. **Given** una lista de líneas de detalle vacía, **When** se intenta crear la factura, **Then** la creación es rechazada con un error de validación.
5. **Given** una factura en estado terminal, **When** se intenta editar sus campos, **Then** la edición es rechazada explícitamente.

---

### User Story 3 - Alcanzar y verificar el umbral mínimo de cobertura (Priority: P2)

Como responsable de la calidad del proyecto, necesito una medición objetiva de la cobertura de código del dominio que demuestre que se alcanza al menos el 85% exigido por la constitución, de modo que el gate de calidad de cada PR pueda apoyarse en una evidencia reproducible.

**Why this priority**: El umbral de cobertura es un requisito de gobernanza (constitución, Principio IV). Sin medición no se puede hacer cumplir el gate. Es P2 porque depende de que primero existan las pruebas de comportamiento (P1).

**Independent Test**: Se puede verificar ejecutando la suite con recolección de cobertura y comprobando que el porcentaje de líneas cubiertas del proyecto de dominio es ≥ 85%.

**Acceptance Scenarios**:

1. **Given** la suite de pruebas del dominio completa, **When** se ejecuta con recolección de cobertura, **Then** se genera un reporte de cobertura legible por la herramienta de CI.
2. **Given** el reporte de cobertura del dominio, **When** se inspecciona el porcentaje de líneas cubiertas, **Then** es igual o superior al 85%.

---

### Edge Cases

- ¿Qué ocurre al intentar una transición desde un estado terminal (Pagado/Desactivado) por la vía automática de tiempo? Debe no aplicar transición sin lanzar error inesperado.
- ¿Qué ocurre al solicitar una transición manual hacia el mismo estado actual? Debe tratarse como transición no permitida si no figura en la matriz.
- ¿Qué ocurre cuando una línea de detalle tiene cantidad o precio no positivos? La creación debe rechazarse por el invariante de subtotales.
- ¿Cómo se garantiza el determinismo de las pruebas dependientes del tiempo (transiciones por días)? El "momento actual" debe poder inyectarse para evitar pruebas inestables.
- ¿Qué ocurre con el contador de reintentos de notificación al entrar en un nuevo estado notificable? Debe reiniciarse a cero.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La suite DEBE verificar, para cada estado de origen del conjunto activo, todas las transiciones permitidas por la matriz de negocio (Pending→{PrimerRecordatorio, Pagado}; PrimerRecordatorio→{SegundoRecordatorio, Pagado}; SegundoRecordatorio→{Desactivado, Pagado}; Desactivado→{Pagado}; Pagado→{}).
- **FR-002**: La suite DEBE verificar que toda transición no contemplada por la matriz es rechazada explícitamente y deja el estado de la factura sin cambios.
- **FR-003**: La suite DEBE verificar las transiciones automáticas por tiempo en ambos sentidos: no transicionar cuando el plazo no se cumple y transicionar cuando sí se cumple.
- **FR-004**: La suite DEBE verificar la creación de facturas con datos válidos, comprobando estado inicial, monto derivado de la suma de subtotales e historial inicial.
- **FR-005**: La suite DEBE verificar el rechazo de la creación de facturas ante cliente vacío/ausente, monto no positivo y lista de líneas de detalle vacía.
- **FR-006**: La suite DEBE verificar que las facturas en estado terminal rechazan tanto la edición de campos como una nueva transición de pago.
- **FR-007**: Las aserciones DEBEN expresarse con una sintaxis de aserción legible y orientada al comportamiento (estilo Shouldly), priorizando la claridad del mensaje de fallo.
- **FR-008**: Las pruebas dependientes del tiempo DEBEN ser deterministas, inyectando el momento actual en lugar de depender del reloj del sistema.
- **FR-009**: La ejecución de la suite DEBE producir un reporte de cobertura consumible por el pipeline de CI.
- **FR-010**: La cobertura de líneas del proyecto de dominio DEBE ser igual o superior al 85%.
- **FR-011**: Las pruebas NO DEBEN depender de base de datos, red, sistema de archivos ni de ninguna capa externa al dominio; deben ejecutarse de forma aislada y rápida.
- **FR-012**: La suite NO DEBE contener pruebas omitidas o ignoradas (sin `[Ignore]`/skips), conforme al workflow de calidad.

### Key Entities *(include if feature involves data)*

- **Invoice (Factura)**: Entidad raíz del dominio; concentra los invariantes de creación, edición y el ciclo de estado. Es el sujeto principal de las pruebas.
- **InvoiceStatus (Estado de factura)**: Conjunto cerrado de estados activos (Pending, PrimerRecordatorio, SegundoRecordatorio, Desactivado, Pagado) sobre el que se define la matriz de transiciones.
- **InvoiceItem (Línea de detalle)**: Componente que aporta el subtotal usado para derivar el monto de la factura; participa en los invariantes de creación.
- **InvoiceTransitionService (Servicio de transiciones)**: Regla de dominio que evalúa y aplica transiciones automáticas por tiempo y manuales según la matriz.
- **StatusChange (Cambio de estado)**: Registro histórico que cada transición debe dejar; las pruebas verifican que el historial nunca se desincroniza del estado actual.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: La cobertura de líneas del proyecto de dominio es ≥ 85%, medida automáticamente en cada ejecución.
- **SC-002**: El 100% de los estados de origen del conjunto activo tiene al menos un caso de transición permitida y un caso de transición prohibida verificados.
- **SC-003**: El 100% de los invariantes de creación de factura (cliente, monto, líneas de detalle) tiene al menos un caso de rechazo verificado.
- **SC-004**: La suite completa del dominio se ejecuta en menos de 10 segundos en una máquina de desarrollo estándar (evidencia de aislamiento y ausencia de dependencias externas).
- **SC-005**: La suite del dominio termina con cero pruebas fallidas y cero pruebas omitidas en la ejecución de CI.

## Assumptions

- Ya existe un proyecto de pruebas del dominio (`Monolegal.Domain.Tests`) configurado con xUnit, Shouldly y recolección de cobertura (coverlet); esta spec consolida y completa su cobertura hasta el umbral, no parte de cero.
- El alcance de la cobertura se limita a la capa de dominio (entidades, enumeraciones y servicios de dominio); la cobertura de las capas Application, Infrastructure y Api se aborda en specs separadas.
- La matriz de transiciones de referencia es la vigente en el servicio de dominio (spec 006) y el conjunto de estados activos es el definido en la spec 015.
- El umbral del 85% se mide sobre cobertura de líneas, conforme al uso habitual de la herramienta de cobertura del proyecto.
- Las pruebas se ejecutan en el mismo runtime objetivo del backend (.NET 10) y se integran al gate de CI existente.
