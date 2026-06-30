# Feature Specification: Vista de Envíos — Estado de notificaciones por factura y acciones manuales

**Feature Branch**: `019-vista-envios`

**Created**: 2026-06-27

**Status**: Activo

**Input**: User description:

> /speckit-specify Spec 4.9: Vista Envíos
>
> **GIVEN** usuario en `/envios` **WHEN** página carga **THEN**: listado de facturas con su estado de envío (pendiente, enviado, fallido, reintentando); columnas ID, Cliente, Email, Estado de envío, Último intento, Reintentos; filtro por estado de envío y búsqueda por cliente/email; acciones manuales (reenviar email, reintentar fallidos, cancelar envío); POST a endpoints de reenvío/reintento (`/api/invoices/{id}/resend`); indicadores visuales (badges de color) por estado de envío; toast de éxito/error y refresco automático vía TanStack Query; estados de carga (skeletons) y vacíos (empty states).

## Resumen

Esta feature añade la vista `/envios` al panel de administración: un **centro operativo de los envíos de correo por factura**. Mientras que `/configuracion` (spec 017) ofrece herramientas **globales/masivas** sobre los envíos (reenviar todos los fallidos, sanear atascados), esta vista da **visibilidad y control por factura individual**: ver el estado de notificación de cada factura, filtrar y buscar, y ejecutar **acciones manuales por factura** (reenviar la notificación, reintentar las fallidas, cancelar un envío en curso).

La vista convierte en operación cotidiana del administrador lo que hoy está fragmentado: el resultado de la última notificación de cada factura (`LastNotificationOutcome`, `LastNotificationAt`, `LastNotificationError`) deja de ser un dato interno para volverse una **tabla accionable** con indicadores visuales por estado, retroalimentación tipo *toast*, refresco automático del listado tras cada acción y los estados de carga/vacío propios del estándar de calidad de la Fase 4.

## Clarifications

### Session 2026-06-27

- Q: El modelo actual registra por factura un único resultado de la última notificación (`None`/`Sent`/`Skipped`/`Failed`), sin contador de reintentos ni estados persistidos "pendiente"/"reintentando". ¿Cómo se obtienen el estado "reintentando" y la columna "Reintentos" que pide el roadmap? → A: **Seguimiento persistente de reintentos en el dominio**: se introduce un **contador de reintentos** persistido por factura y un estado **"reintentando"** persistido, con los cambios necesarios en dominio, repositorio y worker para mantenerlos. El listado muestra el contador real y el estado "reintentando" mientras un reintento está en curso/programado.
- Q: El envío de notificaciones es **síncrono en el worker** (spec 017: "no existe una cola explícita: el envío es síncrono"). ¿Qué significa entonces la acción "cancelar envío"? → A: **"Marcar como omitido / no notificar"**: "cancelar envío" marca una factura **pendiente** como **omitida** para que el worker **no la procese**; es una acción de dominio (cambia el estado de notificación a omitido) que requiere confirmación explícita y conserva el registro para auditoría.
- Q: ¿Qué facturas componen el listado de la vista de envíos? → A: Todas las facturas que se encuentran en un **estado notificable** (estados con plantilla aplicable: recordatorios, pagado, desactivado), mostrando su última notificación; las facturas en estados sin notificación aplicable no aparecen en el listado por defecto. (Asunción razonable; ver Assumptions.)
- Q: ¿El estado "reintentando" implica reintento automático del worker o es solo transitorio durante una acción manual? → A: **Solo transitorio durante una acción manual** (reenviar/reintentar) en curso; el worker **no** hace reintentos automáticos (sin backoff ni programación). El estado "reintentando" se activa mientras una operación manual de reenvío/reintento está en curso para esa factura.
- Q: ¿"Reintentar fallidos" actúa sobre todos los fallidos del sistema o solo sobre el conjunto filtrado/visible? → A: **Global**: reintenta **todos** los fallidos del sistema reutilizando el endpoint masivo existente `POST /api/settings/email/tools/resend-failed` (spec 017); no hay reintento acotado al subconjunto filtrado (el reenvío por factura cubre casos individuales).
- Q: ¿Cuándo se reinicia a 0 el contador de "Reintentos"? → A: Se **reinicia a 0 cuando la factura cambia a un nuevo estado notificable** (cuando hay una nueva notificación que enviar); el contador cuenta los reintentos del **aviso vigente**, no el histórico de por vida.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ver y entender el estado de envío de cada factura (Priority: P1)

Como administrador, al abrir `/envios` quiero ver un **listado de facturas con el estado de su notificación por correo** (pendiente, enviado, fallido y, si aplica, reintentando), con columnas claras — identificador, cliente, correo de destino, estado de envío, último intento y número de reintentos — e **indicadores visuales por estado** (insignias de color), de modo que de un vistazo entienda qué facturas están al día con sus avisos y cuáles requieren mi atención.

**Why this priority**: Es el núcleo de la vista y la precondición de toda acción: sin visibilidad fiable del estado de envío por factura, las acciones manuales (reenviar, reintentar, cancelar) carecen de contexto. Entrega valor por sí sola como tablero de monitoreo.

**Independent Test**: Abrir `/envios` con facturas en distintos estados de notificación y verificar que cada fila muestra identificador, cliente, correo, estado de envío con su insignia de color correspondiente, fecha/hora del último intento y el número de reintentos; verificar que los estados se distinguen visualmente y también por etiqueta textual (no solo por color).

**Acceptance Scenarios**:

1. **Given** el administrador en `/envios`, **When** la página carga con facturas notificables, **Then** se muestra una tabla con una fila por factura y las columnas: identificador, cliente, correo de destino, estado de envío, último intento y reintentos.
2. **Given** una factura cuya última notificación se envió con éxito, **When** se renderiza su fila, **Then** su estado de envío se muestra como **enviado** con la insignia de color correspondiente y la fecha/hora del último intento.
3. **Given** una factura cuya última notificación falló, **When** se renderiza su fila, **Then** su estado de envío se muestra como **fallido** con su insignia y, cuando exista, una indicación del motivo del error accesible (p. ej. al pasar el cursor o en detalle).
4. **Given** una factura notificable que aún no tiene intento registrado, **When** se renderiza su fila, **Then** su estado de envío se muestra como **pendiente**.
5. **Given** la información transmitida por color, **When** un usuario no distingue colores, **Then** cada estado cuenta además con una **etiqueta textual** legible.

---

### User Story 2 - Reenviar manualmente la notificación de una factura (Priority: P1)

Como administrador, quiero **reenviar la notificación por correo de una factura concreta** desde su fila, para recuperar de inmediato un aviso fallido o repetir un envío puntual sin recurrir a las herramientas masivas ni al equipo técnico.

**Why this priority**: Es la acción manual de mayor valor operativo y la que el roadmap nombra explícitamente (`POST /api/invoices/{id}/resend`). Convierte la vista de pasiva a accionable y cubre el caso más frecuente (recuperar un fallo puntual).

**Independent Test**: En `/envios`, sobre una factura en estado fallido (o enviado), pulsar "Reenviar", confirmar el estado de carga del control, y verificar el *toast* de éxito (con la factura/destino) o de error (con motivo), y que el listado se **refresca automáticamente** reflejando el nuevo último intento.

**Acceptance Scenarios**:

1. **Given** una factura del listado, **When** el administrador pulsa "Reenviar" en su fila, **Then** el sistema solicita el reenvío de la notificación de esa factura y refleja un estado de carga en el control mientras la operación está en curso.
2. **Given** un reenvío que el backend acepta y completa, **When** la operación termina con éxito, **Then** se muestra un *toast* de éxito y el listado se **refresca automáticamente** mostrando el nuevo estado y la nueva fecha de último intento.
3. **Given** un reenvío que falla (proveedor caído, correo inválido, error del backend), **When** la operación termina, **Then** se muestra un *toast* de error con el motivo cuando esté disponible y el listado refleja el estado real tras el intento.
4. **Given** un reenvío en curso, **When** el administrador interactúa, **Then** el control evita el **doble envío** mientras la petición está pendiente.
5. **Given** una factura sin correo de destino resoluble, **When** el administrador intenta reenviar, **Then** el sistema lo impide o lo reporta como no aplicable con una indicación clara, sin un envío sin sentido.

---

### User Story 3 - Filtrar por estado de envío y buscar por cliente/correo (Priority: P2)

Como administrador, quiero **filtrar el listado por estado de envío** y **buscar por cliente o correo**, para concentrarme rápidamente en las facturas que requieren acción (p. ej. todas las fallidas) sin recorrer toda la tabla.

**Why this priority**: Multiplica la utilidad del tablero a medida que crece el volumen de facturas, pero depende de que el listado (US1) ya exista. Es independientemente testeable y entrega valor inmediato de productividad.

**Independent Test**: En `/envios`, aplicar el filtro "fallido" y verificar que solo aparecen facturas fallidas; escribir parte de un nombre de cliente o de un correo en la búsqueda y verificar que el listado se reduce a las coincidencias; combinar filtro y búsqueda y verificar el resultado.

**Acceptance Scenarios**:

1. **Given** facturas en varios estados, **When** el administrador selecciona un estado de envío en el filtro, **Then** el listado muestra únicamente las facturas en ese estado.
2. **Given** el administrador escribe texto en la búsqueda, **When** el texto coincide parcialmente con el nombre de un cliente o un correo, **Then** el listado se reduce a las filas coincidentes.
3. **Given** un filtro y una búsqueda activos a la vez, **When** ambos se aplican, **Then** el listado muestra solo las filas que satisfacen ambas condiciones.
4. **Given** un filtro/búsqueda sin coincidencias, **When** no hay resultados, **Then** se muestra un **estado vacío** claro que distingue "sin coincidencias" de "no hay envíos".
5. **Given** filtros aplicados, **When** el administrador los limpia, **Then** el listado vuelve a mostrar todas las facturas notificables.

---

### User Story 4 - Acciones por lote: reintentar fallidos y cancelar envío (Priority: P3)

Como administrador, quiero acciones de conveniencia para **reintentar todas las notificaciones fallidas** (acción global del sistema) y, cuando aplique, **cancelar un envío**, para recuperar la operación de forma eficiente sin actuar fila por fila.

**Why this priority**: Aporta eficiencia operativa pero es secundaria frente a la visibilidad (US1) y al reenvío individual (US2); "reintentar fallidos" se apoya en la capacidad masiva ya existente (spec 017) y "cancelar envío" depende de la aclaración de su semántica.

**Independent Test**: En `/envios`, con facturas fallidas presentes, ejecutar "Reintentar fallidos" y verificar la confirmación previa (si aplica), el *toast* de resultado con el conteo afectado y el refresco del listado; verificar el comportamiento de "cancelar envío" según la semántica acordada.

**Acceptance Scenarios**:

1. **Given** facturas en estado fallido, **When** el administrador ejecuta "Reintentar fallidos", **Then** el sistema reintenta el envío de **todas** las notificaciones fallidas del sistema y muestra un *toast* con el resultado y el número de elementos afectados, refrescando el listado.
2. **Given** una acción de lote sin elementos sobre los que actuar, **When** se ejecuta, **Then** se informa con un mensaje claro de "nada que procesar", sin tratarlo como error.
3. **Given** una factura **pendiente**, **When** el administrador inicia "Cancelar envío" y confirma, **Then** el sistema marca la factura como **omitida** (el worker no la procesará), conserva su registro, reporta el resultado con *toast* y refresca el listado; sobre una factura no pendiente la acción está deshabilitada o se reporta como no aplicable.
4. **Given** una acción de lote potencialmente destructiva, **When** el administrador la inicia, **Then** el sistema solicita **confirmación explícita** antes de ejecutarla.

---

### Edge Cases

- **Sin envíos**: cuando no hay facturas notificables, la vista muestra un **estado vacío** específico distinto del de "sin coincidencias de filtro".
- **Carga inicial**: mientras se obtiene el listado se muestran **skeletons**, sin pantallas en blanco ni saltos de layout.
- **Error de carga**: si el listado no se puede obtener, se muestra un estado de error legible con opción de reintentar la carga.
- **Estado "reintentando" / "Reintentos"**: el contador de reintentos es **persistido**; "reintentando" es un estado **transitorio** que la vista muestra solo mientras una acción manual de reenvío/reintento está en curso (el worker no reintenta automáticamente).
- **Cancelar sin nada que cancelar**: si la factura no está **pendiente**, "Cancelar envío" se deshabilita o informa que no hay envío pendiente que cancelar.
- **Factura sin correo de destino**: las filas sin correo resoluble se muestran con indicación clara y las acciones de envío se deshabilitan o reportan no aplicable.
- **Doble acción**: los controles de reenvío/reintento se deshabilitan mientras la operación está en curso para evitar duplicados.
- **Refresco concurrente**: si otro administrador o el worker cambian el estado de una factura, el refresco automático tras una acción muestra el estado real del backend.
- **Toast con/sin motivo**: si el backend no aporta motivo legible, el *toast* de error muestra un mensaje genérico claro.
- **Estado "omitido" (Skipped)**: las facturas cuyo último resultado fue "omitido" (sin plantilla aplicable o sin destinatario) se representan de forma legible y no se confunden con "enviado" ni con "fallido".
- **Movimiento reducido**: con "reducir movimiento" activa, las animaciones de toasts, skeletons y transiciones se atenúan sin ocultar información.

## Requirements *(mandatory)*

### Functional Requirements

#### Listado y estado de envío

- **FR-001**: La vista `/envios` MUST mostrar un **listado de facturas** con su **estado de envío** derivado del resultado de su última notificación, contemplando los estados **pendiente**, **enviado**, **fallido** y **reintentando** (este último mientras un reintento está en curso/programado).
- **FR-001a**: El sistema MUST representar un estado **"reintentando"** por factura, distinguible de pendiente/enviado/fallido, activo **únicamente mientras una acción manual de reenvío/reintento está en curso** para esa factura. El worker MUST NOT realizar reintentos automáticos (sin backoff ni programación); no hay un estado "reintentando" de larga duración asociado a una cola de reintento automático.
- **FR-002**: Cada fila MUST mostrar las columnas: **identificador de factura**, **cliente**, **correo de destino**, **estado de envío**, **último intento** (fecha/hora) y **reintentos**.
- **FR-002a**: El sistema MUST mantener de forma **persistente** un **contador de reintentos** por factura, incrementado en cada intento de notificación posterior al primero (worker o acción manual), y la vista MUST mostrar su valor real en la columna "Reintentos".
- **FR-002b**: El contador de reintentos MUST **reiniciarse a 0 cuando la factura cambia a un nuevo estado notificable** (nuevo aviso a enviar); cuenta los reintentos del **aviso vigente**, no un histórico de por vida.
- **FR-003**: El estado de envío MUST representarse con un **indicador visual** (insignia de color) por estado y MUST acompañarse siempre de una **etiqueta textual** legible (la información no se transmite solo por color).
- **FR-004**: La vista MUST presentar **estados de carga** mediante *skeletons* durante la obtención del listado y MUST presentar **estados vacíos** diferenciados para "no hay envíos" y "sin coincidencias de filtro".
- **FR-005**: Cuando una notificación haya fallado, la fila MUST permitir conocer el **motivo del error** cuando esté disponible, de forma accesible.
- **FR-006**: El listado MUST estar **paginado o acotado** conforme a la política del proyecto (sin consultas sin límite), manteniendo el rendimiento del panel.

#### Filtro y búsqueda

- **FR-007**: La vista MUST permitir **filtrar por estado de envío** (al menos pendiente, enviado, fallido y, si aplica, reintentando).
- **FR-008**: La vista MUST permitir **buscar por cliente y por correo de destino** mediante coincidencia parcial.
- **FR-009**: El filtro y la búsqueda MUST poder **combinarse**, mostrando solo las filas que satisfacen todas las condiciones activas, y MUST poder **limpiarse** para restaurar el listado completo.

#### Acciones manuales por factura

- **FR-010**: La vista MUST ofrecer una acción de **reenviar la notificación de una factura** individual mediante `POST /api/invoices/{id}/resend`, con estado de carga en el control y prevención de **doble envío**.
- **FR-011**: El resultado del reenvío MUST reportarse con *toast* de **éxito** (identificando la factura/destino) o de **error** (con motivo cuando esté disponible).
- **FR-012**: Tras cualquier acción que modifique el estado de envío, el listado MUST **refrescarse automáticamente** para reflejar el estado real (vía la capa de estado de servidor del panel).
- **FR-013**: Si una factura no tiene **correo de destino** resoluble, la acción de reenvío MUST impedirse o reportarse como **no aplicable** con indicación clara, sin realizar un envío sin sentido.

#### Acciones de conveniencia (lote)

- **FR-014**: La vista MUST ofrecer una acción **"Reintentar fallidos"** que reintente **todas** las notificaciones en estado **fallido** del sistema (acción global), **reutilizando** el endpoint masivo existente `POST /api/settings/email/tools/resend-failed` (spec 017), informando el **resultado** y el **número de elementos afectados**, y tratando "sin elementos" como mensaje informativo (no error). El reintento acotado a un subconjunto filtrado queda **fuera de alcance** (los casos individuales se cubren con el reenvío por factura, FR-010).
- **FR-015**: La vista MUST ofrecer la acción **"Cancelar envío"** que marca una factura **pendiente** como **omitida** para que el worker **no la procese**. La acción MUST solicitar **confirmación explícita** antes de ejecutarse, MUST **conservar el registro** de la factura (no borra), MUST reportar el resultado con *toast* y refrescar el listado, y MUST estar **deshabilitada o reportarse como no aplicable** cuando la factura no esté pendiente (no hay envío pendiente que cancelar).

#### Experiencia, accesibilidad y calidad — estándar de la Fase 4

- **FR-016**: Todos los elementos interactivos (filtros, búsqueda, botones de acción y *toasts*) MUST ser operables por teclado, con foco visible, en español y consistentes con el resto del panel (tipografía, espaciado, colores, dark mode *built-in*).
- **FR-017**: El mecanismo de *toast* MUST ser accesible (anunciado a tecnologías de asistencia) y MUST respetar la preferencia de "reducir movimiento", al igual que skeletons y transiciones.
- **FR-018**: La vista MUST mostrarse correctamente, sin desbordamiento horizontal ni solapamientos, en anchos representativos de **móvil** y de **escritorio**.
- **FR-019**: El código de la feature MUST alcanzar **100/100 en la herramienta de inspección de React del proyecto de forma honesta**, sin suprimir avisos, y MUST mantener los presupuestos de rendimiento del panel (TTI < 2s).

### Key Entities *(include if feature involves data)*

- **Factura (vista de envío)**: la factura tal como se representa en este listado — identificador, cliente asociado, correo de destino, y los campos de su última notificación: tipo, **resultado** (`None`/`Sent`/`Skipped`/`Failed` → pendiente/omitido/enviado/fallido), **fecha del último intento** y **motivo de error** cuando aplique.
- **Estado de envío**: estado de la notificación de la factura — pendiente, enviado, fallido, **reintentando** y omitido — derivado del resultado de la última notificación; "reintentando" es un estado **transitorio** mientras una acción manual está en curso. Cada estado tiene su insignia de color y etiqueta textual.
- **Reintentos**: **contador persistido** del número de intentos de notificación posteriores al primero para el **aviso vigente** de la factura, mantenido por el worker y por las acciones manuales; se **reinicia a 0** cuando la factura pasa a un nuevo estado notificable.
- **Resultado de acción de lote**: conteos devueltos por las acciones masivas (p. ej. intentados/reenviados/fallidos, o saneados) que la vista presenta tras "Reintentar fallidos".

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un administrador puede identificar todas las facturas con notificación fallida en menos de 10 segundos usando el filtro por estado, sin recorrer manualmente la tabla completa.
- **SC-002**: El estado de envío de cada factura es distinguible tanto por color como por etiqueta textual en el 100% de las filas.
- **SC-003**: Al reenviar la notificación de una factura, el administrador recibe un resultado explícito (éxito o error con motivo) y ve el listado reflejar el nuevo estado en una sola acción, sin recargar manualmente la página.
- **SC-004**: La búsqueda por cliente o correo reduce el listado a las coincidencias en el 100% de los casos con coincidencia parcial, y los estados vacíos distinguen "sin coincidencias" de "no hay envíos".
- **SC-005**: Las acciones de lote informan el resultado y el número de elementos afectados en el 100% de las ejecuciones, y solicitan confirmación antes de cualquier acción destructiva.
- **SC-006**: La vista presenta skeletons durante la carga y nunca muestra una pantalla en blanco ni salto de layout perceptible al cargar el listado.
- **SC-007**: Toda la vista es completamente operable solo con teclado, con foco siempre visible.
- **SC-008**: La vista se muestra correctamente, sin desbordamiento horizontal ni solapamientos, en anchos representativos de móvil y de escritorio.
- **SC-009**: La herramienta de inspección de React del proyecto reporta 100/100 para el código de la feature sin avisos suprimidos artificialmente, y se mantienen los presupuestos de rendimiento del panel (TTI < 2s).

## Assumptions

- **Punto de partida del modelo**: hoy cada factura registra únicamente el resultado de su **última** notificación (`LastNotificationType`, `LastNotificationOutcome` con valores `None`/`Sent`/`Skipped`/`Failed`, `LastNotificationAt`, `LastNotificationError`); no existe contador de reintentos ni estado persistido "reintentando". El mapeo de estados de la vista es: `None` → **pendiente**, `Sent` → **enviado**, `Failed` → **fallido**, `Skipped` → **omitido**. Esta feature **extiende el modelo** (Q1) con un **contador de reintentos persistido**; el estado "reintentando" es **transitorio** (solo durante una acción manual en curso) y el worker **no** realiza reintentos automáticos.
- **"Cancelar envío" = marcar como omitido** (Q2): cancelar el envío de una factura **pendiente** la marca como **omitida** (`Skipped`) para que el worker no la procese; es una acción de dominio con confirmación explícita que conserva el registro para auditoría.
- **Relación con `/configuracion` (spec 017)**: las herramientas de `/configuracion` son **globales/masivas** y las acciones **por-factura** corresponden a esta vista, tal como anticipó la spec 017. "Reintentar fallidos" de esta vista es una acción **global** que reutiliza la capacidad masiva existente (`/api/settings/email/tools/resend-failed`).
- **Endpoint de reenvío por factura**: el roadmap nombra `POST /api/invoices/{id}/resend`, que **aún no existe** y se añade como parte de esta feature, confinando el proveedor de correo concreto a la capa Infrastructure detrás de la abstracción de envío existente.
- **Envío síncrono**: el envío de notificaciones es **síncrono en el worker** (no hay cola explícita); por ello "cancelar envío" no cancela un despacho encolado sino que marca la factura pendiente como omitida para que el worker no la procese (Q2).
- **Alcance del listado** (Q3, asunción razonable): el listado muestra las facturas en **estado notificable** (estados con plantilla aplicable) con su última notificación; las facturas en estados sin notificación aplicable no aparecen por defecto.
- **Resolución del correo de destino**: el correo mostrado por fila se obtiene mediante la resolución de correo de cliente ya existente en el sistema; las facturas sin correo resoluble se muestran como tales y deshabilitan las acciones de envío.
- **Mecanismo de toast y estado de servidor**: la vista reutiliza el sistema de *toasts* accesible del panel y la capa de estado de servidor (TanStack Query) para el refresco automático tras las acciones; la spec describe el comportamiento, no la librería.
- **Autenticación e idioma**: el acceso al panel está protegido por una capa previa (rol Admin) y toda la interfaz se presenta en español.
- **Stack mandatado**: por la Constitución, la interfaz usa React 19 + Vite + Tailwind con componentes shadcn/ui, animaciones con Motion, estado de servidor con TanStack Query y verificación de calidad con React Doctor; el backend usa ASP.NET Core 10 (Minimal APIs), MongoDB y FluentValidation (requisitos del proyecto, no decisiones abiertas de esta spec).
