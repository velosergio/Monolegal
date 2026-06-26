# Feature Specification: Formulario de Transición Manual de Estado, Dashboard como Inicio y Gráfico Donut por Estado

**Feature Branch**: `016-transition-form-donut-dashboard`

**Created**: 2026-06-26

**Status**: Draft

**Input**: User description:

> /speckit-specify @roadmap.md (295-305) — Spec 4.5: Form - Manual State Transition
>
> **GIVEN** modal de detalle abierto **WHEN** usuario selecciona nuevo estado y confirma **THEN**: form validación frontend; POST a `/api/invoices/transition/{id}`; toast de éxito/error; tabla y modal se actualizan.
>
> Además: cambiar la ruta `"/"` para que sea el dashboard, y que "facturas por estado" cambie a gráfico **donut** con colores.

## Resumen

Esta feature cierra y formaliza el **cambio manual de estado de una factura** como un **formulario** dentro del modal de detalle (roadmap 4.5): validación en el cliente antes de enviar, envío de la transición, **retroalimentación tipo *toast*** de éxito o error, y actualización coherente del listado (tabla) y del modal sin recargar la página. La spec 015 ya ejecuta el cambio de estado dentro del modal; esta feature lo eleva al estándar 4.5 introduciendo **validación de formulario explícita** y un **mecanismo de notificación tipo toast** del que el panel hoy carece.

Adicionalmente, esta feature hace del **dashboard la pantalla de inicio** del panel (la ruta raíz `"/"` muestra el dashboard en lugar de redirigir al listado de facturas) y reemplaza el gráfico de **distribución de facturas por estado** por un **gráfico de dona (donut)** con un **color por estado** coherente con las etiquetas de estado del resto del panel.

La experiencia mantiene el estándar de calidad de la Fase 4: componentes de interfaz consistentes, animaciones suaves que respetan "reducir movimiento", accesibilidad por teclado y lector de pantalla, dark mode *built-in* y verificación honesta de calidad de código.

## Clarifications

### Session 2026-06-26

- Q: La ruta raíz `"/"` debe mostrar el dashboard; hoy también existe `/dashboard`. ¿Cómo conviven? → A: `"/"` muestra el dashboard y se **elimina por completo** la ruta `/dashboard` (URL única; no se mantiene la ruta antigua ni redirección).
- Q: ¿Qué se muestra en el centro (hueco) del gráfico de dona por estado? → A: El **total de facturas** (número destacado) con una etiqueta "Total".
- Q: ¿Cómo se combinan el toast y el mensaje de error inline existente en el formulario? → A: **Toast para éxito y error**; además, ante error se **conserva un mensaje inline persistente** en el formulario (el toast no sustituye al inline en errores).
- Q: ¿A dónde redirige una ruta desconocida (incluida la antigua `/dashboard`) ahora que el inicio es el dashboard? → A: A la **raíz `"/"`** (dashboard), la nueva pantalla de inicio.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cambiar el estado de una factura mediante un formulario con confirmación y toast (Priority: P1)

Como administrador, dentro del modal de detalle quiero cambiar el estado de una factura usando un formulario que valide mi selección antes de enviar, confirme el envío y me muestre una notificación tipo *toast* de éxito o de error, de modo que sepa con certeza si la operación se completó, viendo el listado y el modal actualizados al instante.

**Why this priority**: Es el núcleo de la spec 4.5 del roadmap y completa el flujo de gestión de la cartera (selección → validación → envío → confirmación). Sin la retroalimentación clara (toast) y la validación, el cambio de estado queda incompleto frente a lo pedido.

**Independent Test**: Abrir el modal de una factura con al menos una transición válida, intentar confirmar sin seleccionar destino (la acción se impide con una indicación de validación), seleccionar un destino permitido y confirmar; verificar que aparece un *toast* de éxito, que el estado mostrado, el historial del modal y la fila correspondiente del listado reflejan el nuevo estado sin recargar la página; luego forzar un rechazo del backend y verificar que aparece un *toast* de error y el estado mostrado no cambia.

**Acceptance Scenarios**:

1. **Given** el modal de detalle abierto en una factura con transiciones válidas, **When** el usuario intenta confirmar el cambio sin haber seleccionado un estado destino, **Then** el formulario impide el envío y muestra una indicación de validación legible, sin realizar ninguna petición.
2. **Given** el usuario ha seleccionado un estado destino permitido, **When** confirma el cambio, **Then** se envía la transición, los controles reflejan el estado "en curso" (deshabilitados/ocupados) y se evita el doble envío.
3. **Given** una transición que el backend acepta, **When** la operación tiene éxito, **Then** se muestra un *toast* de éxito legible, y el estado actual mostrado, el historial del modal y la fila del listado quedan coherentes con el nuevo estado, sin recargar la página.
4. **Given** una transición que el backend rechaza (no permitida o error), **When** la operación falla, **Then** se muestra un *toast* de error legible con el motivo cuando esté disponible y, además, un mensaje de error *inline* persistente en el formulario; el estado mostrado de la factura no cambia y los datos se refrescan para reflejar la realidad del backend.
5. **Given** una factura sin transiciones válidas (estado terminal o sin destinos), **When** se abre su modal, **Then** el formulario de cambio de estado se oculta o se deshabilita con una indicación clara del motivo, y no es posible iniciar un cambio.

---

### User Story 2 - El dashboard es la pantalla de inicio (Priority: P2)

Como administrador, al abrir la aplicación en la dirección raíz quiero aterrizar directamente en el dashboard de estadísticas, para tener de inmediato la visión general de la cartera como primera pantalla del panel.

**Why this priority**: Cambia la primera impresión y el punto de entrada del panel hacia la vista analítica. Aporta valor, pero es independiente del flujo de cambio de estado (P1) y reutiliza el dashboard ya existente.

**Independent Test**: Navegar a la ruta raíz del panel y verificar que se muestra el dashboard (no el listado de facturas); verificar que la entrada de navegación del dashboard queda resaltada como activa; y que el resto de la navegación (Facturas, Configuración) sigue funcionando con coherencia en escritorio y móvil.

**Acceptance Scenarios**:

1. **Given** un usuario que abre la aplicación en la dirección raíz, **When** la aplicación carga, **Then** se muestra el dashboard de estadísticas como pantalla inicial, no el listado de facturas.
2. **Given** el usuario en la pantalla de inicio (dashboard), **When** observa la navegación lateral, **Then** la entrada correspondiente al dashboard aparece resaltada como sección activa.
3. **Given** el usuario en el dashboard de inicio, **When** navega a "Facturas" y vuelve, **Then** la navegación es coherente y la dirección/ruta reflejada permanece consistente con la sección mostrada, en escritorio y móvil.

---

### User Story 3 - Distribución por estado como gráfico de dona con colores (Priority: P2)

Como administrador, en el dashboard quiero que la distribución de facturas por estado se muestre como un gráfico de dona (donut) con un color distinto por estado, para captar de un vistazo la proporción de cada estado en la cartera.

**Why this priority**: Mejora la legibilidad de la métrica clave del dashboard ("por estado"). Es independiente del cambio de estado (P1) y de la ruta de inicio (US2), aunque comparte la pantalla del dashboard.

**Independent Test**: Abrir el dashboard con datos y verificar que la distribución por estado se representa como un gráfico de dona con un segmento por estado, cada uno con un color coherente con la etiqueta de ese estado en el resto del panel; verificar que existe una leyenda o etiquetado que asocia color ↔ estado ↔ cantidad/proporción; verificar la animación de entrada suave (respetando "reducir movimiento"); y verificar el estado vacío (sin facturas) con un mensaje claro en lugar de un gráfico roto.

**Acceptance Scenarios**:

1. **Given** estadísticas con facturas en varios estados, **When** se renderiza el dashboard, **Then** la distribución por estado se muestra como un gráfico de dona con un segmento por estado, cada segmento con un color distinto y coherente con la etiqueta de color de ese estado en el resto del panel.
2. **Given** el gráfico de dona, **When** se renderiza, **Then** se acompaña de una leyenda/etiquetado accesible que asocia cada color con su estado y su valor (cantidad y/o proporción), legible en español, y muestra en su **centro** el total de facturas con la etiqueta "Total".
3. **Given** las estadísticas disponibles, **When** el gráfico aparece, **Then** lo hace con una animación de entrada suave que respeta la preferencia de "reducir movimiento".
4. **Given** que no existen facturas, **When** se renderiza la distribución por estado, **Then** se muestra un estado vacío claro (sin segmentos o con mensaje), nunca un gráfico roto.
5. **Given** un único estado presente en la cartera, **When** se renderiza la dona, **Then** se muestra correctamente como un único segmento (anillo completo) sin romper el layout ni el etiquetado.

---

### Edge Cases

- **Confirmar sin selección**: el usuario pulsa confirmar sin elegir un destino → el formulario lo impide con validación visible y no envía ninguna petición.
- **Selección que pierde validez**: el destino elegido deja de ser válido (datos desactualizados) y el backend rechaza → *toast* de error, el estado mostrado no cambia y se refrescan los datos.
- **Doble envío**: el usuario confirma rápidamente dos veces → solo se procesa una operación; mientras está en curso, los controles están deshabilitados.
- **Toast de error con/sin motivo**: si el backend no proporciona un motivo legible, el *toast* de error muestra un mensaje genérico claro.
- **Cierre del modal con cambio en curso**: si el modal se cierra mientras una operación está en curso, la notificación de resultado (toast) sigue siendo visible y el listado queda coherente al resolver.
- **Inicio en dashboard sin datos**: la ruta raíz muestra el dashboard aunque no haya facturas → estados vacíos legibles, no pantalla en blanco ni gráfico roto.
- **Acceso directo a rutas existentes**: abrir directamente la ruta del listado o de configuración sigue funcionando; solo cambia el destino de la raíz.
- **Acceso a la ruta `/dashboard` eliminada**: al abrir directamente `/dashboard` (eliminada) o cualquier otra ruta desconocida, el panel MUST redirigir a la raíz `"/"` (dashboard), sin pantalla en blanco ni error, sin tratar `/dashboard` como ruta válida.
- **Estado desconocido/legacy en la dona**: un estado no contemplado en el conjunto activo se representa con un color neutro y etiqueta legible, sin romper la dona ni su leyenda.
- **Movimiento reducido**: con "reducir movimiento" activa, la animación de la dona y de los toasts se atenúa o desactiva sin impedir el uso ni ocultar información.

## Requirements *(mandatory)*

### Functional Requirements

#### Formulario de transición manual de estado — roadmap 4.5

- **FR-001**: El cambio de estado dentro del modal MUST presentarse como un **formulario** con un control de selección del estado destino y una acción explícita de confirmación.
- **FR-002**: El formulario MUST ofrecer únicamente los **estados destino permitidos** para esa factura (provistos por el backend como única fuente de verdad), sin replicar la matriz de transiciones en el frontend.
- **FR-003**: El formulario MUST realizar **validación en el cliente** antes de enviar: si no hay un estado destino seleccionado, MUST impedir el envío y mostrar una indicación de validación legible, sin realizar ninguna petición.
- **FR-004**: Al confirmar una selección válida, el sistema MUST aplicar la transición mediante el flujo de transición existente (envío al endpoint de transición de la factura).
- **FR-005**: Tras un cambio aplicado con éxito, el sistema MUST mostrar una **notificación tipo *toast* de éxito** legible en español.
- **FR-006**: Ante un rechazo o error del backend, el sistema MUST mostrar una **notificación tipo *toast* de error** legible, incluyendo el motivo cuando el backend lo proporcione y un mensaje genérico claro cuando no lo proporcione.
- **FR-006a**: Ante un error, además del *toast*, el formulario MUST conservar un **mensaje de error *inline* persistente** (que no desaparece automáticamente como el toast) con el mismo motivo, para no depender de una notificación transitoria. En caso de éxito, el *toast* es la confirmación (no se requiere mensaje inline de éxito).
- **FR-007**: Tras un cambio exitoso, el sistema MUST actualizar de forma coherente y sin recargar la página: el **estado actual mostrado** en el modal, el **historial** del modal y la **fila correspondiente del listado** (tabla).
- **FR-008**: Ante un cambio fallido, el estado mostrado de la factura MUST NOT cambiar, y el sistema MUST refrescar los datos para reflejar la realidad del backend.
- **FR-009**: Durante una operación de cambio en curso, los controles del formulario MUST reflejar el estado de carga (deshabilitados/ocupados) y MUST prevenir el doble envío.
- **FR-010**: Cuando una factura no tenga transiciones válidas (estado terminal o sin destinos), el formulario de cambio de estado MUST ocultarse o deshabilitarse con una indicación clara del motivo.
- **FR-011**: El mecanismo de notificación tipo *toast* MUST ser accesible (anunciado a tecnologías de asistencia) y MUST respetar la preferencia de "reducir movimiento" en sus animaciones.

#### Dashboard como pantalla de inicio

- **FR-012**: La **ruta raíz** del panel (`"/"`) MUST mostrar el **dashboard de estadísticas** como pantalla de inicio, en lugar de redirigir al listado de facturas.
- **FR-012a**: La ruta `/dashboard` MUST eliminarse por completo (no se mantiene como ruta válida ni como redirección); el dashboard MUST ser accesible **únicamente** desde la ruta raíz `"/"`. La entrada de navegación del dashboard MUST apuntar a `"/"`.
- **FR-013**: Cuando el dashboard se muestra como pantalla de inicio, la entrada de navegación correspondiente MUST quedar **resaltada como sección activa** al estar en la ruta raíz.
- **FR-014**: El cambio del destino de la ruta raíz MUST NOT romper el acceso al resto de secciones existentes (listado de facturas, configuración) ni la navegación entre ellas en escritorio y móvil.
- **FR-014a**: Cualquier ruta desconocida (incluida la antigua `/dashboard`) MUST redirigir a la raíz `"/"` (dashboard), la nueva pantalla de inicio.

#### Gráfico de dona (donut) por estado

- **FR-015**: La **distribución de facturas por estado** en el dashboard MUST representarse como un **gráfico de dona (donut)** con un **segmento por estado**.
- **FR-016**: Cada segmento del gráfico de dona MUST tener un **color distinto y coherente** con la etiqueta de color de ese estado usada en el resto del panel (listado y modal).
- **FR-016a**: El **centro (hueco)** del gráfico de dona MUST mostrar el **total de facturas** como número destacado, acompañado de una etiqueta "Total" legible en español. En el estado vacío (sin facturas) el centro MUST mostrar `0`.
- **FR-017**: El gráfico de dona MUST acompañarse de una **leyenda o etiquetado accesible** que asocie cada color con su estado y su valor (cantidad y/o proporción), en español.
- **FR-018**: El gráfico de dona MUST presentar una **animación de entrada suave** que respete la preferencia de "reducir movimiento".
- **FR-019**: Cuando no existan facturas, la distribución por estado MUST mostrar un **estado vacío claro** (sin segmentos o con mensaje), nunca un gráfico roto; y con un único estado presente MUST mostrarse como un anillo completo sin romper el layout.

#### Experiencia, animación y calidad — estándar de la Fase 4

- **FR-020**: Todos los elementos interactivos nuevos (formulario de cambio de estado, toasts, navegación al inicio) MUST ser operables por teclado, con foco visible, y consistentes visualmente con el resto del panel (tipografía, espaciado, colores y estados de interacción).
- **FR-021**: El código de la feature MUST alcanzar **100/100 en la herramienta de inspección de React del proyecto de forma honesta**, sin suprimir, ignorar ni silenciar avisos para inflar la puntuación, y MUST mantener los presupuestos de rendimiento del panel.

### Key Entities *(include if feature involves data)*

- **Transición de estado (cambio manual)**: acción de pasar una factura de su estado actual a un estado destino permitido; tras aplicarse, queda registrada en el historial de la factura (mecanismo existente). Atributos relevantes para esta feature: factura objetivo, estado destino seleccionado, resultado (éxito/error) y motivo de error cuando aplique.
- **Estados destino permitidos**: conjunto de estados a los que una factura puede transicionar según la matriz de transiciones del dominio; alimenta las opciones del formulario.
- **Distribución por estado (Stats)**: agregado de conteo de facturas por estado; base del gráfico de dona del dashboard.
- **Estado de factura**: clasificación dentro del conjunto activo (pendiente, primer recordatorio, segundo recordatorio, desactivado, pagado) que determina la etiqueta y el **color** asociado, reutilizado por el gráfico de dona.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Un administrador puede completar un cambio de estado de factura (seleccionar destino → confirmar → ver resultado) en menos de 15 segundos, recibiendo en todos los casos una confirmación visible (toast) de éxito o de error.
- **SC-002**: En el 100% de los intentos de confirmar sin haber seleccionado un destino, el formulario impide el envío y muestra una indicación de validación, sin realizar peticiones.
- **SC-003**: Tras un cambio exitoso, el estado actual, el historial y la fila del listado reflejan la nueva información sin que el usuario recargue la página.
- **SC-004**: Tras un cambio fallido, el estado mostrado de la factura no cambia en ningún caso, y el usuario siempre recibe un toast de error legible.
- **SC-005**: Al abrir la aplicación en la dirección raíz, el usuario aterriza en el dashboard en el 100% de los accesos, con la sección de navegación correcta resaltada.
- **SC-006**: En el dashboard, un administrador identifica el estado con mayor proporción de facturas a partir del gráfico de dona en menos de 5 segundos, gracias al color y la leyenda.
- **SC-007**: El gráfico de dona, el formulario y los toasts son completamente operables solo con teclado, con foco siempre visible, y la información transmitida por color cuenta también con etiqueta textual (no se depende solo del color).
- **SC-008**: Con la preferencia de "reducir movimiento" activa, ninguna animación esencial (toast o dona) impide completar las tareas ni oculta información.
- **SC-009**: El dashboard, el gráfico de dona y el formulario se muestran correctamente, sin desbordamiento horizontal ni solapamientos, en anchos representativos de móvil y de escritorio.
- **SC-010**: La herramienta de inspección de React del proyecto reporta 100/100 para el código de la feature sin avisos suprimidos artificialmente, y se mantienen los presupuestos de rendimiento del panel (TTI < 2s, rendimiento > 90 en auditoría estándar).

## Assumptions

- **Alcance respecto a la spec 015**: la spec 015 ya ejecuta el cambio de estado dentro del modal y mantiene la coherencia con el listado vía invalidación de datos. Esta feature **formaliza** ese flujo como el de la spec 4.5 del roadmap: añade **validación de formulario explícita** y un **mecanismo de notificación tipo *toast*** (éxito/error) del que el panel no dispone hoy. La lógica de transición, el historial y la exposición de destinos válidos por el backend se reutilizan sin cambios de contrato.
- **Mecanismo de toast**: el panel no cuenta hoy con un sistema de notificaciones tipo *toast*; esta feature introduce uno, consistente con el sistema de componentes del proyecto (shadcn/ui) y accesible. La spec describe el comportamiento esperado (qué y cuándo se notifica), no la librería concreta.
- **Ruta de inicio** (clarificación 2026-06-26): cambiar `"/"` para que muestre el dashboard implica que el dashboard pasa a ser la pantalla de inicio del panel. La ruta dedicada `/dashboard` se **elimina por completo** (sin redirección): el dashboard es accesible únicamente desde la raíz `"/"` y la entrada de navegación apunta a `"/"` (FR-012a). El listado de facturas y la configuración conservan sus rutas.
- **Colores por estado**: los colores de los segmentos de la dona reutilizan la paleta de etiquetas de estado ya existente en el listado y el modal, garantizando coherencia visual y dark mode. Los estados legacy retirados (ver spec 015) no se consideran; un estado no contemplado se representa con un color neutro.
- **Gráficos in-house**: coherente con la spec 015, los gráficos se construyen con SVG + Motion (sin librería de charting nueva); el gráfico de dona sustituye/extiende la representación por estado existente sin añadir dependencias de runtime de charting.
- **Endpoints reutilizados**: se asume disponible el endpoint de transición (`POST /api/invoices/transition/{id}`), el de detalle (`GET /api/invoices/{id}`, con historial y destinos válidos) y el de estadísticas (`GET /api/invoices/stats`, con conteo por estado), sin cambios de contrato.
- **Autenticación e idioma**: el acceso al panel está protegido por una capa previa (rol Admin); toda la interfaz se presenta en español.
- **Stack mandatado**: por la Constitución, la interfaz usa React 19 + Vite + Tailwind con componentes shadcn/ui, animaciones con Motion, estado de servidor con TanStack Query y verificación de calidad con React Doctor (requisitos del proyecto, no decisiones abiertas de esta spec).
