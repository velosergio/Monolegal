# Feature Specification: Panel de Administración — Detalle de Factura (Modal) y Dashboard de Estadísticas

**Feature Branch**: `015-admin-panel-detail-dashboard`

**Created**: 2026-06-26

**Status**: Activo

**Input**: User description (roadmap.md, Specs 4.3 Invoice Detail Modal y 4.4 Dashboard / Stats):

> ### Spec 4.3: Invoice Detail Modal
> **GIVEN** usuario hace click en fila de tabla **WHEN** modal se abre **THEN**: muestra todos los campos de factura; historial de cambios de estado; botón "Cambiar Estado" (solo si es transición válida); datos actualizados via TanStack Query.
>
> ### Spec 4.4: Dashboard / Stats
> **GIVEN** usuario en `/dashboard` **WHEN** página carga **THEN**: cards con stats (total, por estado, por cliente); gráficos (motion animados); último refresh mostrado.

## Resumen

Esta feature añade al panel de administración de Monolegal dos capacidades que se apoyan en el listado ya existente (spec 4.2): un **modal de detalle de factura** que se abre al seleccionar una fila y permite revisar toda la información de una factura, su **historial completo de cambios de estado** y **cambiar su estado** cuando existe una transición válida; y un **dashboard de estadísticas** que resume la cartera con tarjetas (total de facturas, distribución por estado y por cliente) y gráficos animados, indicando cuándo fue la última actualización de los datos.

La experiencia mantiene el estándar de calidad de la Fase 4: componentes de interfaz consistentes, animaciones suaves y discretas, estados de carga claros (skeletons), accesibilidad por teclado y lector de pantalla, y verificación honesta de calidad de código.

Para soportarlo, esta feature requiere **dos extensiones acotadas de backend**: persistir y exponer un **historial completo de transiciones de estado** por factura (hoy el sistema solo guarda la fecha de la última transición), y exponer los **estados destino permitidos** para una factura, de modo que el frontend determine la validez del botón "Cambiar Estado" sin duplicar la matriz de transiciones del dominio.

## Clarifications

### Session 2026-06-26

- Q: El roadmap 4.3 pide "Historial de cambios de estado", pero el backend no persiste un historial de transiciones (solo la fecha de la última transición, el contador de recordatorios y la fecha del último recordatorio). ¿Qué muestra el modal? → A: **Audit log completo**. Se extiende el backend para persistir y exponer el historial completo de todas las transiciones de estado de la factura; el modal lo muestra como una línea de tiempo.
- Q: El roadmap separa 4.3 (modal con botón "Cambiar Estado") de 4.5 (formulario de transición manual). ¿El modal solo expone el botón o también ejecuta el cambio? → A: **Ejecuta el cambio completo**. El cambio de estado se realiza dentro del propio modal usando el endpoint de transición existente, sin esperar a la spec 4.5.
- Q: ¿Cómo se determina si una transición es válida para mostrar/habilitar el botón, si la matriz de transiciones vive solo en el dominio backend? → A: **El backend expone los destinos válidos**. El backend devuelve los estados destino permitidos para cada factura y el frontend solo los consume, evitando duplicar la lógica de dominio (alineado con la Constitución).
- Q: ¿Qué hacer con el método muerto `UpdateStatusAsync` (actualización parcial de estado que puede saltarse el historial)? → A: **Eliminarlo por completo** (interfaz del repositorio, implementación Mongo, fakes y tests asociados). El cambio de estado queda con una única vía que siempre registra historial (directriz "no dejar nada en legacy").
- Q: ¿Cómo evitar que las facturas existentes queden sin historial? → A: **Migración de backfill única e idempotente** que siembra un evento de creación en cada factura sin historial. Las transiciones pasadas reales no se reconstruyen ni se inventan.
- Q: ¿Alcance de limpieza de los estados legacy del enum (Borrador/Vencida/Cancelada)? → A: **Eliminarlos del conjunto de estados**. Las facturas nuevas inician en **Pendiente**; los documentos existentes en un estado legacy se migran a un estado activo válido en la misma migración (compatibilidad de datos antes de retirar los valores).
- Q: ¿Qué mapeo aplica la migración para los estados legacy? → A: **Borrador→Pendiente, Vencida→Pendiente, Cancelada→Desactivado**. (Decisión firme; sustituye al mapeo "a confirmar".)
- Q: En un cambio de estado MANUAL desde el modal, ¿se notifica al cliente por correo? → A: **Sí, notificar** reutilizando el mecanismo de notificación de transiciones existente (spec 013); un fallo de envío no revierte el cambio.
- Q: ¿Cómo se mantiene fresco el dashboard y su indicador de "último refresh"? → A: **Carga al montar/navegar + botón manual de actualizar**, sin auto-refresco periódico (polling). El indicador usa el momento real de la última obtención de datos.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consultar el detalle completo de una factura (Priority: P1)

Como administrador, al hacer clic en una fila del listado quiero abrir un modal que muestre toda la información de esa factura —identificador, cliente, monto, estado actual, fechas y datos de recordatorios— para entender su situación sin salir de la página de facturas.

**Why this priority**: Es el valor central de la spec 4.3 y el punto de entrada de todo lo demás (historial y cambio de estado viven dentro del mismo modal). Sin el detalle, no hay nada que mostrar ni sobre lo que actuar.

**Independent Test**: Abrir el listado, hacer clic en una fila y verificar que se abre un modal que muestra todos los campos de la factura con formato legible (monto como moneda, fechas en español), que los datos provienen de una consulta fresca al abrirse, que se puede cerrar (botón, tecla de escape, clic fuera) y que un fallo de carga muestra un mensaje de error con opción de reintento sin romper el listado.

**Acceptance Scenarios**:

1. **Given** el listado de facturas cargado, **When** el usuario hace clic en una fila, **Then** se abre un modal con el detalle de esa factura mostrando identificador, cliente, monto, estado actual, fecha de creación, fecha de última actualización, número de recordatorios enviados, fecha del último recordatorio y fecha de la última transición de estado.
2. **Given** el modal abriéndose, **When** se solicitan los datos de la factura, **Then** se muestra un estado de carga (skeleton) con la forma del contenido y, al resolver, el contenido aparece con una transición suave.
3. **Given** el modal abierto, **When** el usuario pulsa el botón de cierre, la tecla de escape o hace clic fuera del modal, **Then** el modal se cierra y el foco regresa a la fila de origen.
4. **Given** un fallo al obtener el detalle, **When** se renderiza el modal, **Then** se muestra un mensaje de error legible y una acción para reintentar, sin afectar al listado de fondo.

---

### User Story 2 - Revisar el historial de cambios de estado (Priority: P1)

Como administrador, dentro del modal de detalle quiero ver el historial completo de cambios de estado de la factura (qué cambió, desde qué estado, hacia qué estado y cuándo) ordenado cronológicamente, para entender cómo llegó la factura a su estado actual.

**Why this priority**: Es un requisito explícito de la spec 4.3 y un soporte clave para decidir si cambiar el estado. Junto con US1 conforma el detalle útil de una factura.

**Independent Test**: Abrir el modal de una factura que ha tenido varias transiciones y verificar que se muestra una línea de tiempo con cada cambio (estado origen, estado destino, fecha/hora legible y, cuando exista, el origen del cambio —automático del worker o manual), ordenada de forma clara; y abrir el modal de una factura sin transiciones posteriores a su creación, verificando que se muestra al menos el evento de creación o un estado vacío con mensaje claro.

**Acceptance Scenarios**:

1. **Given** una factura con varias transiciones de estado, **When** se abre su modal, **Then** se muestra una línea de tiempo con cada cambio: estado anterior, estado nuevo, fecha/hora legible y, si está disponible, el origen del cambio (automático/manual).
2. **Given** el historial con múltiples eventos, **When** se renderiza, **Then** los eventos aparecen en un orden cronológico claro y consistente (p. ej. del más reciente al más antiguo).
3. **Given** una factura sin cambios de estado registrados más allá de su creación, **When** se abre su modal, **Then** se muestra el evento de creación o un estado vacío con un mensaje claro, sin una sección de historial vacía y sin contexto.
4. **Given** que el estado se acaba de cambiar desde el propio modal (US3), **When** la operación tiene éxito, **Then** el historial se actualiza para reflejar el nuevo evento sin necesidad de reabrir el modal.

---

### User Story 3 - Cambiar el estado de una factura desde el modal (Priority: P1)

Como administrador, dentro del modal quiero cambiar el estado de la factura cuando exista una transición válida, eligiendo entre los estados destino permitidos y confirmando el cambio, para gestionar la cartera sin herramientas externas.

**Why this priority**: Es la acción de valor de la spec 4.3 ("Botón Cambiar Estado") y, por decisión de clarificación, se ejecuta completa dentro de este modal. Convierte el modal de una vista pasiva en una herramienta de gestión.

**Independent Test**: Abrir el modal de una factura que admite al menos una transición y verificar que el botón "Cambiar Estado" está visible/habilitado y ofrece únicamente los estados destino permitidos; ejecutar un cambio válido y verificar que el estado y el historial se actualizan; abrir el modal de una factura sin transiciones válidas y verificar que el botón está oculto o deshabilitado con una indicación clara; e intentar un cambio que el backend rechaza, verificando que se muestra el error sin alterar el estado mostrado.

**Acceptance Scenarios**:

1. **Given** una factura con al menos una transición válida, **When** se abre su modal, **Then** el botón "Cambiar Estado" se muestra habilitado y, al activarlo, ofrece solo los estados destino permitidos para esa factura (provistos por el backend).
2. **Given** una factura sin transiciones válidas (estado terminal o sin destinos permitidos), **When** se abre su modal, **Then** el botón "Cambiar Estado" se oculta o se muestra deshabilitado con una indicación de por qué no hay acción disponible.
3. **Given** el usuario eligiendo un estado destino permitido, **When** confirma el cambio, **Then** se aplica la transición, el estado actual y el historial del modal se actualizan, y los datos del listado de fondo quedan coherentes con el nuevo estado.
4. **Given** un cambio rechazado por el backend (transición no permitida o error), **When** se intenta confirmar, **Then** se muestra un mensaje de error legible y el estado mostrado de la factura no cambia.
5. **Given** una operación de cambio de estado en curso, **When** aún no ha resuelto, **Then** los controles reflejan el estado de carga (p. ej. botón deshabilitado/ocupado) y se evita el doble envío.

---

### User Story 4 - Visión general de la cartera en el dashboard (Priority: P2)

Como administrador, al entrar al dashboard quiero ver de un vistazo el total de facturas y su distribución por estado y por cliente, con gráficos animados, para captar el estado global de la cartera sin recorrer el listado.

**Why this priority**: Es la spec 4.4 completa. Aporta valor analítico transversal, pero depende de datos ya disponibles y es independiente del modal de detalle, por eso es P2 respecto al detalle/cambio de estado.

**Independent Test**: Navegar a la sección de dashboard y verificar que durante la carga se muestran skeletons; que al resolver se muestran tarjetas con el total de facturas, el desglose por estado y el desglose por cliente; que existen gráficos que representan esas distribuciones con animación de entrada suave; que se muestra una indicación de la última actualización de los datos; y que un listado de datos vacío o un fallo muestran mensajes claros.

**Acceptance Scenarios**:

1. **Given** el usuario navegando al dashboard, **When** los datos están en curso, **Then** se muestran skeletons con la forma de las tarjetas y gráficos, no una pantalla en blanco.
2. **Given** los datos de estadísticas resueltos, **When** se renderiza el dashboard, **Then** se muestran tarjetas con el total de facturas, la distribución por estado y la distribución por cliente, con cifras legibles.
3. **Given** las estadísticas disponibles, **When** se renderizan los gráficos, **Then** estos representan las distribuciones por estado y por cliente con una animación de entrada suave (respetando "reducir movimiento").
4. **Given** el dashboard cargado, **When** el usuario lo observa, **Then** se muestra una indicación legible de cuándo se actualizaron por última vez los datos.
5. **Given** un fallo al obtener las estadísticas, **When** se renderiza el dashboard, **Then** se muestra un mensaje de error con opción de reintento, sin romper el resto del panel.

---

### User Story 5 - Acceso a la sección Dashboard desde la navegación (Priority: P3)

Como administrador, quiero que la entrada "Dashboard" de la navegación lateral —hoy deshabilitada— quede habilitada y enrutada, para llegar al dashboard como una sección de primer nivel del panel.

**Why this priority**: Es el "pegamento" de navegación que convierte el dashboard en parte del producto. El dashboard puede construirse y probarse antes (US4), por eso esta historia de habilitación es P3.

**Independent Test**: Verificar que la entrada "Dashboard" de la navegación lateral está habilitada (ya no marcada como "próximamente"), que al seleccionarla se navega a la ruta del dashboard, que la sección activa queda resaltada, y que la navegación entre Facturas y Dashboard funciona en escritorio y móvil.

**Acceptance Scenarios**:

1. **Given** el panel cargado, **When** el usuario abre la navegación lateral, **Then** la entrada "Dashboard" aparece habilitada (sin la marca "próximamente") junto a "Facturas".
2. **Given** la navegación lateral, **When** el usuario selecciona "Dashboard", **Then** se navega a la ruta del dashboard y la sección activa queda resaltada.
3. **Given** el usuario en el dashboard, **When** selecciona "Facturas", **Then** vuelve al listado, manteniendo una navegación coherente en escritorio y móvil.

---

### Edge Cases

- **Factura inexistente o id inválido**: al intentar abrir el detalle de una factura que ya no existe → el modal muestra un mensaje claro de "no encontrada" en lugar de contenido vacío.
- **Historial vacío o solo creación**: factura sin transiciones posteriores → se muestra el evento de creación o un estado vacío con mensaje, nunca una sección de historial en blanco.
- **Estado terminal**: factura en un estado sin transiciones válidas (p. ej. pagada/desactivada) → botón "Cambiar Estado" oculto o deshabilitado con indicación.
- **Transición que pierde validez**: el backend rechaza un cambio que el frontend creía válido (datos desactualizados) → se muestra el error y se refrescan los datos; el estado mostrado no cambia indebidamente.
- **Cambio de estado concurrente**: el estado cambió en otra parte mientras el modal estaba abierto → al cambiar/refrescar, el modal refleja el estado real del backend.
- **Dashboard sin datos**: no hay facturas en el sistema → las tarjetas muestran cero de forma legible y los gráficos muestran un estado vacío claro, no un gráfico roto.
- **Distribución por cliente muy amplia**: muchos clientes distintos → el desglose por cliente se presenta de forma legible (p. ej. principales clientes y agrupación del resto) sin desbordar el layout.
- **Estado desconocido/legacy**: una factura con un estado no contemplado en el flujo activo → se muestra con una etiqueta neutra y su valor, sin romper el modal ni el dashboard.
- **Datos desactualizados**: tras un cambio de estado, el listado, el modal y el dashboard deben converger a la información más reciente sin requerir recarga manual de la página.
- **Movimiento reducido**: con la preferencia de "reducir movimiento" activa, las animaciones de modal y gráficos se atenúan o desactivan sin impedir el uso.
- **Documentos en estado legacy**: facturas almacenadas en Borrador/Vencida/Cancelada se migran (Borrador→Pendiente, Vencida→Pendiente, Cancelada→Desactivado) antes de retirar los valores legacy; tras la migración no existen documentos con estados no soportados, y el modal/listado no muestran estados legacy.
- **Factura sin historial tras migración**: la migración de backfill es idempotente; reejecutarla no duplica el evento de creación ni altera facturas que ya tienen historial.

## Requirements *(mandatory)*

### Functional Requirements

#### Modal de detalle de factura — roadmap 4.3

- **FR-001**: El listado MUST permitir abrir un modal de detalle al activar una fila (clic o teclado), identificando inequívocamente la factura seleccionada.
- **FR-002**: El modal MUST mostrar todos los campos relevantes de la factura: identificador, cliente, monto, estado actual, fecha de creación, fecha de última actualización, número de recordatorios enviados, fecha del último recordatorio y fecha de la última transición de estado.
- **FR-003**: El modal MUST formatear los datos de manera legible en español: el monto como moneda y las fechas/horas en formato legible; el estado MUST mostrarse como etiqueta de color coherente con el listado.
- **FR-004**: El modal MUST obtener los datos de la factura mediante una consulta fresca al abrirse y mantenerlos sincronizados con el estado de servidor, de modo que reflejen cambios realizados desde el propio modal sin recargar la página.
- **FR-005**: Mientras el detalle se carga, el modal MUST mostrar un estado de carga (skeleton) con la forma del contenido; ante un fallo MUST mostrar un mensaje de error legible con acción de reintento, sin romper el listado de fondo.
- **FR-006**: El modal MUST ser cerrable mediante botón de cierre, tecla de escape y clic fuera del contenido, devolviendo el foco al elemento de origen.

#### Historial de cambios de estado — roadmap 4.3

- **FR-007**: El sistema MUST registrar y persistir cada transición de estado de una factura (estado anterior, estado nuevo, fecha/hora y, cuando aplique, el origen del cambio: automático del worker o manual), de modo que exista un historial completo consultable. *(Extensión de backend.)*
- **FR-008**: El backend MUST exponer el historial de transiciones de una factura para su consumo por el panel.
- **FR-009**: El modal MUST mostrar el historial de cambios de estado como una línea de tiempo ordenada cronológicamente de forma clara, indicando para cada evento el estado anterior, el estado nuevo, la fecha/hora legible y, si está disponible, el origen del cambio.
- **FR-010**: Cuando una factura no tenga transiciones registradas más allá de su creación, el modal MUST mostrar el evento de creación o un estado vacío con mensaje claro, evitando una sección de historial sin contexto.
- **FR-011**: Tras un cambio de estado exitoso realizado desde el modal, el historial mostrado MUST reflejar el nuevo evento sin requerir reabrir el modal.

#### Cambio de estado desde el modal — roadmap 4.3 (acción completa)

- **FR-012**: El backend MUST exponer, para una factura dada, el conjunto de estados destino permitidos según la matriz de transiciones del dominio, como única fuente de verdad de la validez. *(Extensión de backend.)*
- **FR-013**: El modal MUST mostrar el botón "Cambiar Estado" habilitado únicamente cuando existe al menos una transición válida; cuando no existan transiciones válidas, el botón MUST ocultarse o deshabilitarse con una indicación clara del motivo.
- **FR-014**: El control de cambio de estado MUST ofrecer al usuario solo los estados destino permitidos para esa factura (provistos por el backend), sin replicar la matriz de transiciones en el frontend.
- **FR-015**: Al confirmar un estado destino permitido, el sistema MUST aplicar la transición mediante el flujo de transición existente y, al tener éxito, actualizar el estado actual mostrado, el historial del modal y la información del listado de fondo, manteniéndolos coherentes.
- **FR-016**: Ante un rechazo del cambio por el backend (transición no permitida o error), el modal MUST mostrar un mensaje de error legible sin alterar el estado mostrado, y MUST refrescar los datos para reflejar la realidad del backend.
- **FR-017**: Durante una operación de cambio de estado en curso, los controles MUST reflejar el estado de carga y prevenir el doble envío.
- **FR-017a**: Un cambio de estado manual realizado desde el modal MUST notificar al cliente reutilizando el mecanismo de notificación de transiciones existente (spec 013), igual que las transiciones automáticas. Un fallo en el envío del correo MUST NOT revertir el cambio de estado ni hacer fallar la operación.

#### Dashboard de estadísticas — roadmap 4.4

- **FR-018**: El panel MUST ofrecer una sección de dashboard accesible desde la navegación lateral, habilitando la entrada "Dashboard" (hoy deshabilitada) y resaltándola como activa cuando corresponde.
- **FR-019**: El dashboard MUST mostrar tarjetas con: el total de facturas, la distribución por estado y la distribución por cliente, con cifras legibles.
- **FR-020**: El dashboard MUST representar las distribuciones por estado y por cliente mediante gráficos con animación de entrada suave.
- **FR-021**: El dashboard MUST mostrar una indicación legible de cuándo se obtuvieron por última vez los datos (último refresh), basada en el momento real de la última obtención.
- **FR-021a**: El dashboard MUST cargar los datos al montar/navegar a la sección y MUST ofrecer un control manual para actualizarlos bajo demanda. NO MUST realizar auto-refresco periódico (polling) en segundo plano.
- **FR-022**: Mientras las estadísticas se cargan, el dashboard MUST mostrar skeletons con la forma de las tarjetas y gráficos; ante un fallo MUST mostrar un mensaje de error con acción de reintento, sin romper el resto del panel.
- **FR-023**: Cuando no existan datos (sin facturas), el dashboard MUST mostrar ceros legibles y estados vacíos claros en lugar de gráficos rotos.

#### Experiencia, animación y calidad — estándar de la Fase 4

- **FR-024**: Las transiciones clave (apertura/cierre del modal, carga → contenido, entrada de gráficos, cambios de estado) MUST ser suaves y discretas, sin saltos de layout perceptibles ni parpadeos.
- **FR-025**: Todas las animaciones MUST respetar la preferencia del sistema de "reducir movimiento", atenuándose o desactivándose cuando esté activa.
- **FR-026**: Todos los elementos interactivos (apertura del modal, controles de cambio de estado, cierre, navegación al dashboard, gráficos/tarjetas con interacción) MUST ser operables por teclado, con foco visible y gestión correcta del foco del modal (trap y retorno), expuestos de forma accesible a tecnologías de asistencia (WCAG A como mínimo).
- **FR-027**: El modal y el dashboard MUST mantener consistencia visual con el resto del panel usando el mismo sistema de componentes de interfaz (tipografía, espaciado, colores y estados de interacción).
- **FR-028**: El código de la feature MUST alcanzar una puntuación de 100/100 en la herramienta de inspección de React del proyecto **de forma honesta**, sin suprimir, ignorar ni silenciar avisos para inflar la puntuación.

#### Integridad de datos y eliminación de legacy — clarificación 2026-06-26

- **FR-029**: El cambio de estado MUST tener una **única vía** que registre el evento de historial. El sistema MUST NOT conservar caminos alternos de actualización de estado que no registren historial (se elimina el método de actualización parcial de estado sin historial). Verificable: no existe ninguna operación que altere el estado de una factura sin añadir su evento de historial.
- **FR-030**: Toda factura MUST disponer de al menos un evento de historial consultable. Las facturas creadas antes de esta feature MUST recibir, mediante una **migración única e idempotente**, un evento de creación; las transiciones pasadas no reconstruibles NO se inventan. Verificable: tras la migración, ninguna factura queda con historial vacío.
- **FR-031**: El sistema MUST retirar los estados legacy (Borrador/Vencida/Cancelada) del conjunto de estados soportados. Las facturas nuevas MUST iniciar en **Pendiente** (ya no en Borrador). Los documentos existentes en un estado legacy MUST migrarse a un estado activo válido **como parte de la misma migración**, antes de retirar los valores legacy, de modo que tras la migración no existan facturas en estados no soportados.

### Key Entities *(include if feature involves data)*

- **Factura (Invoice)**: factura del sistema. Para el detalle se consideran: identificador, cliente, monto, estado actual, fecha de creación, fecha de última actualización, número de recordatorios enviados, fecha del último recordatorio y fecha de la última transición de estado.
- **Evento de historial de estado (Transición)**: registro de un cambio de estado de una factura, con estado anterior, estado nuevo, fecha/hora del cambio y origen (automático del worker o manual). El conjunto ordenado de estos eventos conforma el historial de la factura.
- **Estados destino permitidos**: para una factura concreta, el conjunto de estados a los que puede transicionar según la matriz de transiciones del dominio; determina la disponibilidad y las opciones del cambio de estado.
- **Estadísticas de cartera (Stats)**: agregados de la colección de facturas: total de facturas, conteo por estado y conteo por cliente; base de las tarjetas y gráficos del dashboard.
- **Estado de factura**: clasificación de la factura dentro del **conjunto activo** (pendiente, primer recordatorio, segundo recordatorio, desactivado, pagado) que determina la etiqueta de color, las transiciones válidas y las agregaciones. Los estados legacy (borrador/vencida/cancelada) se retiran del sistema (ver FR-031); no son válidos como estado de factura tras la migración.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Desde el listado, un administrador puede abrir el detalle de una factura concreta y comprender su estado e información clave en menos de 5 segundos.
- **SC-002**: El historial de una factura permite reconstruir la secuencia completa de sus cambios de estado (origen, destino y momento) sin recurrir a otra herramienta.
- **SC-003**: El botón "Cambiar Estado" solo está disponible cuando hay una transición válida, y el usuario nunca puede seleccionar un estado destino no permitido para esa factura.
- **SC-004**: Tras un cambio de estado exitoso, el estado actual, el historial y el listado reflejan la nueva información sin que el usuario recargue la página.
- **SC-005**: En el dashboard, un administrador capta el total de facturas y su distribución por estado y por cliente en menos de 10 segundos desde que cargan los datos.
- **SC-006**: En toda carga de datos (modal y dashboard) el usuario ve skeletons con la forma del contenido; en ningún flujo aparece una pantalla en blanco como único indicador.
- **SC-007**: El modal y el dashboard son completamente utilizables solo con teclado, con foco siempre visible y gestión correcta del foco del modal (no se pierde ni se "escapa" detrás del modal).
- **SC-008**: Con la preferencia de "reducir movimiento" activa, ninguna animación esencial (modal o gráficos) impide completar las tareas.
- **SC-009**: El modal y el dashboard se muestran correctamente, sin desbordamiento horizontal ni solapamientos, en anchos representativos de móvil y de escritorio.
- **SC-010**: La herramienta de inspección de React del proyecto reporta 100/100 para el código de la feature sin avisos suprimidos artificialmente, y se mantienen los presupuestos de rendimiento del panel (TTI < 2s, rendimiento > 90 en auditoría estándar).

## Assumptions

- **Alcance**: esta feature cubre las specs 4.3 (Modal de Detalle de Factura, incluyendo la ejecución completa del cambio de estado) y 4.4 (Dashboard / Stats) del roadmap. El formulario de transición manual descrito de forma independiente en la spec 4.5 queda **absorbido** en lo necesario para que el cambio de estado funcione dentro del modal; cualquier alcance adicional de 4.5 se evaluará por separado.
- **Dependencia de backend — historial**: el sistema actual solo persiste la fecha de la última transición (`LastStatusTransitionAt`), el contador de recordatorios y la fecha del último recordatorio, no un historial completo. Esta feature **extiende el backend** para registrar y exponer un historial completo de transiciones de estado (estado anterior, estado nuevo, fecha/hora y origen), habilitando FR-007 a FR-011.
- **Dependencia de backend — transiciones válidas**: la matriz de transiciones válidas vive hoy solo en el dominio del backend (`InvoiceTransitionService`). Esta feature **extiende el backend** para exponer los estados destino permitidos por factura (FR-012), de modo que el frontend determine la validez sin duplicar la matriz, conforme a la Constitución (no propagar lógica de dominio fuera de su capa).
- **Estadísticas disponibles**: se asume disponible el endpoint de estadísticas (`GET /api/invoices/stats`) que ya devuelve total, conteo por estado y conteo por cliente; el dashboard lo consume sin cambios de contrato.
- **Detalle y transición disponibles**: se asume disponible el endpoint de detalle (`GET /api/invoices/{id}`) y el de transición manual (`POST /api/invoices/transition/{id}`), reutilizados por el modal.
- **Stack mandatado**: por la Constitución, la interfaz se construye con componentes shadcn/ui sobre React 19 + Vite + Tailwind, animaciones con Motion, estado de servidor con TanStack Query y verificación de calidad con React Doctor (requisitos del proyecto, no decisiones abiertas de esta spec).
- **Identidad del cliente**: el cliente se identifica por su `clientId` mientras no exista una entidad de cliente con nombre; cuando exista, "Cliente" podrá mostrar el nombre sin cambiar el alcance.
- **Ruteo del dashboard**: el panel ya usa enrutamiento por rutas (`/facturas`, `/configuracion`); esta feature añade/habilita la ruta del dashboard y la entrada de navegación correspondiente.
- **Autenticación**: se asume que el acceso al panel está protegido a nivel de aplicación (rol Admin) por una capa previa; esta feature no implementa autenticación.
- **Idioma**: toda la interfaz se presenta en español.
- **Sin legacy (código y datos)**: por la directriz "no dejar nada en legacy", esta feature (a) elimina el método muerto de actualización parcial de estado (`UpdateStatusAsync`) y sus tests asociados, (b) ejecuta una migración única e idempotente que siembra el evento de creación en las facturas sin historial y (c) retira los estados legacy del enum, migrando antes los documentos que los tengan a un estado activo válido y cambiando el estado inicial de una factura nueva a Pendiente.
- **Mapeo de migración de estados legacy (firme, clarificación 2026-06-26)**: Borrador→Pendiente, Vencida→Pendiente, Cancelada→Desactivado. Además del requisito firme de que ningún documento quede en estado no soportado (FR-031), este es el mapeo concreto que aplica la migración.
- **Backfill de historial**: la migración solo puede sembrar un evento de creación (estado inicial en el momento de creación). Las transiciones intermedias previas a esta feature no se reconstruyen, ya que no se registraban; el historial es completo y fiable solo a partir de la entrada en vigor de la feature.
