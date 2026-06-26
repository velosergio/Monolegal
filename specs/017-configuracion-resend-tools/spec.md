# Feature Specification: Vista de Configuración — Proveedor de Email, Plantillas, Prueba de Envío y Herramientas de Administración

**Feature Branch**: `017-configuracion-resend-tools`

**Created**: 2026-06-26

**Status**: Draft

**Input**: User description:

> /speckit-specify @roadmap.md (306-318) — Spec 4.6: Vista Configuración + Resend API + Tools
>
> **GIVEN** usuario en `/configuracion` **WHEN** página carga **THEN**: sección de configuración de Resend API (API key, dominio remitente); gestión de plantillas de email (asunto, cuerpo, variables); botón de prueba para enviar email de test; tools/utilidades de administración (reenvío manual, limpieza de cola); validación de credenciales con feedback (toast éxito/error); persistencia de ajustes vía API.

## Resumen

Esta feature convierte la vista `/configuracion` (hoy limitada a la preferencia de tema) en el **centro de administración del envío de correos** del panel. Reúne en una sola pantalla cuatro capacidades que hoy no existen para el administrador:

1. **Configuración del proveedor de email**: el administrador puede revisar y ajustar los datos del emisor (dominio/dirección remitente, nombre visible) y la credencial del proveedor de correo (API key / clave de acceso), con **validación de credenciales** que confirma si el proveedor acepta la configuración, y retroalimentación tipo *toast* de éxito o error.
2. **Gestión de plantillas de email**: editar el **asunto** y el **cuerpo** de las plantillas de notificación (recordatorio, confirmación de pago, aviso de desactivación) usando **variables** sustituibles (p. ej. identificador de factura, monto, cliente), con vista previa del resultado.
3. **Prueba de envío**: un botón para enviar un **correo de prueba** a una dirección indicada por el administrador, que ejercita la configuración y las plantillas reales y reporta el resultado con *toast*.
4. **Herramientas de administración**: utilidades operativas sobre los envíos (reenvío manual y limpieza/saneamiento de los envíos pendientes o atascados) que permiten al administrador recuperar la operación sin intervención técnica.

Todos los ajustes se **persisten vía API** y la experiencia mantiene el estándar de calidad de la Fase 4: componentes consistentes (shadcn/ui), animaciones que respetan "reducir movimiento", accesibilidad por teclado y lector de pantalla, dark mode *built-in*, interfaz en español y verificación honesta de calidad de código.

## Clarifications

### Session 2026-06-26

- Q: El roadmap nombra "Resend API" pero el envío actual es SMTP (MailKit). ¿Esta feature migra, mantiene o soporta ambos? → A: **Soportar ambos proveedores (SMTP y Resend)**, **seleccionables desde `/configuracion`**; la vista permite elegir el proveedor activo y configurar los datos propios de ese proveedor.
- Q: ¿Dónde se persiste la credencial del proveedor (API key / contraseña SMTP), dado que el roadmap pide configurarla por la vista pero la Constitución prohíbe credenciales en BD? → A: **Híbrido**: el remitente (dirección/dominio, nombre), el proveedor activo y las plantillas se persisten en BD vía API; **las credenciales secretas NO se almacenan en BD** sino que se gestionan por variables de entorno / secrets. La vista muestra el **estado** de la credencial (configurada/validada) sin exponerla.
- Q: ¿Qué alcance tienen las herramientas de administración frente a la futura Vista de Envíos (4.10) y qué significa "limpieza de cola"? → A: En `/configuracion` las herramientas son **globales/masivas** (reenviar todos los fallidos/pendientes, sanear envíos atascados); el detalle/acciones por-factura corresponden a la Vista de Envíos (4.10). "Limpieza de cola" = **sanear los registros de envíos pendientes o atascados** (no existe una cola explícita; el envío es síncrono en el worker).
- Q: ¿Cuál es el conjunto canónico de variables admitidas en las plantillas? → A: Conjunto **extendido**: `factura.id`, `factura.monto`, `factura.vencimiento`, `factura.estado`, `factura.fechaEmision`, `cliente.nombre`, `cliente.email`, `cliente.empresa`, `enlacePago`. Solo estas variables se admiten; cualquier otra se rechaza en validación.
- Q: ¿Qué le ocurre a los envíos atascados al ejecutar "limpieza/saneamiento"? → A: Se **marcan como fallidos** (salen del estado pendiente/en curso atascado); los registros se **conservan** para auditoría y para que "reenvío manual" pueda recuperarlos después. No se borran ni se reintentan automáticamente.
- Q: ¿Sobre qué conjunto de envíos actúa el "reenvío manual" global? → A: Solo sobre envíos en estado **fallido**. Los pendientes los procesa el worker y los ya enviados no se reenvían; el saneamiento (que convierte atascados en fallidos) es el que habilita su reenvío.
- Q: ¿Cuándo toma efecto en los envíos reales el cambio de proveedor activo guardado? → A: **En runtime**: el worker/envío lee la configuración del sistema (proveedor activo y parámetros no secretos) en cada ciclo/envío y aplica el cambio sin reinicio; las credenciales se leen siempre del entorno.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Seleccionar y configurar el proveedor de email, validarlo y persistirlo (Priority: P1)

Como administrador, en `/configuracion` quiero **elegir el proveedor de correo activo** (SMTP o Resend), ajustar la **configuración propia de ese proveedor** y los datos del emisor (dominio/dirección remitente, nombre visible), **validar** que la configuración es aceptada por el proveedor y guardar los cambios, de modo que las notificaciones de facturas se envíen desde una identidad correcta y con un proveedor verificado, sabiendo en todo momento —mediante *toast*— si la validación y el guardado tuvieron éxito o fallaron.

**Why this priority**: Es el núcleo de la spec 4.6 y la precondición para que el envío de correos funcione de forma fiable. Sin un proveedor seleccionado, validado y persistido, las plantillas y la prueba de envío carecen de base operativa.

**Independent Test**: Abrir `/configuracion`, seleccionar el proveedor (SMTP o Resend), modificar la configuración propia del proveedor y el dominio/dirección remitente, pulsar "Validar", verificar que aparece un *toast* de éxito cuando el proveedor acepta la configuración y de error (con motivo) cuando la rechaza; guardar y recargar la página para comprobar que la selección y los ajustes persisten.

**Acceptance Scenarios**:

1. **Given** el administrador en `/configuracion`, **When** la página carga, **Then** se muestra la sección de email con el **proveedor activo seleccionable** (SMTP o Resend), los datos actuales del emisor (dominio/dirección remitente, nombre visible) y un indicador del **estado de la credencial**, sin exponer en claro ningún valor secreto.
2. **Given** el administrador cambia el proveedor activo, **When** lo selecciona, **Then** la vista muestra los **campos de configuración propios** de ese proveedor (los específicos de SMTP o los de Resend), sin perder el resto de ajustes ya introducidos.
3. **Given** el administrador edita la dirección/dominio remitente o un campo del proveedor con un valor inválido, **When** intenta guardar, **Then** el formulario impide el guardado y muestra una indicación de validación legible junto al campo, sin enviar la petición.
4. **Given** un proveedor seleccionado con su credencial disponible, **When** el administrador pulsa "Validar credenciales", **Then** el sistema comprueba la configuración contra ese proveedor y muestra un *toast* de **éxito** si es aceptada o de **error** con el motivo si es rechazada.
5. **Given** una configuración válida, **When** el administrador guarda, **Then** la selección de proveedor y los ajustes no secretos se **persisten vía API**, se muestra un *toast* de éxito y, al recargar, la página refleja los valores guardados.
6. **Given** un guardado que el backend rechaza (validación o error), **When** la operación falla, **Then** se muestra un *toast* de error con el motivo cuando esté disponible y los datos mostrados se refrescan para reflejar la realidad del backend.

---

### User Story 2 - Gestionar las plantillas de email (Priority: P2)

Como administrador, quiero editar el asunto y el cuerpo de las plantillas de notificación (recordatorio, confirmación de pago, aviso de desactivación) usando variables sustituibles y ver una vista previa, para personalizar el contenido de los correos sin depender del equipo técnico.

**Why this priority**: Aporta autonomía de contenido y es independiente de la credencial del proveedor (US1), aunque comparte la pantalla de configuración. Hoy las plantillas están fijas en el sistema.

**Independent Test**: En `/configuracion`, seleccionar una plantilla (p. ej. "Recordatorio"), editar su asunto y cuerpo incluyendo al menos una variable disponible, ver la vista previa con datos de ejemplo, guardar y recargar para comprobar la persistencia; verificar que una variable desconocida se señala como inválida antes de guardar.

**Acceptance Scenarios**:

1. **Given** el administrador en la sección de plantillas, **When** la página carga, **Then** se listan las plantillas disponibles (recordatorio, confirmación de pago, aviso de desactivación) con su asunto y cuerpo actuales y la lista de **variables admitidas**.
2. **Given** el administrador edita el asunto o el cuerpo de una plantilla, **When** inserta una variable admitida, **Then** la vista previa muestra el resultado renderizado con datos de ejemplo.
3. **Given** una plantilla con una variable **no admitida** o un campo obligatorio vacío, **When** el administrador intenta guardar, **Then** el sistema impide el guardado y muestra una indicación de validación legible.
4. **Given** una plantilla editada y válida, **When** el administrador guarda, **Then** los cambios se persisten vía API, se muestra un *toast* de éxito y, al recargar, se reflejan los valores guardados.
5. **Given** una plantilla modificada, **When** el administrador elige restablecer al contenido por defecto, **Then** la plantilla vuelve a su contenido base tras una confirmación.

---

### User Story 3 - Enviar un correo de prueba (Priority: P2)

Como administrador, quiero enviar un correo de prueba a una dirección que yo indique usando la configuración y las plantillas reales, para comprobar de extremo a extremo que el envío funciona antes de que afecte a clientes reales.

**Why this priority**: Da confianza operativa y cierra el ciclo configuración → plantilla → envío. Es independiente y aporta valor incluso sin tocar herramientas de administración.

**Independent Test**: En `/configuracion`, introducir una dirección de destino válida, elegir la plantilla a probar, pulsar "Enviar prueba" y verificar el *toast* de éxito (con confirmación del destino) o de error (con motivo); verificar que una dirección inválida se impide en el cliente.

**Acceptance Scenarios**:

1. **Given** una dirección de destino válida y una plantilla seleccionada, **When** el administrador pulsa "Enviar prueba", **Then** el sistema envía un correo de prueba usando la configuración y la plantilla reales y muestra un *toast* de éxito que confirma el destino.
2. **Given** una dirección de destino con formato inválido, **When** el administrador intenta enviar la prueba, **Then** el formulario impide el envío y muestra una indicación de validación, sin realizar la petición.
3. **Given** que el envío de prueba falla (credencial inválida, proveedor caído u otro error), **When** la operación termina, **Then** se muestra un *toast* de error con el motivo cuando esté disponible y un mensaje genérico claro cuando no lo esté.
4. **Given** un envío de prueba en curso, **When** el administrador espera la respuesta, **Then** el control refleja el estado de carga y se previene el doble envío.

---

### User Story 4 - Herramientas de administración de envíos (Priority: P3)

Como administrador, quiero utilidades para reenviar manualmente notificaciones y limpiar/sanear los envíos pendientes o atascados, para recuperar la operación de envío sin intervención técnica.

**Why this priority**: Es operativamente valiosa pero secundaria frente a configurar y validar el envío. Tiene relación con la futura Vista de Envíos (roadmap 4.10); aquí se ofrecen utilidades de administración de alcance acotado.

**Independent Test**: En `/configuracion`, ejecutar "reenvío manual" sobre una notificación fallida/pendiente y verificar el *toast* de resultado; ejecutar la "limpieza" de envíos y verificar la confirmación previa y el *toast* de resultado con el conteo afectado.

**Acceptance Scenarios**:

1. **Given** la sección de herramientas, **When** la página carga, **Then** se muestran las utilidades disponibles (reenvío manual y limpieza de envíos pendientes/atascados) con una descripción clara de su efecto.
2. **Given** una acción potencialmente destructiva (limpieza de envíos), **When** el administrador la inicia, **Then** el sistema solicita confirmación explícita antes de ejecutarla.
3. **Given** una utilidad ejecutada, **When** la operación termina, **Then** se muestra un *toast* con el resultado (éxito/error) y, cuando aplique, el número de elementos afectados.
4. **Given** una utilidad sin elementos sobre los que actuar, **When** se ejecuta, **Then** se informa con un mensaje claro de que no había nada que procesar, sin tratarlo como error.

---

### Edge Cases

- **Credencial gestionada por entorno**: la pantalla nunca muestra la credencial; indica su estado (configurada/no configurada/validada) según lo provisto por variables de entorno/secrets y NO permite editar el valor secreto desde la UI.
- **Cambio de proveedor activo**: al alternar entre SMTP y Resend se muestran los campos propios del proveedor sin perder los ajustes ya introducidos; el estado de credencial mostrado corresponde al proveedor activo.
- **Guardar sin validar**: si el administrador guarda sin haber validado el proveedor activo, el sistema persiste los ajustes no secretos pero indica claramente que la credencial no ha sido verificada.
- **Validación sin credencial**: pulsar "Validar" cuando el proveedor activo no tiene credencial configurada (no presente en el entorno) produce una indicación clara, no una petición sin sentido.
- **Variable no admitida en plantilla**: una variable desconocida se señala en validación antes de guardar; nunca se persiste una plantilla con variables inválidas.
- **Cuerpo de plantilla vacío**: un asunto o cuerpo vacío se impide en validación.
- **Prueba a dirección inválida**: se impide en el cliente con indicación de validación.
- **Doble envío / doble guardado**: los controles se deshabilitan mientras la operación está en curso para evitar duplicados.
- **Toast con/sin motivo**: si el backend no aporta un motivo legible, el *toast* de error muestra un mensaje genérico claro.
- **Limpieza sin elementos**: la herramienta informa "nada que procesar" en lugar de error.
- **Movimiento reducido**: con "reducir movimiento" activa, las animaciones de toasts y transiciones se atenúan sin ocultar información.
- **Acceso concurrente**: si otro administrador cambió los ajustes, al guardar se refleja la realidad del backend tras la operación (último guardado coherente con lo persistido).

## Requirements *(mandatory)*

### Functional Requirements

#### Configuración del proveedor de email — roadmap 4.6

- **FR-001**: La vista `/configuracion` MUST incluir una sección de **configuración del proveedor de email** que muestre los datos actuales del emisor: **dirección/dominio remitente** y **nombre visible del remitente**.
- **FR-002**: La sección MUST permitir **seleccionar el proveedor de correo activo** entre **SMTP** y **Resend**, y MUST mostrar los **campos de configuración propios** del proveedor seleccionado (los específicos de SMTP — p. ej. host, puerto, usuario, TLS — o los de Resend — p. ej. dominio remitente verificado), sin perder el resto de ajustes ya introducidos al cambiar de proveedor.
- **FR-002a**: Tanto SMTP como Resend MUST poder configurarse y validarse desde la vista; el envío real de notificaciones MUST usar el proveedor marcado como **activo**.
- **FR-002b**: El cambio de proveedor activo y de los parámetros no secretos persistidos MUST aplicarse **en runtime**: el proceso de envío (worker) MUST leer la configuración del sistema en cada ciclo/envío y honrar el cambio **sin requerir reinicio**; las credenciales se leen siempre del entorno.
- **FR-003**: El formulario MUST realizar **validación en el cliente** de los campos del emisor (formato de dirección/dominio remitente, nombre no vacío) y de los campos requeridos del proveedor seleccionado, antes de permitir el guardado.
- **FR-004**: La sección MUST ofrecer una acción de **"Validar credenciales"** que comprueba la configuración contra el **proveedor activo** y reporta el resultado mediante *toast* de **éxito** o de **error** con el motivo cuando esté disponible.
- **FR-005**: La **selección de proveedor activo** y los **ajustes no secretos** del emisor/proveedor MUST **persistirse vía API** y MUST reflejarse al recargar la página.
- **FR-006**: Tras un guardado exitoso, el sistema MUST mostrar un *toast* de éxito; ante un rechazo/error, MUST mostrar un *toast* de error con el motivo cuando esté disponible y refrescar los datos para reflejar la realidad del backend.
- **FR-007**: La feature MUST soportar **ambos proveedores (SMTP y Resend)** de forma abstracta y conmutable, de modo que el proveedor concreto quede confinado a la capa de infraestructura y se pueda alternar mediante la selección de la vista sin cambios en los consumidores del envío.
- **FR-008**: Las **credenciales secretas** del proveedor (API key de Resend, contraseña SMTP) MUST gestionarse **únicamente por variables de entorno / secrets** y MUST NOT almacenarse en la base de datos. La vista MUST mostrar el **estado** de la credencial del proveedor activo (configurada / no configurada / validada) **sin exponer su valor**, y NO MUST persistir el valor secreto vía API. Los datos no secretos (proveedor activo, remitente, nombre, parámetros no sensibles) sí se persisten vía API.

#### Gestión de plantillas de email

- **FR-009**: La vista MUST permitir **listar y editar** las plantillas de notificación existentes (al menos: recordatorio, confirmación de pago, aviso de desactivación), incluyendo su **asunto** y su **cuerpo**.
- **FR-010**: Cada plantilla MUST exponer la lista de **variables admitidas** que pueden insertarse en el asunto y el cuerpo. El conjunto canónico admitido es exactamente: `factura.id`, `factura.monto`, `factura.vencimiento`, `factura.estado`, `factura.fechaEmision`, `cliente.nombre`, `cliente.email`, `cliente.empresa`, `enlacePago`. Cualquier variable fuera de este conjunto MUST tratarse como no admitida.
- **FR-011**: El editor de plantillas MUST validar que solo se usan **variables admitidas** y que el asunto y el cuerpo no quedan vacíos, impidiendo el guardado en caso contrario con una indicación legible.
- **FR-012**: La vista MUST ofrecer una **vista previa** del asunto y el cuerpo renderizados con **datos de ejemplo** para las variables.
- **FR-013**: Los cambios de plantilla MUST **persistirse vía API**, reflejarse al recargar y confirmar el resultado con *toast* de éxito/error.
- **FR-014**: La vista MUST permitir **restablecer una plantilla a su contenido por defecto**, previa confirmación.
- **FR-015**: Las plantillas guardadas MUST ser las que utiliza el sistema al enviar las notificaciones reales de facturas (el contenido editable sustituye al contenido fijo actual).

#### Prueba de envío

- **FR-016**: La vista MUST ofrecer una acción de **"Enviar correo de prueba"** que envía un correo a una **dirección indicada por el administrador**, usando la configuración y la plantilla seleccionada reales.
- **FR-017**: El formulario de prueba MUST validar en el cliente el **formato de la dirección de destino** antes de enviar.
- **FR-018**: El resultado del envío de prueba MUST reportarse con *toast* de **éxito** (confirmando el destino) o de **error** (con motivo cuando esté disponible).
- **FR-019**: Durante el envío de prueba, el control MUST reflejar el estado de carga y MUST prevenir el doble envío.

#### Herramientas de administración

- **FR-020**: La vista MUST ofrecer una utilidad de **reenvío manual** que actúa **únicamente sobre los envíos en estado fallido** (no reenvía los ya enviados ni interfiere con los pendientes que procesa el worker), con confirmación del resultado mediante *toast*.
- **FR-021**: La vista MUST ofrecer una utilidad de **limpieza/saneamiento** de envíos pendientes o atascados que MUST **marcarlos como fallidos** (sacándolos del estado atascado) **conservando los registros** para auditoría y posterior reenvío; MUST NOT borrar registros ni reintentarlos automáticamente, y MUST solicitar **confirmación explícita** antes de ejecutarse.
- **FR-022**: Las utilidades de administración MUST informar el **resultado** (éxito/error y, cuando aplique, número de elementos afectados) y MUST manejar el caso "sin elementos que procesar" como un mensaje informativo, no un error.
- **FR-023**: Las herramientas de administración de `/configuracion` MUST operar de forma **global/masiva** (p. ej. reenviar todas las notificaciones fallidas/pendientes, sanear todos los envíos atascados); las acciones **por-factura** quedan fuera de alcance y corresponden a la futura Vista de Envíos (roadmap 4.10). La "limpieza de cola" se entiende como el **saneamiento de los registros de envíos pendientes o atascados** (no existe una cola explícita: el envío es síncrono en el worker).

#### Experiencia, accesibilidad y calidad — estándar de la Fase 4

- **FR-024**: Todos los elementos interactivos (formularios, botones de validar/guardar/probar, herramientas y *toasts*) MUST ser operables por teclado, con foco visible, en español y consistentes con el resto del panel (tipografía, espaciado, colores, dark mode).
- **FR-025**: El mecanismo de notificación tipo *toast* MUST ser accesible (anunciado a tecnologías de asistencia) y MUST respetar la preferencia de "reducir movimiento".
- **FR-026**: La vista MUST presentar **estados de carga** (mientras se obtienen o guardan ajustes) y **estados de error** legibles, sin pantallas en blanco ni controles ambiguos.
- **FR-027**: El código de la feature MUST alcanzar **100/100 en la herramienta de inspección de React del proyecto de forma honesta**, sin suprimir ni silenciar avisos, y MUST mantener los presupuestos de rendimiento del panel.

### Key Entities *(include if feature involves data)*

- **Configuración de email (ajustes del emisor)**: **proveedor activo** (SMTP o Resend), datos del emisor — dirección/dominio remitente, nombre visible — y los parámetros **no secretos** propios del proveedor seleccionado. Es parte de la configuración del sistema persistida (sin valores secretos).
- **Credencial del proveedor**: secreto de acceso al proveedor de correo (API key de Resend / contraseña SMTP); se gestiona **solo por variables de entorno / secrets**, nunca se persiste en BD ni se expone en la interfaz. La vista solo refleja su **estado** (configurada/no configurada/validada).
- **Plantilla de email**: para cada tipo de notificación (recordatorio, confirmación de pago, aviso de desactivación), un **asunto** y un **cuerpo** con **variables** admitidas sustituibles por datos de la factura/cliente.
- **Variable de plantilla**: marcador admitido del conjunto canónico (`factura.id`, `factura.monto`, `factura.vencimiento`, `factura.estado`, `factura.fechaEmision`, `cliente.nombre`, `cliente.email`, `cliente.empresa`, `enlacePago`) que se sustituye por el dato real al renderizar; `cliente.empresa` y `enlacePago` pueden estar vacíos si el dato no existe.
- **Envío/notificación**: registro del intento de envío de un correo (resultado: pendiente/enviado/fallido) sobre el que operan las herramientas de reenvío y limpieza.
- **Configuración del sistema (SystemSettings)**: agregado de ajustes persistidos del panel que ya contiene la configuración de transiciones y se extiende con la configuración de email y plantillas.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un administrador puede configurar el emisor, validar la credencial y guardar en menos de 2 minutos, recibiendo siempre una confirmación visible (toast) del resultado de validar y de guardar.
- **SC-002**: En el 100% de los intentos de validar credenciales, el administrador recibe un resultado explícito (éxito o error con motivo) sin ambigüedad.
- **SC-003**: Tras guardar cualquier ajuste (proveedor o plantilla), los valores persisten y se reflejan al recargar la página en el 100% de los casos.
- **SC-004**: Un administrador puede editar una plantilla con variables y ver su vista previa renderizada en menos de 1 minuto, y el sistema impide el 100% de los guardados con variables no admitidas o campos obligatorios vacíos.
- **SC-005**: Un administrador puede enviar un correo de prueba y conocer el resultado (éxito/error con motivo) en una sola acción, sin pasos técnicos adicionales.
- **SC-006**: Las herramientas de administración informan el resultado y el número de elementos afectados en el 100% de las ejecuciones, y solicitan confirmación antes de cualquier acción destructiva.
- **SC-007**: La credencial almacenada nunca se muestra en claro en la interfaz en ningún flujo.
- **SC-008**: Toda la vista de configuración es completamente operable solo con teclado, con foco siempre visible, y la información transmitida por color cuenta también con etiqueta textual.
- **SC-009**: La vista se muestra correctamente, sin desbordamiento horizontal ni solapamientos, en anchos representativos de móvil y de escritorio.
- **SC-010**: La herramienta de inspección de React del proyecto reporta 100/100 para el código de la feature sin avisos suprimidos artificialmente, y se mantienen los presupuestos de rendimiento del panel (TTI < 2s, rendimiento > 90 en auditoría estándar).

## Assumptions

- **Punto de partida**: hoy `/configuracion` solo gestiona la preferencia de tema; esta feature añade las secciones de proveedor de email, plantillas, prueba de envío y herramientas, manteniendo la sección de apariencia existente.
- **Configuración del sistema existente**: ya existe un agregado de configuración del sistema persistido (con la configuración de transiciones de facturas) y un patrón de endpoints para leerlo/actualizarlo; la configuración de email y plantillas se modela como extensión de ese agregado, reutilizando el patrón existente.
- **Plantillas actualmente fijas**: el contenido de las notificaciones está hoy fijo en el sistema; esta feature lo convierte en **editable y persistido**, manteniendo las mismas variables disponibles (identificador de factura, monto, cliente) como punto de partida.
- **Mecanismo de toast**: el panel incorpora (o reutiliza, si ya existe tras la feature 016) un sistema de notificaciones tipo *toast* accesible y consistente con shadcn/ui; la spec describe el comportamiento esperado, no la librería concreta.
- **Proveedores soportados** (clarificación 2026-06-26): la feature soporta **SMTP y Resend**, seleccionables desde `/configuracion`. El proveedor concreto se mantiene confinado a la capa Infrastructure detrás de la abstracción de envío existente; el frontend solo selecciona el proveedor activo y configura sus parámetros no secretos. La implementación SMTP ya existe; la de Resend se añade como segundo proveedor conmutable.
- **Relación con la Vista de Envíos (roadmap 4.10)** (clarificación 2026-06-26): las herramientas de administración de esta vista son **globales/masivas** (reenviar todas las fallidas/pendientes, sanear envíos atascados); el detalle y las acciones por-factura corresponden a la futura vista 4.10.
- **Autenticación e idioma**: el acceso al panel está protegido por una capa previa (rol Admin); toda la interfaz se presenta en español.
- **Stack mandatado**: por la Constitución, la interfaz usa React 19 + Vite + Tailwind con componentes shadcn/ui, animaciones con Motion, estado de servidor con TanStack Query y verificación de calidad con React Doctor; el backend usa ASP.NET Core 10 (Minimal APIs), MongoDB y FluentValidation, con el proveedor de email confinado a la capa Infrastructure (requisitos del proyecto, no decisiones abiertas de esta spec).
- **Política de secretos** (clarificación 2026-06-26): los secretos (API key de Resend, contraseña SMTP) se gestionan **solo por variables de entorno / Docker secrets** y **no se persisten en BD** ni se exponen en la UI. Lo que se persiste vía API es el proveedor activo, el remitente, el nombre y los parámetros no sensibles; la vista refleja el estado de la credencial sin su valor.
