# Especificación de Funcionalidad: Envío de Correos y Registro en Transiciones

**Rama de Funcionalidad**: `013-transition-email-notifications`

**Creado**: 2026-06-25

**Estado**: Activo

**Entrada**: User description: "### Spec 3.3: Email Sending on Transition — GIVEN transición de estado WHEN worker procesa factura THEN envía correo con template según nuevo estado, actualiza `LastReminderSentAt`, incrementa `RemindersCount`, registra éxito/error en BD. ### Spec 3.4: Logging & Monitoring — GIVEN worker ejecutándose WHEN procesa facturas THEN Serilog registra timestamp, factura, estado anterior/nuevo, resultado email; logs persistidos (file o cloud); estructurado en formato JSON."

## Clarifications

### Session 2026-06-25

- Q: ¿Qué transiciones disparan el envío de correo? → A: Cualquier transición de estado de una factura, sea automática del worker o manual vía API (incluida la confirmación de pago al pasar a `pagado`).
- Q: ¿Se envía correo al transicionar a `desactivado`? → A: Sí, se envía un correo de aviso de desactivación / última notificación al cliente.
- Q: ¿Dónde se registra el resultado del envío? → A: En la propia factura (último resultado de envío más los metadatos `LastReminderSentAt` y `RemindersCount`).
- Q: ¿Qué ocurre si falla el envío? → A: La transición ya aplicada NO se revierte, no se actualizan los contadores de recordatorio, y la factura se reintenta de forma natural en el siguiente ciclo del worker si sigue siendo elegible.

## Escenarios de Usuario y Pruebas *(obligatorio)*

### Historia de Usuario 1 - Notificación al Cliente Según el Nuevo Estado (Prioridad: P1)

Como sistema de gestión de cartera, quiero que cuando una factura cambia de estado —ya sea por el procesamiento automático del worker o por una acción manual a través de la API— se envíe al cliente un correo con la plantilla correspondiente al nuevo estado, para mantener al cliente informado del avance del cobro.

**Por qué esta prioridad**: Es el valor de negocio central de esta funcionalidad: convertir las transiciones de estado en comunicación efectiva con el cliente. Sin el envío del correo, la transición de estado no produce ningún efecto visible para el cliente.

**Prueba Independiente**: Puede probarse de forma aislada preparando una factura elegible, disparando una transición de estado y verificando, mediante una implementación falsa del contrato de correo, que se solicita el envío con el correo del cliente, la factura y la plantilla correspondiente al nuevo estado.

**Escenarios de Aceptación**:

1. **Dado** una factura que transiciona a un estado de recordatorio (`segundorecordatorio`), **Cuando** el worker procesa la factura, **Entonces** se envía un correo de recordatorio al cliente usando la plantilla asociada a ese estado.
2. **Dado** una factura que transiciona a `pagado` (por ejemplo, mediante una acción manual vía API), **Cuando** se aplica la transición, **Entonces** se envía un correo de confirmación de pago al cliente usando la plantilla asociada a ese estado.
3. **Dado** una factura que transiciona a `desactivado`, **Cuando** el worker procesa la factura, **Entonces** se envía un correo de aviso de desactivación / última notificación al cliente usando la plantilla asociada a ese estado.
4. **Dado** una factura que transiciona a un estado sin plantilla de correo asociada, **Cuando** se procesa la transición, **Entonces** no se intenta el envío y se registra que no había notificación aplicable.

---

### Historia de Usuario 2 - Seguimiento de Recordatorios en la Factura (Prioridad: P1)

Como sistema, quiero que tras un envío exitoso se actualice la marca de tiempo del último recordatorio y se incremente el conteo de recordatorios de la factura, y que el resultado del envío quede registrado en la persistencia, para que el propio worker no reenvíe correos antes de tiempo y para auditar qué notificaciones se entregaron.

**Por qué esta prioridad**: El conteo y la marca de tiempo de recordatorio son insumos que el worker de transiciones (feature `012`) usa para decidir la elegibilidad; si no se actualizan tras enviar, el sistema reenviaría correos o transicionaría de forma incorrecta. Registrar el resultado en BD es la fuente de verdad para auditoría.

**Prueba Independiente**: Puede probarse disparando una transición con envío exitoso simulado y verificando que la factura persistida tiene la nueva marca de tiempo del último recordatorio, el conteo incrementado en uno y el resultado del envío registrado.

**Escenarios de Aceptación**:

1. **Dado** una transición cuyo envío de correo se completa con éxito, **Cuando** finaliza el procesamiento de la factura, **Entonces** la marca de tiempo del último recordatorio se actualiza al momento del envío y el conteo de recordatorios se incrementa en uno.
2. **Dado** una transición cuyo envío de correo falla, **Cuando** finaliza el procesamiento de la factura, **Entonces** no se incrementa el conteo de recordatorios ni se actualiza la marca de tiempo del último recordatorio, y se registra el resultado de error en la persistencia.
3. **Dado** cualquier intento de envío (éxito o error), **Cuando** finaliza el procesamiento de la factura, **Entonces** el resultado del envío queda registrado de forma consultable en la persistencia.

---

### Historia de Usuario 3 - Observabilidad Estructurada del Envío (Prioridad: P2)

Como operador del sistema, quiero que cada procesamiento de factura registre de forma estructurada la marca de tiempo, la factura, el estado anterior y el nuevo, y el resultado del envío del correo, en registros persistidos, para auditar el comportamiento del cobro automático y diagnosticar problemas de notificación.

**Por qué esta prioridad**: Mejora la observabilidad y el soporte, pero el valor de negocio principal (la notificación y el seguimiento) se entrega aun sin registros detallados; por eso es P2.

**Prueba Independiente**: Puede probarse procesando una factura que transiciona y verificando que se genera un registro estructurado (formato JSON) con marca de tiempo, identificador de factura, estado anterior, nuevo estado y resultado del envío, y que dicho registro queda persistido.

**Escenarios de Aceptación**:

1. **Dado** una factura que el worker procesa, **Cuando** se aplica la transición e intenta el envío, **Entonces** se genera un registro estructurado con marca de tiempo, identificador de factura, estado anterior, nuevo estado y resultado del envío.
2. **Dado** un fallo en el envío del correo, **Cuando** el worker procesa la factura, **Entonces** el registro estructurado refleja el resultado de error con suficiente contexto para diagnosticar (factura y motivo del fallo).
3. **Dado** los registros generados por el worker, **Cuando** se consultan, **Entonces** están en formato estructurado JSON y persistidos en un destino consultable.

### Casos Límite

- ¿Qué ocurre cuando el correo del cliente es nulo, vacío o tiene formato inválido? El envío no se intenta o se reporta como error; la transición de estado ya aplicada no se revierte y el lote continúa.
- ¿Qué ocurre cuando el proveedor de correo no está disponible o falla el envío? Se registra el error (en BD y en logs), no se actualizan el conteo ni la marca de tiempo de recordatorio, la transición no se revierte y el procesamiento del resto del lote continúa.
- ¿Qué ocurre cuando la factura transiciona a un estado que no tiene plantilla de correo asociada? No se intenta envío y se registra que no aplicaba notificación (los estados con plantilla son recordatorio, `pagado` y `desactivado`).
- ¿Qué ocurre si el mismo ciclo del worker vuelve a evaluar una factura ya notificada? La marca de tiempo y el conteo actualizados evitan reenvíos antes de cumplir nuevamente el umbral.
- ¿Qué ocurre si falla la persistencia del resultado del envío después de enviar el correo? El fallo se registra en logs; el efecto debe quedar trazable para evitar reenvíos indebidos en el siguiente ciclo.
- ¿Qué ocurre si el envío de correo tarda demasiado? El procesamiento de una factura individual no debe bloquear indefinidamente el lote; un fallo o tiempo de espera se aísla por factura.

## Requisitos *(obligatorio)*

### Requisitos Funcionales

- **RF-001**: Cuando una factura cambia de estado —sea por una transición automática del worker o por una transición manual a través de la API—, el sistema DEBE intentar enviar al cliente un correo cuya plantilla corresponda al nuevo estado de la factura.
- **RF-002**: El sistema DEBE seleccionar la plantilla de correo en función del nuevo estado de la factura: recordatorio para los estados de recordatorio, confirmación para `pagado` y aviso de desactivación para `desactivado`.
- **RF-003**: El sistema DEBE realizar el envío a través del contrato abstracto de correo existente (`IEmailService`, feature `011`), sin acoplarse a un proveedor de correo concreto fuera de la capa de infraestructura.
- **RF-004**: Cuando el envío del correo se completa con éxito, el sistema DEBE actualizar en la factura la marca de tiempo del último recordatorio (`LastReminderSentAt`) al momento del envío.
- **RF-005**: Cuando el envío del correo se completa con éxito, el sistema DEBE incrementar en uno el conteo de recordatorios (`RemindersCount`) de la factura.
- **RF-006**: Cuando el envío del correo falla, el sistema NO DEBE actualizar la marca de tiempo del último recordatorio ni incrementar el conteo de recordatorios.
- **RF-007**: El sistema DEBE registrar el resultado de cada intento de envío (éxito o error) sobre la propia factura, como último resultado de envío consultable, junto con los metadatos de recordatorio.
- **RF-008**: El sistema DEBE aislar los fallos de envío o de actualización por factura, de modo que un error en una factura no aborte el procesamiento del resto del lote ni revierta la transición de estado ya aplicada.
- **RF-009**: El sistema DEBE omitir el envío cuando el nuevo estado no tenga una plantilla de notificación asociada, registrando que no había notificación aplicable.
- **RF-010**: El sistema DEBE omitir o reportar como error el envío cuando el correo del cliente sea nulo, vacío o con formato inválido, sin interrumpir el lote.
- **RF-011**: Por cada factura procesada, el sistema DEBE generar un registro estructurado con, como mínimo, marca de tiempo, identificador de factura, estado anterior, nuevo estado y resultado del envío del correo.
- **RF-012**: Los registros generados DEBEN estar en formato estructurado JSON.
- **RF-013**: Los registros generados DEBEN persistirse en un destino consultable (archivo o destino en la nube).
- **RF-014**: Ante un fallo de envío, el registro estructurado DEBE incluir contexto suficiente para diagnóstico (al menos identificador de factura y motivo del fallo).
- **RF-015**: Ante un fallo de envío, el sistema NO DEBE revertir la transición de estado ya aplicada ni marcar la factura para intervención manual; la factura DEBE poder reevaluarse y reintentarse de forma natural en el siguiente ciclo del worker si continúa siendo elegible.

### Entidades Clave

- **Invoice**: Factura sujeta a notificación; relevante por su estado (`Status`), la marca de tiempo del último recordatorio (`LastReminderSentAt`), el conteo de recordatorios (`RemindersCount`) y el correo del cliente destinatario. Definida en `005-invoice-entity`.
- **Plantilla de Correo**: Contenido y formato del correo asociado a un estado destino de la factura (recordatorio, confirmación de pago, aviso de desactivación); determina qué se envía según el nuevo estado.
- **Resultado de Envío**: Evidencia del último intento de notificación (éxito o error, momento, motivo en caso de fallo) que se persiste sobre la propia factura para auditoría y para evitar reenvíos indebidos.
- **Registro de Procesamiento**: Entrada estructurada de observabilidad por factura procesada (marca de tiempo, factura, estado anterior, nuevo estado, resultado del envío), persistida en formato JSON.

## Criterios de Éxito *(obligatorio)*

### Resultados Medibles

- **CE-001**: El 100% de las transiciones a un estado con plantilla asociada producen un intento de envío de correo al cliente.
- **CE-002**: El 100% de los envíos exitosos resultan en la actualización de la marca de tiempo del último recordatorio y el incremento en uno del conteo de recordatorios de la factura.
- **CE-003**: El 0% de los envíos fallidos modifican la marca de tiempo del último recordatorio o el conteo de recordatorios.
- **CE-004**: El 100% de los intentos de envío (éxito o error) quedan registrados de forma consultable en la persistencia.
- **CE-005**: El 100% de las facturas procesadas generan un registro estructurado en formato JSON con marca de tiempo, factura, estado anterior, nuevo estado y resultado del envío.
- **CE-006**: Un fallo en el envío o en la actualización de una factura no aborta el procesamiento del resto del lote ni revierte la transición ya aplicada en el 100% de los casos.

## Suposiciones

- El contrato abstracto de correo (`IEmailService`) ya está definido (feature `011`) y expone operaciones de recordatorio y de confirmación de pago; esta feature lo consume. El aviso de desactivación puede requerir una operación/plantilla adicional sobre dicho contrato, lo que se detallará en la planificación.
- El worker de transiciones de estado (feature `012`) ya aplica las transiciones automáticas y aísla errores por factura; esta feature añade el envío del correo y el registro como efecto del procesamiento de cada transición, sin redefinir las reglas de transición. El envío también se dispara ante transiciones manuales realizadas a través de la API (p. ej. marcar `pagado`).
- La transición de estado y el envío del correo se tratan como efectos relacionados pero independientes en cuanto a consistencia: la transición ya aplicada no se revierte si el correo falla; los contadores de recordatorio solo se actualizan ante envío exitoso; las facturas con envío fallido se reintentan de forma natural en el siguiente ciclo del worker si siguen siendo elegibles.
- El mapeo entre estado destino y plantilla de correo es: estados de recordatorio (`primerrecordatorio`/`segundorecordatorio`) usan plantilla de recordatorio; `pagado` usa plantilla de confirmación de pago; `desactivado` usa plantilla de aviso de desactivación / última notificación.
- El correo del cliente proviene de los datos de la factura/cliente existentes; su obtención y validación de formato es responsabilidad de quien invoca el contrato y de la implementación de correo.
- La persistencia del resultado de envío y de los metadatos de recordatorio se realiza sobre la factura en MongoDB, manteniendo el estado fuera de memoria para soportar reinicios y escalado horizontal del worker.
- El logging estructurado se apoya en el stack de observabilidad del proyecto (Serilog, formato JSON), conforme a la constitución; el destino persistente puede ser archivo o un destino en la nube según configuración del entorno.
- El contenido concreto y diseño visual de las plantillas de correo (HTML, branding) es responsabilidad de la implementación; esta spec define qué plantilla aplica según el estado, no su maquetación.
