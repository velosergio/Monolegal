# Feature Specification: Tests de Componentes Frontend

**Feature Branch**: `022-tests-componentes-frontend`

**Created**: 2026-06-29

**Status**: Activo

**Input**: Roadmap Spec 5.3 — "Frontend Component Tests": componentes React verificados con Vitest (renderiza sin errores, interacciones simuladas, async handlers con TanStack Query mockeado, snapshot tests para UI crítica).

## Contexto

La suite de pruebas del frontend ya cubre de forma amplia tres de los cuatro criterios de la Spec 5.3: render sin errores, interacciones simuladas (click/select) y manejadores asíncronos con TanStack Query (48 archivos de test, 161 casos en verde). El criterio pendiente es **snapshot tests para UI crítica**: hoy no existe ningún snapshot en el proyecto. Además, varios componentes **presentacionales** estables aún no tienen ninguna prueba dedicada (insignias de estado, tarjetas de métricas, estados vacíos, esqueletos de carga, pie del sidebar).

Esta feature cierra la Spec 5.3 estableciendo regresión por snapshot para la UI crítica estable y completando la cobertura de render/estructura de los componentes presentacionales que aún no se prueban. No se modifica código de producción: solo se añaden pruebas.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Regresión por snapshot de la UI crítica (Priority: P1)

Como responsable de calidad del frontend, quiero que los componentes presentacionales críticos y estables tengan pruebas de snapshot, de modo que cualquier cambio no intencionado en su marcado (estructura, clases, textos) se detecte automáticamente en CI antes de llegar a producción.

**Why this priority**: Es el único criterio de la Spec 5.3 que aún no se cumple. Aporta una red de seguridad de regresión visual/estructural sobre los componentes que definen la identidad de la interfaz (insignias de estado, tarjetas, estados vacíos, esqueletos).

**Independent Test**: Se puede validar de forma independiente ejecutando la suite de Vitest: la primera corrida genera los snapshots y las corridas siguientes fallan si el marcado cambia sin actualizar el snapshot. Entrega valor por sí sola como detección de regresión.

**Acceptance Scenarios**:

1. **Given** un componente presentacional crítico estable (p. ej. la insignia de estado de factura), **When** se ejecuta su prueba de snapshot por primera vez, **Then** se almacena un snapshot determinista del marcado renderizado.
2. **Given** un snapshot previamente almacenado, **When** el marcado del componente no cambia entre corridas, **Then** la prueba pasa sin diferencias.
3. **Given** un snapshot previamente almacenado, **When** el marcado del componente cambia, **Then** la prueba falla mostrando la diferencia, obligando a revisar y actualizar el snapshot de forma consciente.
4. **Given** un componente cuyo contenido depende de la fecha actual (pie con el año), **When** se ejecuta su snapshot, **Then** el resultado es determinista porque la fecha del sistema se fija en la prueba.

---

### User Story 2 - Cobertura de render/estructura de componentes presentacionales sin pruebas (Priority: P2)

Como desarrollador del frontend, quiero que los componentes presentacionales que hoy carecen de prueba dedicada verifiquen que renderizan sin errores y exponen su contenido/estructura accesible, para que la regla "renderiza sin errores" de la Spec 5.3 aplique a toda la UI crítica y no solo a la mayoría.

**Why this priority**: Complementa el snapshot con aserciones legibles sobre el contenido visible y la accesibilidad (roles, textos, variantes por prop), reduciendo la dependencia exclusiva del snapshot y documentando el comportamiento esperado de cada componente.

**Independent Test**: Cada componente sin cobertura recibe un archivo de prueba que lo renderiza con props representativas y afirma el contenido/estructura visible; se valida ejecutando Vitest sobre ese archivo.

**Acceptance Scenarios**:

1. **Given** la insignia de estado de factura, **When** se renderiza con un estado conocido, **Then** muestra la etiqueta legible correspondiente; **And** con un estado desconocido muestra el valor en bruto con estilo neutro.
2. **Given** la tarjeta de métrica del dashboard, **When** se renderiza con etiqueta y valor, **Then** ambos son visibles; **And** el ícono opcional se marca como decorativo (oculto a lectores de pantalla) cuando se provee.
3. **Given** un estado vacío (listado de facturas/envíos), **When** se renderiza, **Then** comunica el mensaje de "sin resultados" con su texto de ayuda.
4. **Given** un esqueleto de carga, **When** se renderiza, **Then** queda oculto a tecnologías de asistencia y reproduce la estructura de columnas del contenido real.

---

### User Story 3 - Verificación consolidada de los cuatro criterios de la Spec 5.3 (Priority: P3)

Como mantenedor del roadmap, quiero una verificación consolidada de que los cuatro criterios de la Spec 5.3 (render, interacciones, async, snapshots) están cubiertos por la suite de Vitest, para poder marcar la spec como implementada con evidencia.

**Why this priority**: Cierra formalmente la spec y deja trazabilidad entre criterios del roadmap y pruebas existentes/nuevas, sin duplicar pruebas que ya cubren render/interacción/async.

**Independent Test**: Se ejecuta la suite completa de Vitest y se confirma que pasa en verde, incluyendo los nuevos snapshots; se documenta el inventario criterio → prueba.

**Acceptance Scenarios**:

1. **Given** la suite completa del frontend, **When** se ejecuta `vitest run`, **Then** todos los archivos de prueba pasan, incluidos los nuevos de snapshot y de render.
2. **Given** los cuatro criterios del roadmap, **When** se revisa el inventario de pruebas, **Then** cada criterio tiene al menos una prueba que lo respalda.

---

### Edge Cases

- ¿Qué ocurre con componentes que usan animación (Motion) o valores no deterministas (fechas, aleatoriedad)? → Se excluyen del snapshot directo o se neutraliza la fuente de indeterminismo (fijar fecha del sistema, preferencia de movimiento reducido) para que el snapshot sea estable.
- ¿Qué pasa si un snapshot legítimamente debe cambiar (rediseño)? → La diferencia obliga a una actualización consciente del snapshot revisada en el PR; nunca se actualiza a ciegas en CI.
- ¿Componentes con estado desconocido/valores límite (estado de factura no reconocido, listas vacías, total 0)? → Las pruebas cubren explícitamente la rama neutra/vacía.
- ¿Componentes que dependen de proveedores (tema, router, query client)? → Se renderizan con los proveedores mínimos necesarios; los snapshots se limitan a componentes presentacionales con dependencias acotadas.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La suite DEBE incluir pruebas de snapshot para los componentes presentacionales críticos y estables identificados (insignias de estado, tarjeta de métrica, estados vacíos, esqueletos de carga, pie del sidebar).
- **FR-002**: Los snapshots DEBEN ser deterministas: cualquier fuente de indeterminismo (fecha actual, aleatoriedad, animación) se neutraliza dentro de la prueba para evitar fallos intermitentes.
- **FR-003**: Los componentes presentacionales que hoy carecen de prueba dedicada DEBEN recibir al menos una prueba que verifique render sin errores y contenido/estructura visible.
- **FR-004**: Las pruebas de render NO DEBEN depender únicamente del snapshot; DEBEN incluir aserciones legibles sobre contenido, roles accesibles o variantes por prop cuando aplique.
- **FR-005**: Las pruebas DEBEN cubrir las variantes/ramas relevantes de cada componente (estado conocido vs desconocido, presencia/ausencia de prop opcional, estado colapsado vs expandido).
- **FR-006**: NO se DEBE modificar código de producción para esta feature; el alcance es exclusivamente añadir pruebas.
- **FR-007**: La suite completa de Vitest DEBE pasar en verde tras añadir las nuevas pruebas, sin pruebas omitidas (`.skip`/`.only`), conforme al Principio IV.
- **FR-008**: La feature DEBE dejar trazabilidad entre los cuatro criterios del roadmap (render, interacciones, async, snapshots) y las pruebas que los respaldan.
- **FR-009**: Las nuevas pruebas DEBEN seguir las convenciones existentes del proyecto (Vitest + Testing Library, alias `@/`, setup global compartido, documentación en español).

### Key Entities *(include if feature involves data)*

- **Componente presentacional crítico**: Componente de UI determinista, con pocas dependencias, cuyo marcado es estable y representativo de la identidad visual (insignia de estado, tarjeta de métrica, estado vacío, esqueleto, pie). Es candidato a snapshot.
- **Snapshot**: Representación serializada del marcado renderizado de un componente, almacenada en el repositorio y comparada en cada corrida para detectar regresiones.
- **Inventario de criterios**: Tabla de trazabilidad que asocia cada criterio del roadmap (render, interacciones, async, snapshots) con las pruebas que lo respaldan.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: El 100% de los componentes presentacionales críticos identificados cuenta con al menos una prueba de snapshot.
- **SC-002**: Los componentes presentacionales que antes no tenían prueba dedicada quedan con cobertura de render/estructura (0 componentes presentacionales críticos sin prueba al cierre).
- **SC-003**: La suite completa del frontend pasa en verde en dos corridas consecutivas sin diferencias de snapshot (cero fallos intermitentes).
- **SC-004**: Los cuatro criterios de la Spec 5.3 quedan respaldados por al menos una prueba cada uno, documentado en un inventario de trazabilidad.
- **SC-005**: No se introduce ningún cambio en código de producción del frontend (solo archivos de prueba y de snapshot).

## Assumptions

- La infraestructura de pruebas (Vitest, Testing Library, jsdom, setup global con polyfills de Radix/Motion/localStorage) ya está configurada y operativa; no requiere cambios.
- "UI crítica" se interpreta como los componentes presentacionales deterministas que definen la identidad visual y los estados base de la aplicación (insignias, tarjetas, estados vacíos, esqueletos, pie); las páginas compuestas y los componentes con lógica asíncrona ya están cubiertos por pruebas de interacción/async existentes y no requieren snapshot.
- Los snapshots se almacenan en el repositorio y se revisan en code review; la actualización a ciegas en CI no está permitida.
- Los criterios de render, interacciones y async ya están cubiertos por la suite existente; esta feature añade el criterio de snapshot y completa los huecos de render, sin duplicar pruebas existentes.
