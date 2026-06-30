# Feature Specification: Tests E2E con Playwright

**Feature Branch**: `023-tests-e2e-playwright`

**Created**: 2026-06-29

**Status**: Activo

**Input**: Roadmap Spec 5.4 — "E2E Tests - Playwright": dada la aplicación fullstack, cuando se corren los tests E2E, entonces se validan los flujos críticos: abrir lista de facturas, filtrar por estado, hacer una transición manual y ver el dashboard actualizado.

## Contexto

El proyecto ya cubre pruebas unitarias de dominio (Spec 5.1), de integración de API (Spec 5.2) y de componentes de frontend con Vitest (Spec 5.3). Falta el nivel más alto de la pirámide de pruebas: **pruebas end-to-end** que ejerciten la aplicación completa (frontend + backend + base de datos) como lo haría una persona usuaria real desde el navegador. Hoy no existe ninguna prueba E2E ni infraestructura para ejecutarlas.

Esta feature establece la suite E2E sobre los flujos críticos del panel administrativo, en línea con el Principio IV de la constitución ("E2E Tests: jornadas críticas del usuario — listar facturas → filtrar → transicionar → confirmar"). La aplicación no tiene autenticación, por lo que las pruebas acceden directamente a las vistas. El sistema dispone de datos de desarrollo deterministas (3 clientes y 8 facturas con estados variados) que se siembran de forma idempotente, y de una herramienta de reinicio de base de datos, lo que permite poner el entorno en un estado conocido antes de cada corrida. El alcance es exclusivamente añadir pruebas e infraestructura de pruebas; no se modifica el comportamiento de producción.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Listar y filtrar facturas por estado (Priority: P1)

Como responsable de calidad, quiero una prueba E2E que abra la lista de facturas y la filtre por estado en un navegador real, de modo que se verifique de extremo a extremo que el listado carga datos del backend y que el filtrado por estado devuelve únicamente las facturas correspondientes.

**Why this priority**: Es el punto de entrada de la jornada crítica del usuario y el primero de los flujos del roadmap. Valida la integración real frontend↔backend↔datos sin mocks y sienta la base para los flujos siguientes.

**Independent Test**: Se ejecuta la suite E2E contra la aplicación levantada con datos sembrados; la prueba navega a la vista de facturas, confirma que se muestran facturas y luego aplica un filtro de estado, verificando que solo aparecen facturas de ese estado. Entrega valor por sí sola como verificación de la vista principal.

**Acceptance Scenarios**:

1. **Given** la aplicación levantada con datos de prueba sembrados, **When** la usuaria abre la vista de facturas, **Then** se muestra una tabla con las facturas existentes (cliente, monto, estado) sin errores de carga.
2. **Given** la vista de facturas cargada, **When** la usuaria selecciona un estado concreto en el filtro de estado, **Then** la tabla muestra únicamente facturas de ese estado.
3. **Given** un filtro de estado aplicado, **When** la usuaria vuelve a "Todos los estados", **Then** se muestran de nuevo todas las facturas.
4. **Given** un estado sin facturas asociadas, **When** la usuaria lo selecciona en el filtro, **Then** se muestra el estado vacío correspondiente sin error.

---

### User Story 2 - Realizar una transición manual de estado (Priority: P1)

Como responsable de calidad, quiero una prueba E2E que abra el detalle de una factura y cambie su estado mediante una transición permitida, de modo que se verifique que la transición se persiste y se refleja en la interfaz.

**Why this priority**: Es la acción de negocio central del panel (gestionar el ciclo de cobro) y el corazón de la jornada crítica del usuario. Ejercita la escritura real contra el backend y la regla de transiciones permitidas.

**Independent Test**: Partiendo de una factura en un estado no terminal, la prueba abre su detalle, elige un estado destino entre los permitidos y confirma la transición, verificando la confirmación visible y el nuevo estado en la interfaz.

**Acceptance Scenarios**:

1. **Given** una factura en un estado no terminal, **When** la usuaria abre su detalle, **Then** se muestra el control para cambiar de estado con únicamente los estados destino permitidos.
2. **Given** el detalle de una factura no terminal, **When** la usuaria selecciona un estado destino permitido y confirma el cambio, **Then** se muestra una confirmación visible y la factura aparece con el nuevo estado.
3. **Given** una factura en un estado terminal, **When** la usuaria abre su detalle, **Then** se le indica que la factura no admite cambios de estado y no hay control de transición disponible.
4. **Given** una transición completada, **When** la usuaria vuelve a la lista de facturas, **Then** la factura figura con el estado actualizado de forma persistente.

---

### User Story 3 - Ver el dashboard actualizado tras una transición (Priority: P2)

Como responsable de calidad, quiero una prueba E2E que, tras realizar una transición, navegue al dashboard y verifique que las métricas y gráficos reflejan el cambio, de modo que se confirme la coherencia de los datos agregados de extremo a extremo.

**Why this priority**: Cierra la jornada crítica del usuario ("…→ confirmar") verificando que la transición se propaga a las vistas agregadas. Depende de los flujos P1, por lo que tiene prioridad inmediatamente posterior.

**Independent Test**: Tras ejecutar una transición que cambie la distribución por estado, la prueba abre el dashboard y comprueba que las métricas y la distribución por estado reflejan el nuevo conteo respecto al estado previo.

**Acceptance Scenarios**:

1. **Given** una distribución de facturas por estado conocida, **When** la usuaria realiza una transición que cambia esa distribución y abre el dashboard, **Then** la métrica total y la distribución por estado reflejan el cambio.
2. **Given** el dashboard abierto tras una transición, **When** se comparan los datos con el estado previo, **Then** el conteo del estado origen disminuye y el del estado destino aumenta de forma consistente.
3. **Given** una base de datos sin facturas, **When** la usuaria abre el dashboard, **Then** se muestra el estado vacío correspondiente sin error.

---

### Edge Cases

- ¿Qué ocurre si el backend no está disponible al cargar una vista? → La prueba debe poder distinguir el estado de error de la interfaz ("no se pudieron cargar…") de un fallo de la propia prueba; la suite asume el sistema levantado y sano como precondición.
- ¿Cómo se evita el acoplamiento al orden o a datos residuales entre pruebas? → El entorno se restablece a un estado conocido (reinicio + sembrado idempotente) antes de la corrida, y cada prueba elige sus datos objetivo por estado/relación, no por posición fija.
- ¿Qué pasa con valores no deterministas (fechas, "último refresco", animaciones de gráficos)? → Las aserciones se basan en contenido estable (estados, etiquetas, conteos) y no en marcas de tiempo ni en detalles de animación.
- ¿Qué ocurre si una factura ya está en estado terminal y no admite transición? → Es un caso cubierto explícitamente: la prueba verifica la ausencia de control de transición y el mensaje informativo.
- ¿Cómo se comporta la suite en distintos navegadores/resoluciones? → Las pruebas se basan en roles/etiquetas accesibles y textos visibles, evitando selectores frágiles, para tolerar diferencias de render.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La suite E2E DEBE ejercitar la aplicación fullstack real (frontend servido + backend + base de datos), sin sustituir el backend por mocks.
- **FR-002**: La suite DEBE cubrir el flujo de abrir la lista de facturas y verificar que se cargan datos reales sin errores.
- **FR-003**: La suite DEBE cubrir el filtrado de facturas por estado, verificando que el resultado contiene únicamente facturas del estado seleccionado y que es posible volver a "todos los estados".
- **FR-004**: La suite DEBE cubrir la realización de una transición manual de estado sobre una factura no terminal, eligiendo un estado destino entre los permitidos y verificando la confirmación visible y el estado actualizado.
- **FR-005**: La suite DEBE verificar que una factura en estado terminal no ofrece control de transición y comunica que no admite cambios.
- **FR-006**: La suite DEBE verificar que, tras una transición, el dashboard refleja la nueva distribución por estado y la métrica total, cerrando la jornada crítica del usuario.
- **FR-007**: La suite DEBE poder situar el entorno en un estado de datos conocido y reproducible antes de ejecutarse (reinicio de base de datos + datos sembrados deterministas), de modo que las pruebas sean repetibles e independientes del orden de ejecución.
- **FR-008**: Las pruebas DEBEN localizar elementos mediante roles, etiquetas accesibles y textos visibles estables, evitando selectores frágiles dependientes de la estructura interna.
- **FR-009**: La suite E2E DEBE poder ejecutarse mediante un comando dedicado y DEBE ser apta para CI (resultado determinista, código de salida que falle si alguna prueba falla, sin pruebas omitidas conforme al Principio IV).
- **FR-010**: La suite NO DEBE modificar el comportamiento del código de producción; el alcance es exclusivamente añadir pruebas e infraestructura de pruebas.
- **FR-011**: La documentación de la feature (spec, plan, tareas, instrucciones de ejecución) DEBE estar en español, conforme al Principio III.
- **FR-012**: La suite DEBE pasar de forma estable; cualquier prueba intermitente se considera defecto y debe corregirse o aislarse, no ignorarse silenciosamente.

### Key Entities *(include if feature involves data)*

- **Flujo crítico (jornada de usuario)**: Secuencia de pasos que una usuaria realiza en el navegador (listar → filtrar → transicionar → confirmar en dashboard); es la unidad de prueba E2E.
- **Estado conocido de datos**: Conjunto determinista de clientes y facturas con estados variados, obtenido por reinicio + sembrado idempotente, que sirve de precondición reproducible para las pruebas.
- **Estado de factura**: Etapa del ciclo de cobro (pendiente, recordatorios, pagado, desactivado), con transiciones permitidas definidas por el backend como fuente de verdad; algunos estados son terminales.
- **Distribución por estado**: Agregado mostrado en el dashboard (conteos y gráficos) que debe permanecer coherente con los estados individuales de las facturas tras cada transición.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Los cuatro flujos críticos del roadmap (abrir lista, filtrar por estado, transición manual, dashboard actualizado) quedan cubiertos por al menos una prueba E2E cada uno.
- **SC-002**: La suite E2E completa pasa en verde en dos corridas consecutivas sobre el mismo entorno sin fallos intermitentes (cero flakiness).
- **SC-003**: La suite se ejecuta con un único comando y devuelve código de salida distinto de cero si cualquier prueba falla (apta para CI).
- **SC-004**: Partiendo de un entorno restablecido y sembrado, el 100% de las pruebas E2E es independiente del orden de ejecución (cualquier subconjunto puede correr aislado y pasar).
- **SC-005**: No se introduce ningún cambio en el comportamiento del código de producción (solo archivos de prueba, configuración y utilidades de prueba).
- **SC-006**: Una transición realizada por la suite se refleja de forma consistente tanto en la lista de facturas como en los agregados del dashboard dentro de la misma corrida.

## Assumptions

- La aplicación no tiene autenticación, por lo que las pruebas acceden directamente a las vistas sin pasar por un login.
- El frontend se sirve y el backend se ejecuta en un entorno de desarrollo/CI accesible para el navegador de pruebas; la suite asume el sistema levantado y sano como precondición.
- Los datos de desarrollo (3 clientes, 8 facturas con estados variados) y la herramienta de reinicio de base de datos están disponibles para crear un estado conocido y reproducible; el sembrado es idempotente.
- Las transiciones permitidas las define el backend como fuente de verdad; las pruebas eligen estados destino entre los ofrecidos por la interfaz, no asumen un grafo de transiciones fijo.
- "Dashboard actualizado" se interpreta como que las vistas agregadas reflejan la transición dentro de la misma sesión de prueba, sin requerir recarga manual de la página.
- El alcance se limita a los cuatro flujos críticos del roadmap; flujos secundarios (crear/editar/eliminar factura, envíos, clientes, configuración) quedan fuera de esta feature salvo en lo necesario para preparar precondiciones.
