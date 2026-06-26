# Feature Specification: Panel de Administración — Layout Base y Listado de Facturas

**Feature Branch**: `014-admin-panel-invoices`

**Created**: 2026-06-25

**Status**: Activo

**Input**: User description: "Quiero que uses shadcn para construir el panel de administración, quiero que sea con animaciones con motion, transiciones suaves, skeletons y buenas prácticas, quiero 100 de 100 en react doctor de forma honesta" (roadmap.md, Specs 4.1 Layout Base y 4.2 Invoices Page - Listado)

## Resumen

Construir la primera versión navegable del panel de administración de Monolegal: una **estructura base** (navbar con logo, barra lateral de navegación, pie de página, diseño responsive y modo oscuro) y la **página de listado de facturas** (tabla con ID, Cliente, Monto, Estado y Última Acción; filtro por estado; búsqueda por cliente; paginación de 10 ítems por página; y skeleton loaders mientras se cargan los datos).

La experiencia debe sentirse **pulida y profesional**: componentes de interfaz consistentes, animaciones de entrada/salida y micro-interacciones suaves, transiciones que no bloqueen la lectura, estados de carga claros (skeletons) y un nivel de calidad de código verificable de forma honesta (sin silenciar avisos) por la herramienta de inspección de React del proyecto.

## Clarifications

### Session 2026-06-25

- Q: ¿Dónde se resuelven el filtro por estado, la búsqueda por cliente y la paginación (servidor vs. cliente)? → A: Filtro por estado y paginación del lado del **servidor** (contrato de API existente). La **búsqueda por cliente es GLOBAL y entra en el alcance de esta spec**: debe cubrir todas las facturas que cumplen el filtro (no solo la página cargada), lo que requiere extender el endpoint de listado con un parámetro de búsqueda por cliente del lado del servidor, integrado con el filtro de estado y la paginación.
- Q: ¿Qué ítems muestra la navegación lateral en esta feature? → A: Facturas como sección funcional y activa; Dashboard y Configuración **visibles pero deshabilitados** ("próximamente"), sin ruta activa, para comunicar el mapa del producto sin prometer funcionalidad fuera de alcance.
- Q: ¿Cómo se expone el control de tema claro/oscuro? → A: **Fuera de alcance** en esta feature. El panel debe verse correctamente en claro y oscuro y respetar la preferencia del sistema (conservando cualquier elección ya almacenada), pero el **control visible para cambiar el tema se difiere a la futura sección de Configuración**.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Navegar por el panel desde una estructura base coherente (Priority: P1)

Como administrador, al abrir la aplicación quiero encontrar una estructura clara —marca/logo arriba, navegación lateral y pie de página— para orientarme y moverme entre las secciones del panel desde cualquier tamaño de pantalla y con modo claro/oscuro.

**Why this priority**: Es el esqueleto sobre el que vive todo el panel. Sin una estructura navegable, ninguna página posterior tiene un hogar coherente. Entrega valor inmediato: una aplicación que se ve y se siente como un producto, no como una pantalla suelta.

**Independent Test**: Cargar la raíz de la aplicación y verificar que se muestran navbar con logo, barra de navegación lateral y pie de página; que la navegación lleva a la sección de facturas; que el layout se reorganiza correctamente en móvil y escritorio; y que el modo oscuro/claro se puede alternar y persiste.

**Acceptance Scenarios**:

1. **Given** la aplicación recién iniciada, **When** se carga la ruta raíz, **Then** se muestran navbar con logo de Monolegal, barra lateral de navegación y pie de página con información de la aplicación.
2. **Given** el panel cargado en escritorio, **When** se reduce el ancho a tamaño móvil, **Then** el layout se adapta (la navegación lateral se colapsa en un menú accesible) sin romper la lectura ni provocar desbordamiento horizontal.
3. **Given** una preferencia de tema almacenada (la del sistema o una elección previa), **When** recarga la página, **Then** el panel respeta ese tema (claro/oscuro/sistema) y se renderiza correctamente en él. (El control para cambiar el tema queda fuera de alcance de esta feature.)
4. **Given** el panel con la navegación lateral, **When** el usuario selecciona la sección "Facturas", **Then** la sección activa queda resaltada y se muestra el listado de facturas.

---

### User Story 2 - Consultar el listado de facturas con estados de carga claros (Priority: P1)

Como administrador, quiero ver todas las facturas en una tabla con ID, Cliente, Monto, Estado y Última Acción, y que mientras cargan los datos se muestren skeletons en lugar de una pantalla en blanco, para entender de inmediato qué está pasando y poder revisar el estado de la cartera.

**Why this priority**: Es el valor central del panel: visibilizar las facturas y su estado. Junto con la estructura base constituye el MVP demostrable de la Fase 4.

**Independent Test**: Abrir la sección de facturas y verificar que durante la carga aparecen skeletons con la forma de la tabla, que al resolver se muestran las filas con las columnas requeridas (ID, Cliente, Monto, Estado, Última Acción), que el monto y la fecha se muestran con formato legible, que el estado se muestra como etiqueta de color, y que un listado vacío o un error muestran un mensaje claro en lugar de una tabla vacía sin contexto.

**Acceptance Scenarios**:

1. **Given** el usuario en la sección de facturas, **When** la petición de datos está en curso, **Then** se muestran skeleton loaders con la forma de la tabla (no una pantalla en blanco ni un spinner aislado).
2. **Given** la petición de datos resuelta con resultados, **When** se renderiza la tabla, **Then** cada fila muestra ID (legible/abreviado), Cliente, Monto (formato moneda), Estado (etiqueta de color) y Última Acción (fecha/hora legible).
3. **Given** la petición resuelta sin resultados, **When** se renderiza la tabla, **Then** se muestra un estado vacío con un mensaje claro ("No hay facturas para mostrar") en lugar de una tabla sin filas.
4. **Given** un fallo de la petición de datos, **When** se renderiza la sección, **Then** se muestra un mensaje de error legible y, si aplica, una acción para reintentar, sin romper el resto del panel.

---

### User Story 3 - Filtrar, buscar y paginar el listado (Priority: P2)

Como administrador, quiero filtrar las facturas por estado, buscar por cliente y avanzar por páginas de 10 ítems, para encontrar rápidamente las facturas relevantes sin perderme en listados largos.

**Why this priority**: Multiplica la utilidad del listado en cuanto crece el volumen. Depende de que el listado (US2) exista, por eso es P2.

**Independent Test**: Con un conjunto de facturas en varios estados, aplicar un filtro por estado y verificar que solo se muestran las coincidencias; escribir el identificador de un cliente y verificar que el listado se reduce a sus facturas; y navegar entre páginas verificando que cada página muestra como máximo 10 ítems y que los controles de paginación reflejan el total.

**Acceptance Scenarios**:

1. **Given** facturas en distintos estados, **When** el usuario elige un estado en el filtro, **Then** la tabla muestra solo las facturas en ese estado y los controles reflejan el nuevo total.
2. **Given** el filtro por estado aplicado, **When** el usuario selecciona "Todos", **Then** la tabla vuelve a mostrar facturas de todos los estados.
3. **Given** un valor escrito en la búsqueda por cliente, **When** el usuario teclea, **Then** la tabla muestra únicamente las facturas cuyo cliente coincide, con un comportamiento que no provoque parpadeos ni saltos bruscos en cada pulsación.
4. **Given** más de 10 facturas que cumplen el criterio, **When** se renderiza el listado, **Then** se muestran como máximo 10 ítems por página y existen controles para avanzar/retroceder de página.
5. **Given** el usuario en la página 2, **When** cambia el filtro o la búsqueda, **Then** la paginación se reinicia a la primera página de los nuevos resultados.

---

### User Story 4 - Experiencia animada, accesible y de calidad verificable (Priority: P3)

Como usuario del panel, quiero que las transiciones entre estados (carga → contenido, apertura/cierre de menú, cambios de página/filtro) sean suaves y discretas, y como equipo queremos que la calidad del código sea verificable de forma honesta, para que el panel se sienta profesional y se mantenga sano en el tiempo.

**Why this priority**: Es la capa de pulido transversal. El panel funciona sin ella, pero el objetivo explícito es una experiencia animada y un estándar de calidad medible. Se evalúa al final porque atraviesa a las demás historias.

**Independent Test**: Observar que el contenido aparece con una transición suave al terminar la carga, que el menú lateral se abre/cierra con animación, y que los cambios de filtro/página no producen saltos abruptos; navegar con teclado y lector de pantalla por la navegación, la tabla y los controles; y ejecutar la herramienta de inspección de React del proyecto obteniendo una puntuación perfecta sin avisos silenciados.

**Acceptance Scenarios**:

1. **Given** la transición de skeleton a contenido, **When** los datos llegan, **Then** el contenido aparece con una animación de entrada suave (sin parpadeo ni salto de layout perceptible).
2. **Given** la navegación lateral en móvil, **When** se abre o cierra, **Then** la transición es animada y reversible sin trabarse.
3. **Given** un usuario que respeta "reducir movimiento" en su sistema, **When** ocurren animaciones, **Then** estas se atenúan o desactivan conforme a esa preferencia.
4. **Given** un usuario navegando solo con teclado, **When** recorre navegación, filtros, búsqueda, paginación y filas, **Then** todos los elementos interactivos son alcanzables y operables, con foco visible.
5. **Given** el código del panel terminado, **When** se ejecuta la inspección de React del proyecto, **Then** la puntuación es 100/100 sin avisos suprimidos artificialmente para alcanzarla.

---

### Edge Cases

- **Listado vacío**: sin facturas (o filtro/búsqueda sin coincidencias) → estado vacío con mensaje claro, manteniendo visibles los controles de filtro/búsqueda.
- **Página fuera de rango**: si tras filtrar/buscar la página actual ya no existe, el listado se reposiciona a una página válida (p. ej. la primera).
- **Error de carga**: fallo de red o del servidor → mensaje de error legible y opción de reintento; el resto del panel sigue usable.
- **Monto/fecha atípicos**: importes grandes o ausencia de fecha de última acción → formato consistente sin romper la alineación de columnas.
- **Estado desconocido**: una factura con un estado no contemplado → se muestra una etiqueta neutra con el valor en bruto en lugar de fallar.
- **Búsqueda con espacios o mayúsculas**: la búsqueda por cliente ignora mayúsculas/minúsculas y espacios sobrantes en los extremos.
- **Viewport muy estrecho**: la tabla permite desplazamiento horizontal controlado en pantallas pequeñas sin desbordar el layout.
- **Movimiento reducido**: con preferencia de "reducir movimiento" activa, las animaciones no deben provocar mareo ni impedir el uso.

## Requirements *(mandatory)*

### Functional Requirements

#### Estructura base (Layout) — roadmap 4.1

- **FR-001**: El panel MUST presentar una barra superior (navbar) con el logo/marca de Monolegal visible en todo momento.
- **FR-002**: El panel MUST incluir una navegación lateral con la sección **Facturas** funcional y resaltada como activa, y con entradas para **Dashboard** y **Configuración** visibles pero **deshabilitadas** (marcadas como "próximamente"), sin ruta activa, hasta que se aborden en features posteriores.
- **FR-003**: El panel MUST incluir un pie de página con información de la aplicación (p. ej. nombre y versión/año).
- **FR-004**: El layout MUST ser responsive: en escritorio la navegación lateral es persistente y en móvil se colapsa en un menú accesible que se puede abrir y cerrar.
- **FR-005**: El panel MUST verse correctamente en modo claro y oscuro, respetando la preferencia del sistema por defecto y conservando entre recargas cualquier elección ya almacenada. El **control visible para cambiar el tema queda fuera de alcance** de esta feature (se difiere a la futura sección de Configuración).
- **FR-006**: La navegación MUST permitir llegar a la sección de Facturas y reflejar visualmente cuál es la sección actual.

#### Listado de facturas — roadmap 4.2

- **FR-007**: La sección de Facturas MUST mostrar una tabla con las columnas: ID, Cliente, Monto, Estado y Última Acción.
- **FR-008**: El ID MUST mostrarse de forma legible (abreviado) y el Monto con formato de moneda; la Última Acción MUST mostrarse como fecha/hora legible en español.
- **FR-009**: El Estado MUST mostrarse como una etiqueta visualmente diferenciada por color según el estado de la factura.
- **FR-010**: Mientras los datos se cargan, la sección MUST mostrar skeleton loaders con la forma de la tabla (no pantallas en blanco ni spinners aislados como único indicador).
- **FR-011**: La sección MUST ofrecer un filtro por estado que limite el listado a las facturas del estado elegido, e incluir una opción para ver todos los estados. El filtro se resuelve del lado del servidor.
- **FR-012**: La sección MUST ofrecer una búsqueda por cliente **global**: debe devolver todas las facturas cuyo cliente coincide en el conjunto completo de datos (no solo en la página actualmente cargada), combinándose con el filtro por estado y respetando la paginación. La coincidencia ignora mayúsculas/minúsculas y espacios en los extremos. La búsqueda se resuelve del lado del servidor (requiere un parámetro de búsqueda por cliente en el endpoint de listado).
- **FR-013**: El listado MUST paginarse del lado del servidor mostrando como máximo 10 ítems por página, con controles para avanzar y retroceder y una indicación de la página/total actual. La paginación refleja el total de coincidencias del filtro y la búsqueda activos.
- **FR-014**: Al cambiar el filtro o la búsqueda, la paginación MUST reiniciarse a la primera página de los nuevos resultados.
- **FR-015**: Cuando no haya facturas que mostrar (vacío o sin coincidencias), la sección MUST mostrar un estado vacío con un mensaje claro.
- **FR-016**: Ante un fallo al obtener los datos, la sección MUST mostrar un mensaje de error legible y, cuando sea posible, una acción para reintentar, sin romper el resto del panel.

#### Experiencia, animación y calidad — solicitud explícita del usuario

- **FR-017**: Las transiciones de estado clave (carga → contenido, apertura/cierre del menú, cambios de filtro/página) MUST ser suaves y discretas, sin saltos de layout perceptibles ni parpadeos.
- **FR-018**: Las animaciones MUST respetar la preferencia del sistema de "reducir movimiento", atenuándose o desactivándose cuando esté activa.
- **FR-019**: Todos los elementos interactivos (navegación, filtro, búsqueda, paginación, filas/acciones) MUST ser operables por teclado, con foco visible, y expuestos de forma accesible a tecnologías de asistencia (nivel WCAG A como mínimo).
- **FR-020**: El panel MUST mantener consistencia visual usando un sistema de componentes de interfaz único en toda la feature (tipografía, espaciado, colores y estados de interacción coherentes).
- **FR-021**: El código de la feature MUST alcanzar una puntuación de 100/100 en la herramienta de inspección de React del proyecto **de forma honesta**, es decir, sin suprimir, ignorar ni silenciar avisos para inflar la puntuación.

### Key Entities *(include if feature involves data)*

- **Factura (Invoice)**: representa una factura del sistema. Atributos relevantes para esta vista: identificador, cliente (identificador del cliente), monto, estado y fecha de la última transición de estado ("Última Acción"). Es de solo lectura en esta feature (no se crean ni editan facturas aquí).
- **Estado de factura**: clasificación de la factura (p. ej. pendiente, primer recordatorio, segundo recordatorio, desactivado, pagado, y otros estados internos) que determina la etiqueta de color y el filtrado.
- **Vista de listado**: conjunto de criterios de presentación —filtro por estado, término de búsqueda por cliente y página actual— que determinan qué subconjunto de facturas se muestra.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un administrador puede, desde la carga inicial, llegar al listado de facturas y entender el estado de una factura concreta en menos de 10 segundos.
- **SC-002**: Durante toda carga de datos, el usuario ve skeletons que reflejan la estructura final; en ningún flujo de carga aparece una pantalla en blanco como único indicador.
- **SC-003**: El listado nunca muestra más de 10 facturas por página, y filtrar/buscar reduce visiblemente los resultados manteniendo la paginación coherente.
- **SC-004**: El panel es completamente utilizable —navegación, filtro, búsqueda, paginación— únicamente con teclado, con foco siempre visible.
- **SC-005**: El layout se muestra correctamente, sin desbordamiento horizontal ni solapamientos, en anchos representativos de móvil y de escritorio.
- **SC-006**: La herramienta de inspección de React del proyecto reporta 100/100 sin avisos suprimidos artificialmente.
- **SC-007**: La aplicación alcanza una métrica de "tiempo hasta interactivo" inferior a 2 segundos en condiciones representativas y una puntuación de rendimiento superior a 90 en auditoría estándar.
- **SC-008**: Con la preferencia de "reducir movimiento" activa, ninguna animación esencial impide completar las tareas del panel.

## Assumptions

- **Alcance**: esta feature cubre únicamente las specs 4.1 (Layout Base) y 4.2 (Listado de Facturas) del roadmap. El detalle de factura en modal (4.3), el dashboard de estadísticas (4.4) y el formulario de transición manual (4.5) quedan fuera de alcance y se abordarán en features posteriores.
- **Stack mandatado**: por la Constitución del proyecto y la solicitud explícita del usuario, la interfaz se construye con componentes shadcn/ui sobre React 19 + Vite + Tailwind, animaciones con Motion, estado de servidor con TanStack Query y verificación de calidad con React Doctor (estos son requisitos del proyecto, no decisiones abiertas de esta spec).
- **Solo lectura**: el listado consume facturas existentes; no se crean ni editan facturas en esta feature. La acción de "Pagar" ya existente puede convivir, pero no forma parte del alcance nuevo.
- **Búsqueda por cliente**: la búsqueda es **global** y forma parte del alcance de esta feature. Dado que el contrato actual de la API de listado expone filtro por estado y paginación pero **no** un parámetro de búsqueda por cliente, esta feature **extiende el endpoint de listado con un parámetro de búsqueda por cliente** (server-side) para que la búsqueda cubra todo el conjunto de datos y se combine con el filtro por estado y la paginación. Es una dependencia de backend acotada que habilita el requisito FR-012.
- **Identidad del cliente**: el cliente se identifica por su identificador (`clientId`), pues aún no existe una entidad de cliente con nombre; cuando exista, la columna "Cliente" podrá mostrar el nombre sin cambiar el alcance de esta vista.
- **Control de tema**: el panel debe verse bien en claro y oscuro y respetar la preferencia del sistema/almacenada, pero el control visible para cambiar el tema se difiere a la futura sección de Configuración (fuera de alcance de esta feature).
- **Navegación**: como solo Facturas es funcional (Dashboard y Configuración van deshabilitados), no se requiere enrutamiento multi-ruta complejo en esta feature; la decisión concreta de enrutamiento se aborda en planificación.
- **Autenticación**: se asume que el acceso al panel está protegido a nivel de aplicación (rol Admin) por una capa previa; esta feature no implementa autenticación.
- **Datos**: se asume disponible el endpoint de listado de facturas (`GET /api/invoices` con filtro por estado y paginación) descrito en el contrato existente del proyecto.
- **Idioma**: toda la interfaz del panel se presenta en español.
