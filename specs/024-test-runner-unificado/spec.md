# Feature Specification: Test Runner Unificado

**Feature Branch**: `024-test-runner-unificado`

**Created**: 2026-06-30

**Status**: Draft

**Input**: Roadmap Spec 5.5 — "Test Runner Unificado": dados los distintos sistemas de test del proyecto (backend xUnit, worker xUnit, frontend Vitest, E2E Playwright), cuando se ejecuta un único comando, entonces corre todas las suites de forma multiplataforma, reporta un resultado consolidado (PASS/FAIL por suite) con código de salida agregado y falla el comando completo si cualquier suite falla (apto para CI).

## Contexto

El proyecto ya cuenta con cobertura de pruebas en los tres niveles de la pirámide y en sus distintos componentes: unitarias de dominio (Spec 5.1), de integración de API (Spec 5.2), pruebas del worker de transiciones, de componentes de frontend con Vitest (Spec 5.3) y E2E con Playwright (Spec 5.4). Sin embargo, cada suite se ejecuta con su propia herramienta y comando: las pruebas de backend y de worker con `dotnet test` (cada una en su propio proyecto de pruebas), las de frontend con `vitest run` y las E2E con `playwright test`. Hoy no existe una forma única de correr **todas** las suites de una sola vez.

Esta fragmentación obliga a quien desarrolla a recordar y ejecutar varios comandos en distintos directorios, y dificulta la integración con CI, donde el Principio IV de la constitución exige que ningún cambio se fusione sin pasar **todas** las suites de tests ("CI Gate: sin merge sin pasar todas las suites de tests"). La falta de un punto de entrada único hace fácil olvidar una suite —en particular la del worker, que vive en un proyecto aparte— y deja la verificación completa a criterio manual.

Esta feature establece un **punto de entrada único** para ejecutar todas las suites con un solo comando, multiplataforma (Windows/Linux), que reporta el resultado de cada suite de forma consolidada y devuelve un código de salida agregado que falla si cualquier suite falla. Las suites contempladas son cuatro: **backend**, **worker**, **frontend** y **E2E**. El alcance es exclusivamente la orquestación de la ejecución de pruebas existentes; no se añaden, eliminan ni modifican pruebas, ni se cambia el comportamiento del código de producción.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Ejecutar todas las suites con un solo comando apto para CI (Priority: P1)

Como responsable de calidad o de la pipeline de CI, quiero ejecutar todas las suites de pruebas (backend, worker, frontend y E2E) mediante un único comando que devuelva un código de salida agregado, de modo que la verificación completa del proyecto sea reproducible y pueda usarse como puerta ("gate") de integración sin depender de ejecutar varios comandos a mano.

**Why this priority**: Es el núcleo de la feature y el habilitador directo del CI Gate del Principio IV. Sin un comando único con código de salida agregado, no hay forma fiable de bloquear merges que rompan alguna suite. Entrega valor por sí solo aunque el reporte sea mínimo.

**Independent Test**: Se invoca el comando único en un entorno con todas las suites disponibles; se verifica que ejecuta backend, worker, frontend y E2E, y que el proceso termina con código de salida distinto de cero si alguna suite falla y con cero si todas pasan.

**Acceptance Scenarios**:

1. **Given** un proyecto con todas las suites en verde, **When** se ejecuta el comando único, **Then** corren las cuatro suites (backend, worker, frontend, E2E) y el comando termina con código de salida cero (éxito).
2. **Given** un proyecto en el que la suite de backend o la del worker falla, **When** se ejecuta el comando único, **Then** el comando termina con código de salida distinto de cero (fallo).
3. **Given** un proyecto en el que la suite de frontend o la E2E falla, **When** se ejecuta el comando único, **Then** el comando termina con código de salida distinto de cero (fallo).
4. **Given** la ejecución del comando único, **When** termina, **Then** queda constancia de que se intentaron ejecutar las cuatro suites (ninguna se omite silenciosamente, incluida la del worker).

---

### User Story 2 - Reporte consolidado del resultado por suite (Priority: P2)

Como responsable de calidad, quiero ver al final de la ejecución un resumen claro que indique PASS o FAIL para cada suite (backend, worker, frontend, E2E), de modo que pueda identificar de un vistazo qué componente o nivel de pruebas falló sin tener que revisar todo el log.

**Why this priority**: Mejora sustancialmente la utilidad del comando único al hacer accionable su salida, pero depende de que la ejecución (P1) exista. Acelera el diagnóstico tanto en local como en CI.

**Independent Test**: Tras ejecutar el comando único con al menos una suite fallando, se verifica que el resumen final lista las cuatro suites con su veredicto individual (PASS/FAIL) y que el veredicto coincide con el resultado real de cada suite.

**Acceptance Scenarios**:

1. **Given** una ejecución del comando único, **When** termina, **Then** se muestra un resumen final con una línea por suite (backend, worker, frontend, E2E) indicando PASS o FAIL.
2. **Given** una ejecución en la que solo una suite falla, **When** se consulta el resumen, **Then** esa suite figura como FAIL y las demás como PASS, de forma coherente con el código de salida agregado.
3. **Given** una ejecución en la que todas las suites pasan, **When** se consulta el resumen, **Then** las cuatro suites figuran como PASS.

---

### User Story 3 - Ejecución multiplataforma (Windows/Linux) (Priority: P3)

Como integrante del equipo que trabaja en Windows y como pipeline de CI que corre en Linux, quiero que el mismo comando único funcione en ambos sistemas operativos, de modo que la verificación completa sea idéntica en el entorno de desarrollo local y en CI sin scripts distintos por plataforma.

**Why this priority**: Garantiza paridad entre el entorno local (Windows, según el contexto del equipo) y CI (Linux), evitando divergencias. Es valioso pero secundario respecto a tener el comando funcionando y reportando.

**Independent Test**: Se ejecuta el mismo comando único en Windows y en Linux sobre el mismo proyecto y se verifica que en ambos casos orquesta las cuatro suites y produce el mismo tipo de resultado consolidado y código de salida.

**Acceptance Scenarios**:

1. **Given** un entorno Windows, **When** se ejecuta el comando único, **Then** corre las cuatro suites y produce el resumen consolidado y el código de salida agregado.
2. **Given** un entorno Linux, **When** se ejecuta el mismo comando único, **Then** corre las cuatro suites y produce el resumen consolidado y el código de salida agregado de forma equivalente a Windows.
3. **Given** ambos entornos, **When** se compara la forma de invocación, **Then** es el mismo comando conceptual (sin requerir comandos distintos por plataforma).

---

### Edge Cases

- ¿Qué ocurre si una suite falla a la mitad? → El comando debe registrar esa suite como FAIL, continuar con las demás suites para poder reportarlas todas, y reflejar el fallo en el código de salida agregado.
- ¿Qué pasa si una herramienta de pruebas no está instalada o sus dependencias faltan (p. ej. navegadores de Playwright)? → Se trata como fallo de esa suite (FAIL), no como éxito; el resumen lo indica y el código de salida es distinto de cero.
- ¿Cómo se distingue "todas pasan" de "alguna no llegó a ejecutarse"? → El resumen debe mostrar explícitamente el estado de las cuatro suites; una suite que no pudo ejecutarse no cuenta como PASS.
- ¿Qué pasa si las pruebas de backend y de worker comparten herramienta (`dotnet test`) pero residen en proyectos distintos? → Cada una se ejecuta y reporta como suite independiente; el fallo de una no debe enmascarar ni omitir la ejecución de la otra.
- ¿Qué sucede si fallan dos o más suites a la vez? → Todas las que fallan figuran como FAIL en el resumen y el código de salida agregado es distinto de cero.
- ¿El comando es no interactivo? → Sí; no debe requerir entrada del usuario ni quedarse esperando, para ser apto para CI.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: El sistema DEBE proporcionar un único punto de entrada (un solo comando) que ejecute todas las suites de pruebas del proyecto: backend, worker, frontend y E2E.
- **FR-002**: El comando único DEBE ejecutar la suite de backend (pruebas unitarias y de integración de .NET), la suite del worker de transiciones (pruebas de .NET en su propio proyecto de pruebas), la suite de frontend (pruebas de componentes con el ejecutor de Vitest en modo no interactivo) y la suite E2E (pruebas de Playwright).
- **FR-003**: El comando único DEBE devolver un código de salida agregado: cero si y solo si todas las suites pasan; distinto de cero si cualquier suite falla.
- **FR-004**: El comando único DEBE fallar (código de salida distinto de cero) si **cualquiera** de las suites falla, de forma que sea apto para usarse como puerta de CI conforme al Principio IV.
- **FR-005**: El comando único DEBE producir un reporte consolidado al final de la ejecución que indique el veredicto individual (PASS/FAIL) de cada una de las cuatro suites (backend, worker, frontend, E2E).
- **FR-006**: El veredicto de cada suite en el reporte consolidado DEBE ser coherente con el código de salida agregado (si alguna figura como FAIL, el código de salida es distinto de cero; si todas figuran como PASS, es cero).
- **FR-007**: El comando único DEBE poder ejecutarse de forma equivalente en Windows y en Linux, sin requerir comandos distintos por plataforma.
- **FR-008**: El comando único DEBE ser no interactivo y determinista en su forma de invocación, apto para ejecutarse en un entorno de CI sin intervención manual.
- **FR-009**: El comando único DEBE intentar ejecutar todas las suites y reflejar en el reporte el estado real de cada una; ninguna suite debe omitirse silenciosamente, incluida la del worker por residir en un proyecto separado.
- **FR-010**: Una suite que no pueda ejecutarse (herramienta o dependencia ausente, error de arranque) DEBE contabilizarse como FAIL, nunca como PASS.
- **FR-011**: La feature NO DEBE añadir, eliminar ni modificar pruebas existentes ni alterar el comportamiento del código de producción; su alcance es exclusivamente orquestar la ejecución de las suites ya existentes.
- **FR-012**: La documentación de la feature (spec, plan, tareas, instrucciones de ejecución) DEBE estar en español, conforme al Principio III.

### Key Entities *(include if feature involves data)*

- **Suite de pruebas**: Conjunto de pruebas que se ejecuta y produce un veredicto propio (PASS/FAIL). Las cuatro suites del proyecto son backend, worker, frontend y E2E. Backend y worker comparten herramienta de ejecución (.NET) pero son proyectos de pruebas independientes y se reportan por separado.
- **Resultado de suite**: Veredicto individual (PASS/FAIL) de una suite tras su ejecución, base del reporte consolidado.
- **Reporte consolidado**: Resumen final que agrega los resultados de las cuatro suites en una vista única (una línea por suite) más el veredicto global.
- **Código de salida agregado**: Valor único de terminación del comando que resume el éxito (cero) o fallo (distinto de cero) global de la ejecución, consumido por CI.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Una sola invocación ejecuta las cuatro suites (backend, worker, frontend, E2E); no se requiere ningún comando adicional para completar la verificación del proyecto.
- **SC-002**: Cuando todas las suites pasan, el comando termina con código de salida cero en el 100% de las ejecuciones.
- **SC-003**: Cuando al menos una suite falla, el comando termina con código de salida distinto de cero en el 100% de las ejecuciones.
- **SC-004**: Al finalizar, el resumen consolidado muestra el veredicto (PASS/FAIL) de las cuatro suites, y dicho veredicto coincide con el resultado real de cada suite en el 100% de los casos.
- **SC-005**: El mismo comando produce resultados equivalentes en Windows y en Linux (mismas suites ejecutadas, mismo tipo de reporte y misma semántica de código de salida).
- **SC-006**: La ejecución es completamente no interactiva: no solicita entrada del usuario en ningún momento, lo que permite integrarla en CI sin intervención.
- **SC-007**: No se introduce ningún cambio en el comportamiento del código de producción ni en el contenido de las pruebas existentes (solo orquestación y configuración de ejecución).

## Assumptions

- Todas las suites ya existen y son ejecutables de forma individual con sus herramientas actuales (`dotnet test` para backend y para worker —cada uno en su proyecto de pruebas—, `vitest run` para frontend, `playwright test` para E2E); esta feature solo las orquesta, no las crea ni modifica.
- Las pruebas de backend y de worker son suites independientes que comparten herramienta (.NET); se ejecutan y reportan por separado para dar visibilidad de cuál componente falla.
- El comando único ejecuta **todas** las suites antes de terminar (no se detiene en la primera que falla), de modo que el reporte consolidado pueda mostrar el estado real de las cuatro; el veredicto global se determina al final agregando los resultados. (Decisión por defecto para maximizar la utilidad del reporte; podría revisarse en `/speckit-plan` si se prefiere modo "fail-fast" para CI.)
- Las precondiciones de cada suite (servicios levantados, navegadores de Playwright instalados, base de datos disponible para E2E) se preparan fuera del alcance estricto de esta feature o se documentan como requisito de ejecución; una precondición no satisfecha se refleja como FAIL de la suite afectada.
- El entorno de ejecución dispone de las herramientas necesarias (SDK de .NET y entorno de Node/gestor de paquetes) tanto en local como en CI.
- "Multiplataforma" se interpreta como Windows (entorno local del equipo) y Linux (entorno de CI); macOS no se considera explícitamente en esta feature aunque no se excluye.
- El proyecto actualmente no dispone de un punto de entrada de paquete único en la raíz; la elección del mecanismo concreto de orquestación (script de paquete, script de shell multiplataforma, tarea de build u otro) se decidirá en la fase de plan, sin condicionar esta spec.
