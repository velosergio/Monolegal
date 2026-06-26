# Especificación de Funcionalidad: CRUD de Facturas y Clientes

**Rama de Funcionalidad**: `018-crud-facturas-clientes`

**Creado**: 2026-06-26

**Estado**: Draft

**Entrada**: Descripción del usuario:

> **Spec 4.7: CRUD Facturas** — GIVEN usuario en `/invoices`, WHEN gestiona facturas, THEN: crear factura (form con validación: cliente, monto, items, vencimiento); editar factura existente (datos no bloqueados por estado); eliminar factura (confirmación modal); POST/PUT/DELETE a `/api/invoices`; toast de éxito/error en cada operación; tabla y dashboard se actualizan vía TanStack Query.
>
> **Spec 4.8: CRUD Clientes** — GIVEN usuario en `/clientes`, WHEN gestiona clientes, THEN: listado de clientes con búsqueda y paginación; crear cliente (form con validación: nombre, email, datos de contacto); editar cliente existente; eliminar cliente (confirmación modal, valida facturas asociadas); POST/PUT/DELETE a `/api/clients`; toast de éxito/error y refresco automático del listado.

## Clarifications

### Session 2026-06-26

- Q: ¿Cómo se obtiene el monto total de la factura respecto a los items? → A: Calculado automáticamente como la suma de los importes de los items (campo de solo lectura, no editable por el usuario).
- Q: ¿El email de un cliente debe ser único? → A: Sí, el email es obligatorio y único entre clientes; crear/editar con un email ya existente se rechaza.
- Q: ¿Qué campos componen los "datos de contacto" del cliente y cuáles son obligatorios? → A: Teléfono y dirección, ambos opcionales; solo nombre y email son obligatorios.
- Q: ¿La eliminación de facturas se restringe por estado (p. ej. pagado/desactivado)? → A: No; se puede eliminar una factura en cualquier estado, previa confirmación. El bloqueo por estado terminal aplica solo a la edición, no al borrado.
- Q: ¿Qué estructura tiene cada línea de detalle (item)? → A: Descripción + cantidad + precio unitario; el subtotal de línea = cantidad × precio unitario, y el monto total = suma de subtotales.

## Escenarios de Usuario y Pruebas *(obligatorio)*

### Historia de Usuario 1 - Gestión completa de Facturas (Prioridad: P1)

Como administrador del módulo de facturación, necesito crear, editar y eliminar facturas desde la interfaz de administración, para mantener actualizada la cartera de cobro sin depender de procesos de carga externos ni manipulación directa de la base de datos.

**Por qué esta prioridad**: La gestión de facturas es la operación central del producto. Hoy el módulo solo permite listar, ver detalle, transicionar estado y marcar como pagada; sin alta/edición/baja, todo dato debe sembrarse manualmente, lo que bloquea el uso real por parte de un administrador.

**Prueba Independiente**: Se puede probar de forma aislada creando una factura nueva desde el formulario, verificando que aparece en la tabla y en las métricas del dashboard; luego editándola y confirmando los cambios persistidos; finalmente eliminándola tras confirmar el modal y verificando que desaparece del listado.

**Escenarios de Aceptación**:

1. **Dado** un administrador en la pantalla de facturas, **Cuando** abre el formulario de creación, completa cliente, monto y los demás campos requeridos con datos válidos y confirma, **Entonces** la factura se crea, se muestra un toast de éxito y la tabla y el dashboard reflejan el nuevo registro sin recargar la página.
2. **Dado** el formulario de creación abierto, **Cuando** intenta confirmar con datos inválidos (sin cliente, monto ≤ 0 o campos requeridos vacíos), **Entonces** el sistema bloquea el envío y muestra mensajes de validación por campo.
3. **Dado** un administrador viendo una factura existente, **Cuando** edita los campos no controlados por el ciclo de estado y confirma, **Entonces** los cambios se persisten, se muestra un toast de éxito y la tabla y el dashboard se actualizan.
4. **Dado** un administrador en el listado, **Cuando** solicita eliminar una factura, **Entonces** se le pide confirmación en un modal antes de proceder.
5. **Dado** el modal de confirmación de eliminación, **Cuando** el administrador confirma, **Entonces** la factura se elimina, se muestra un toast de éxito y desaparece del listado y de las métricas del dashboard.
6. **Dado** una factura en estado terminal (`pagado` o `desactivado`), **Cuando** el administrador intenta editarla, **Entonces** el sistema impide la edición de sus campos e informa el motivo.
7. **Dado** cualquier operación de creación, edición o eliminación, **Cuando** el backend responde con error, **Entonces** se muestra un toast de error con un mensaje comprensible y el estado previo de la interfaz se conserva.

---

### Historia de Usuario 2 - Gestión completa de Clientes (Prioridad: P2)

Como administrador, necesito un módulo para listar, buscar, crear, editar y eliminar clientes, para mantener un catálogo de clientes consistente que sirva de base para asociar facturas.

**Por qué esta prioridad**: Las facturas requieren un cliente asociado. Hoy los clientes solo existen como datos sembrados sin interfaz de gestión. Disponer de un catálogo administrable mejora la calidad de los datos de facturación, aunque la gestión de facturas (P1) puede operar de forma básica sobre los clientes ya existentes.

**Prueba Independiente**: Se puede probar de forma aislada accediendo a la pantalla de clientes, buscando por texto, creando un cliente nuevo con datos válidos y verificando que aparece en el listado; editándolo; e intentando eliminarlo en dos condiciones (con y sin facturas asociadas).

**Escenarios de Aceptación**:

1. **Dado** un administrador en la pantalla de clientes, **Cuando** la pantalla carga, **Entonces** ve un listado paginado de clientes con un campo de búsqueda.
2. **Dado** el listado de clientes, **Cuando** escribe un término de búsqueda, **Entonces** el listado se filtra a los clientes que coinciden por nombre o email y la paginación se ajusta al resultado.
3. **Dado** el formulario de creación de cliente, **Cuando** completa nombre y email válidos junto con los datos de contacto y confirma, **Entonces** el cliente se crea, se muestra un toast de éxito y el listado se refresca automáticamente.
4. **Dado** el formulario de creación, **Cuando** intenta confirmar con email inválido o nombre vacío, **Entonces** el sistema bloquea el envío y muestra mensajes de validación por campo.
5. **Dado** un cliente existente, **Cuando** lo edita y confirma, **Entonces** los cambios se persisten, se muestra un toast de éxito y el listado se actualiza.
6. **Dado** un cliente sin facturas asociadas, **Cuando** confirma su eliminación en el modal, **Entonces** el cliente se elimina, se muestra un toast de éxito y desaparece del listado.
7. **Dado** un cliente con facturas asociadas, **Cuando** intenta eliminarlo, **Entonces** el sistema impide la eliminación y muestra un mensaje explicando que existen facturas asociadas.

---

### Casos Límite

- ¿Qué ocurre al crear una factura seleccionando un cliente que fue eliminado por otro usuario entre la apertura del formulario y el envío? El sistema debe rechazar la operación con un error comprensible y no crear la factura.
- ¿Qué ocurre al editar o eliminar una factura/cliente que ya no existe (eliminada concurrentemente)? La operación debe fallar de forma controlada mostrando un toast de error e invalidando los datos cacheados.
- ¿Qué ocurre con la edición de una factura que ya está en un estado terminal (p. ej. `pagado` o `desactivado`)? Los campos editables (cliente, monto, items, vencimiento) quedan bloqueados: una factura en estado terminal no puede editarse, preservando la integridad de los cobros cerrados. El estado nunca es editable desde este formulario (se gestiona vía transiciones existentes).
- ¿Qué ocurre al buscar clientes sin resultados? El listado muestra un estado vacío explícito en lugar de una tabla en blanco.
- ¿Qué ocurre si se elimina una factura que es la única asociada a un cliente? La factura se elimina; el cliente permanece.

## Requisitos *(obligatorio)*

### Requisitos Funcionales — Facturas

- **RF-001**: El sistema DEBE permitir crear una factura mediante un formulario que capture el cliente asociado, las líneas de detalle (items) y la fecha de vencimiento. El monto total NO se captura directamente: se calcula automáticamente como la suma de los subtotales de los items y se muestra de solo lectura.
- **RF-002**: El sistema DEBE validar, antes de crear o editar una factura, que exista un cliente asociado, que exista al menos una línea de detalle válida (con descripción, cantidad mayor que cero y precio unitario mayor que cero, de modo que el monto total calculado sea mayor que cero) y que la fecha de vencimiento sea válida, mostrando mensajes de validación por campo cuando no se cumplan.
- **RF-003**: El sistema DEBE permitir editar los campos de una factura existente (cliente, monto, items, vencimiento) que no estén controlados por el ciclo de vida de estado, siempre que la factura no se encuentre en un estado terminal.
- **RF-004**: El sistema NO DEBE permitir modificar el estado de la factura desde el formulario de edición; el cambio de estado se realiza únicamente mediante el mecanismo de transiciones existente.
- **RF-004a**: El sistema DEBE bloquear la edición de los campos de una factura cuando esta se encuentre en un estado terminal (`pagado` o `desactivado`).
- **RF-005**: El sistema DEBE permitir eliminar una factura en cualquier estado (incluidos `pagado` y `desactivado`), exigiendo confirmación explícita del usuario mediante un modal antes de ejecutar la eliminación. La restricción por estado terminal aplica solo a la edición (RF-004a), no a la eliminación.
- **RF-006**: El sistema DEBE exponer operaciones de creación, actualización y eliminación de facturas a través de la API de facturas (`/api/invoices`).
- **RF-007**: El sistema DEBE mostrar una notificación (toast) de éxito o de error como resultado de cada operación de creación, edición y eliminación de facturas.
- **RF-008**: Tras una operación exitosa de creación, edición o eliminación, el sistema DEBE actualizar automáticamente la tabla de facturas y las métricas del dashboard sin requerir recarga manual de la página.
- **RF-009**: El sistema DEBE conservar el estado previo de la interfaz cuando una operación falla, sin perder los datos introducidos por el usuario en el formulario.
- **RF-010**: La eliminación de una factura DEBE ser permanente (hard delete): el registro se elimina definitivamente de la persistencia.
- **RF-011**: El sistema DEBE ampliar el modelo de factura para incluir líneas de detalle (items) y una fecha de vencimiento, capturables en creación y edición. Cada línea de detalle se compone de descripción, cantidad y precio unitario; su subtotal DEBE calcularse como cantidad × precio unitario. El monto total de la factura DEBE ser igual a la suma de los subtotales de sus líneas y derivarse automáticamente de ellas (no se ingresa ni edita de forma independiente).

### Requisitos Funcionales — Clientes

- **RF-012**: El sistema DEBE mostrar un listado de clientes con paginación.
- **RF-013**: El sistema DEBE permitir buscar clientes por texto, filtrando al menos por nombre y email.
- **RF-014**: El sistema DEBE permitir crear un cliente mediante un formulario que capture nombre y email (obligatorios) y datos de contacto: teléfono y dirección (ambos opcionales).
- **RF-015**: El sistema DEBE validar, antes de crear o editar un cliente, que el nombre no esté vacío y que el email tenga un formato válido, mostrando mensajes de validación por campo.
- **RF-015a**: El sistema DEBE garantizar que el email del cliente sea único entre todos los clientes; un intento de crear o editar un cliente con un email ya registrado por otro cliente DEBE rechazarse con un mensaje de validación.
- **RF-016**: El sistema DEBE permitir editar un cliente existente.
- **RF-017**: El sistema DEBE permitir eliminar un cliente, exigiendo confirmación explícita mediante un modal.
- **RF-018**: El sistema DEBE impedir la eliminación de un cliente que tenga facturas asociadas, informando al usuario el motivo.
- **RF-019**: El sistema DEBE exponer operaciones de creación, actualización y eliminación de clientes a través de la API de clientes (`/api/clients`).
- **RF-020**: El sistema DEBE mostrar una notificación (toast) de éxito o de error como resultado de cada operación de creación, edición y eliminación de clientes.
- **RF-021**: Tras una operación exitosa, el sistema DEBE refrescar automáticamente el listado de clientes sin requerir recarga manual de la página.

### Entidades Clave

- **Factura**: Representa una obligación de cobro asociada a un cliente. Atributos: identificador, cliente asociado, monto (derivado: suma de los subtotales de los items), líneas de detalle (items), fecha de vencimiento, estado del ciclo de vida, marcas de auditoría (creación/actualización), contadores de recordatorios e historial de estado. La gestión de estado permanece fuera del formulario de edición. En estados terminales (`pagado`/`desactivado`) los campos no son editables.
- **Línea de detalle (Item)**: Componente de una factura que describe un concepto cobrado. Atributos: descripción, cantidad y precio unitario; el subtotal de la línea es cantidad × precio unitario. Una factura tiene una o más líneas de detalle cuya suma de subtotales determina el monto total.
- **Cliente**: Representa una entidad a la que se le emiten facturas. Atributos: identificador, nombre (obligatorio), email (obligatorio y único entre clientes), teléfono (opcional) y dirección (opcional). Un cliente puede tener cero o más facturas asociadas. La relación con facturas condiciona la eliminación (RF-018).

## Criterios de Éxito *(obligatorio)*

### Resultados Medibles

- **CE-001**: Un administrador puede crear una factura válida y verla reflejada en la tabla y el dashboard en menos de 5 segundos desde la confirmación, sin recargar la página.
- **CE-002**: El 100% de las operaciones de creación, edición y eliminación (de facturas y clientes) producen una notificación de éxito o error visible para el usuario.
- **CE-003**: El 100% de los intentos de crear/editar con datos inválidos son rechazados antes de enviarse, con al menos un mensaje de validación visible por campo erróneo.
- **CE-004**: El 100% de los intentos de eliminar un cliente con facturas asociadas son rechazados con un mensaje explicativo, sin eliminar el cliente.
- **CE-005**: La búsqueda de clientes devuelve el listado filtrado de forma perceptible como inmediata (resultado visible en menos de 1 segundo bajo carga normal).
- **CE-006**: Tras cualquier operación exitosa, los datos mostrados (tabla de facturas, dashboard, listado de clientes) coinciden con el estado persistido sin intervención manual del usuario.
- **CE-007**: Toda eliminación se ejecuta únicamente tras una confirmación explícita del usuario (cero eliminaciones sin paso de confirmación).

## Supuestos

- El acceso a estas operaciones está restringido a usuarios administradores autenticados, conforme al esquema de autenticación existente (JWT Admin-only).
- La pantalla de gestión de facturas corresponde a la ruta existente del módulo de facturas (`/facturas`); la referencia a `/invoices` en la descripción se interpreta como esa pantalla. La gestión de clientes se expone en una nueva ruta `/clientes`.
- La API de clientes (`/api/clients`) y la entidad Cliente no existen actualmente y se construyen como parte de esta funcionalidad; hoy el cliente solo se referencia como identificador dentro de la factura y mediante datos sembrados.
- La sincronización de tabla y dashboard se logra mediante invalidación/refresco del estado de servidor en el cliente (TanStack Query), conforme al stack tecnológico definido.
- La validación del frontend refleja las reglas de validación del backend, conforme a la constitución (FluentValidation en API).
- El cambio de estado de la factura permanece gobernado por el mecanismo de transiciones existente y queda fuera del alcance de la edición de esta funcionalidad.
- "Datos de contacto" de un cliente se concretan como teléfono y dirección, ambos opcionales; el email es el único dato de contacto obligatorio (además del nombre).
