# Feature Specification: worker-state-transitions

**Feature Branch**: `012-worker-state-transitions`

**Created**: 2026-06-25

**Status**: Activo

**Input**: User description: "### Spec 3.2: Hosted Service - State Transitions — worker corriendo que se ejecuta cada X minutos (configurable), busca facturas en `primerrecordatorio` con días suficientes sin recordatorio, busca facturas en `segundorecordatorio` con días suficientes sin recordatorio, ejecuta transiciones automáticas y registra la ejecución en logs."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ejecución Automática de Transiciones por Tiempo (Priority: P1)

Como sistema, quiero que un proceso en segundo plano revise periódicamente las facturas activas y aplique las transiciones de estado que correspondan según el tiempo transcurrido, para automatizar el ciclo de cobro sin intervención manual.

**Why this priority**: Es el corazón de la automatización del cobro; sin este proceso las transiciones de estado nunca ocurren de forma desatendida y el negocio depende de acciones manuales.

**Independent Test**: Puede probarse de forma aislada preparando facturas en `primerrecordatorio` y `segundorecordatorio` que cumplan los días de espera configurados, disparando una ejecución del proceso y verificando que cada factura elegible cambió al estado siguiente y que las no elegibles permanecieron sin cambios.

**Acceptance Scenarios**:

1. **Given** facturas en estado `primerrecordatorio` cuyo tiempo sin recordatorio supera el umbral configurado, **When** el proceso en segundo plano se ejecuta, **Then** dichas facturas avanzan a `segundorecordatorio`.
2. **Given** facturas en estado `segundorecordatorio` cuyo tiempo sin recordatorio supera el umbral configurado, **When** el proceso en segundo plano se ejecuta, **Then** dichas facturas avanzan a `desactivado`.
3. **Given** facturas que aún no cumplen el tiempo de espera configurado, **When** el proceso se ejecuta, **Then** dichas facturas permanecen en su estado actual.
4. **Given** facturas en estado `pagado` o `desactivado`, **When** el proceso se ejecuta, **Then** dichas facturas se ignoran y no sufren transición alguna.

---

### User Story 2 - Programación Periódica Configurable (Priority: P2)

Como administrador, quiero poder configurar cada cuánto tiempo se ejecuta el proceso en segundo plano, para ajustar la frecuencia de revisión a las necesidades operativas sin recompilar ni redeployar.

**Why this priority**: Permite balancear oportunidad de cobro contra carga del sistema; es necesaria para operar el worker en distintos entornos, aunque depende de que exista la lógica de transición de la P1.

**Independent Test**: Puede probarse configurando un intervalo, observando que el proceso se dispara repetidamente respetando ese intervalo, y luego cambiando el valor para verificar que la nueva frecuencia se aplica.

**Acceptance Scenarios**:

1. **Given** un intervalo de ejecución configurado, **When** el proceso está corriendo, **Then** la revisión de facturas se dispara repetidamente respetando ese intervalo.
2. **Given** que no se especifica un intervalo, **When** el proceso arranca, **Then** se utiliza un intervalo por defecto razonable y se registra cuál se está usando.

---

### User Story 3 - Trazabilidad de Cada Ejecución (Priority: P3)

Como operador del sistema, quiero que cada ejecución del proceso quede registrada con su resultado, para auditar el comportamiento del cobro automático y diagnosticar problemas.

**Why this priority**: Mejora la observabilidad y el soporte, pero el valor de negocio principal (las transiciones) se entrega aun sin registros detallados.

**Independent Test**: Puede probarse ejecutando el proceso y verificando que se generan registros estructurados con marca de tiempo, cantidad de facturas evaluadas, transiciones aplicadas y errores encontrados.

**Acceptance Scenarios**:

1. **Given** una ejecución del proceso, **When** finaliza, **Then** se registra un evento con marca de tiempo, número de facturas evaluadas y número de transiciones aplicadas.
2. **Given** una factura que falla al transicionar, **When** el proceso la procesa, **Then** el error se registra con el identificador de la factura y el proceso continúa con las demás facturas.

### Edge Cases

- ¿Qué sucede cuando una ejecución no encuentra ninguna factura elegible? (El proceso completa la ejecución sin cambios y registra un resultado vacío).
- ¿Qué sucede si una ejecución tarda más que el intervalo configurado? (No deben solaparse ejecuciones concurrentes que procesen las mismas facturas dos veces).
- ¿Qué sucede si falla el acceso a la persistencia durante una ejecución? (La ejecución se marca como fallida en los registros y se reintenta en el siguiente ciclo, sin tumbar el proceso).
- ¿Qué sucede si una factura individual produce un error al aplicar la transición? (Se aísla el error de esa factura y se continúa con el resto del lote).
- ¿Qué sucede si se detiene el proceso a mitad de un ciclo? (El apagado es ordenado y el estado vive en la persistencia, de modo que el siguiente arranque retoma sin inconsistencias).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema DEBE ejecutar un proceso en segundo plano de forma periódica según un intervalo de tiempo configurable.
- **FR-002**: El sistema DEBE usar un intervalo por defecto razonable cuando no se haya configurado uno explícitamente.
- **FR-003**: En cada ejecución el sistema DEBE identificar las facturas en estado `primerrecordatorio` cuyo tiempo transcurrido sin recordatorio supere el umbral de días configurado para esa transición.
- **FR-004**: En cada ejecución el sistema DEBE identificar las facturas en estado `segundorecordatorio` cuyo tiempo transcurrido sin recordatorio supere el umbral de días configurado para esa transición.
- **FR-005**: El sistema DEBE aplicar las transiciones de estado a las facturas elegibles delegando en las reglas de dominio de transición ya definidas, sin duplicar dichas reglas.
- **FR-006**: El sistema DEBE ignorar las facturas que se encuentren en estados terminales o no elegibles (por ejemplo `pagado` y `desactivado`).
- **FR-007**: El sistema DEBE aislar los errores de una factura individual de modo que un fallo no impida procesar el resto del lote.
- **FR-008**: El sistema DEBE registrar cada ejecución con, como mínimo, marca de tiempo, cantidad de facturas evaluadas, cantidad de transiciones aplicadas y cantidad de errores.
- **FR-009**: El sistema DEBE registrar, por cada transición aplicada, el identificador de la factura, su estado anterior y su nuevo estado.
- **FR-010**: El sistema DEBE evitar el solapamiento de ejecuciones que procesen las mismas facturas cuando una ejecución supere la duración del intervalo.
- **FR-011**: El sistema DEBE realizar un apagado ordenado al detenerse, sin dejar el procesamiento en un estado inconsistente.
- **FR-012**: El sistema DEBE leer los umbrales de días por transición desde la configuración administrable existente, en lugar de valores fijos en el código.

### Key Entities *(include if feature involves data)*

- **Invoice**: Factura sujeta a transición; relevante por su estado (`Status`), la marca de tiempo del último recordatorio enviado y el conteo de recordatorios, que determinan su elegibilidad para transicionar.
- **Configuración de Ejecución**: Parámetros que gobiernan el proceso en segundo plano: intervalo de ejecución y umbrales de días por cada transición de estado.
- **Registro de Ejecución**: Evidencia de cada corrida del proceso (marca de tiempo, facturas evaluadas, transiciones aplicadas, errores) destinada a observabilidad y auditoría.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de las facturas elegibles (que cumplen el umbral de días) cambian al estado correcto en la ejecución del proceso que las evalúa.
- **SC-002**: El 0% de las facturas no elegibles o en estados terminales sufren transiciones durante las ejecuciones.
- **SC-003**: El 100% de las ejecuciones del proceso producen un registro de resultado consultable, incluso cuando no se aplica ninguna transición.
- **SC-004**: Un error al procesar una factura individual no impide procesar el resto del lote en el 100% de los casos (el lote continúa).
- **SC-005**: El intervalo de ejecución puede modificarse y la nueva frecuencia queda activa sin necesidad de cambios en el código.

## Assumptions

- Las reglas de transición de estado (qué estado puede pasar a cuál y bajo qué condiciones) ya están definidas en el dominio (ver feature `006-invoice-status-transitions`) y este proceso solo las invoca; no las redefine.
- El envío de correos al ocurrir una transición es responsabilidad de una feature separada (Spec 3.3) y queda fuera del alcance de este worker.
- Los umbrales de días por transición provienen de la vista de configuración administrable definida previamente; los valores del roadmap (7 y 14 días) son defaults de referencia, no valores fijos.
- El estado del procesamiento reside en la persistencia (MongoDB) y no en memoria, permitiendo que el proceso sea reiniciable y escalable horizontalmente.
- "Tiempo sin recordatorio" se calcula a partir de la marca de tiempo del último recordatorio de la factura; si no existe, se usa una fecha de referencia equivalente (por ejemplo, la creación de la factura).
- La ejecución periódica se apoya en un proceso/host de larga vida (hosted service/worker) acorde al stack del proyecto.
