# Especificación de Feature: Dependencias Backend

**Rama Feature**: `002-backend-dependencies`

**Creado**: 2026-06-24

**Estado**: Activo

**Entrada**: Fase 0.2 - Dependencias Backend del roadmap.md

## Escenarios de Usuario & Testing *(obligatorio)*

### Historia de Usuario 1 - Persistencia de Datos Disponible (Prioridad: P1)

El desarrollador dispone del cliente de base de datos documental instalado y referenciado en la capa de Infraestructura, permitiendo implementar repositorios que leen y escriben documentos sin volver a tocar la configuración de dependencias.

**Por qué esta prioridad**: Sin el driver de la base de datos no es posible persistir ninguna entidad de negocio. Es la dependencia que habilita el resto de las features funcionales (facturas, clientes, transiciones de estado).

**Test Independiente**:
- Verificar que el paquete del driver de base de datos documental está referenciado en la capa de Infraestructura.
- Verificar que la solución compila con la dependencia resuelta.
- Verificar que es posible instanciar un cliente de base de datos en un test de la capa de Infraestructura sin errores de resolución de tipos.

**Escenarios de Aceptación**:

1. **Dado** la capa de Infraestructura sin el driver de base de datos, **Cuando** se completa la instalación de dependencias, **Entonces** el paquete del driver documental queda referenciado y restaurado.
2. **Dado** el driver instalado, **Cuando** se compila la solución, **Entonces** la compilación es exitosa con cero errores de dependencias no resueltas.
3. **Dado** el driver disponible, **Cuando** un test referencia los tipos del cliente de base de datos, **Entonces** los tipos resuelven correctamente sin paquetes faltantes.

---

### Historia de Usuario 2 - Validación de Entradas Disponible (Prioridad: P1)

El desarrollador dispone de la librería de validación instalada y referenciada en la capa de Aplicación, permitiendo definir reglas de validación declarativas para todas las entradas antes de procesarlas.

**Por qué esta prioridad**: La constitución exige validación en todos los inputs de la API. Sin la librería disponible, no pueden escribirse validadores ni los tests que los cubren.

**Test Independiente**:
- Verificar que el paquete de validación está referenciado en la capa de Aplicación.
- Verificar que la solución compila con la dependencia resuelta.
- Verificar que es posible declarar una clase validadora derivada del tipo base de la librería.

**Escenarios de Aceptación**:

1. **Dado** la capa de Aplicación sin la librería de validación, **Cuando** se completa la instalación de dependencias, **Entonces** el paquete de validación queda referenciado y restaurado.
2. **Dado** la librería de validación instalada, **Cuando** se define un validador para un objeto de prueba, **Entonces** el validador compila y ejecuta reglas sin errores de tipos.

---

### Historia de Usuario 3 - Logging Estructurado Disponible (Prioridad: P1)

El desarrollador dispone de la librería de logging estructurado instalada y referenciada en las capas que emiten logs (API e Infraestructura), permitiendo registrar acciones significativas en formato estructurado desde el primer endpoint.

**Por qué esta prioridad**: La constitución exige logging estructurado JSON para toda acción significativa. Es transversal y debe estar disponible antes de implementar cualquier flujo observable.

**Test Independiente**:
- Verificar que el paquete de logging estructurado está referenciado en las capas correspondientes.
- Verificar que la solución compila con la dependencia resuelta.
- Verificar que es posible configurar un logger estructurado en el arranque de la API.

**Escenarios de Aceptación**:

1. **Dado** las capas sin la librería de logging, **Cuando** se completa la instalación de dependencias, **Entonces** el paquete de logging estructurado queda referenciado y restaurado.
2. **Dado** la librería de logging instalada, **Cuando** se inicializa el logger en el arranque de la API, **Entonces** el logger se configura sin errores de tipos faltantes.

---

### Historia de Usuario 4 - Framework de API con APIs Mínimas Disponible (Prioridad: P1)

El desarrollador dispone del framework web con soporte de APIs mínimas en la capa de API, permitiendo declarar endpoints sin el modelo completo de controladores MVC.

**Por qué esta prioridad**: La constitución exige Minimal APIs (sin MVC completo). Es la dependencia que habilita exponer cualquier endpoint HTTP.

**Test Independiente**:
- Verificar que la capa de API tiene el SDK web y el soporte de APIs mínimas disponibles.
- Verificar que la aplicación web arranca y registra un endpoint mínimo de prueba.

**Escenarios de Aceptación**:

1. **Dado** la capa de API sin el framework web, **Cuando** se completa la instalación de dependencias, **Entonces** el SDK web con APIs mínimas queda disponible.
2. **Dado** el framework web disponible, **Cuando** se declara un endpoint mínimo y arranca la aplicación, **Entonces** el endpoint responde sin errores de dependencias.

---

### Historia de Usuario 5 - Framework de Pruebas Disponible (Prioridad: P1)

El desarrollador dispone del framework de pruebas unitarias y la librería de aserciones fluidas instalados en el proyecto de Tests, permitiendo escribir y ejecutar pruebas siguiendo el ciclo Red-Green-Refactor desde el inicio.

**Por qué esta prioridad**: La constitución exige desarrollo Test-First. Sin el framework de pruebas y las aserciones, no puede escribirse ni ejecutarse ninguna prueba, bloqueando todo el flujo de desarrollo.

**Test Independiente**:
- Verificar que el framework de pruebas y la librería de aserciones están referenciados en el proyecto de Tests.
- Verificar que el runner de pruebas descubre y ejecuta una prueba trivial.
- Verificar que una aserción fluida ejecuta correctamente.

**Escenarios de Aceptación**:

1. **Dado** el proyecto de Tests sin framework de pruebas, **Cuando** se completa la instalación de dependencias, **Entonces** el framework de pruebas y la librería de aserciones quedan referenciados y restaurados.
2. **Dado** el framework de pruebas instalado, **Cuando** se ejecuta el comando de pruebas con una prueba trivial, **Entonces** el runner descubre y ejecuta la prueba con resultado verde.
3. **Dado** la librería de aserciones disponible, **Cuando** una prueba usa una aserción fluida, **Entonces** la aserción compila y evalúa la condición correctamente.

---

### Casos Límite

- ¿Qué pasa si una dependencia se referencia en una capa que viola la dirección de dependencias de Arquitectura Limpia (ej: el driver de base de datos referenciado en Domain)? → La instalación debe ubicar cada dependencia solo en la capa permitida; Domain permanece sin dependencias de infraestructura.
- ¿Qué pasa si dos paquetes resuelven a versiones incompatibles del mismo runtime de la plataforma? → La restauración debe fallar de forma explícita y reportar el conflicto de versiones, sin dejar la solución en estado parcialmente restaurado.
- ¿Qué pasa si no hay conectividad con el repositorio de paquetes durante la restauración? → La instalación debe reportar el fallo de restauración con un mensaje claro, sin marcar la fase como completada.

---

## Requisitos *(obligatorio)*

### Requisitos Funcionales

- **FR-001**: La capa de API DEBE tener disponible el framework web de la plataforma backend en su versión mayor objetivo (10) con soporte de APIs mínimas.
- **FR-002**: La capa de Infraestructura DEBE tener referenciado el driver de la base de datos documental para persistencia de documentos.
- **FR-003**: La capa de Aplicación DEBE tener referenciada la librería de validación declarativa de entradas.
- **FR-004**: Las capas que emiten logs (API e Infraestructura) DEBEN tener referenciada la librería de logging estructurado.
- **FR-005**: El proyecto de Tests DEBE tener referenciado el framework de pruebas unitarias.
- **FR-006**: El proyecto de Tests DEBE tener referenciada la librería de aserciones fluidas.
- **FR-007**: Cada dependencia DEBE ubicarse únicamente en la(s) capa(s) permitida(s) por la dirección de dependencias de Arquitectura Limpia, manteniendo la capa Domain libre de dependencias de infraestructura.
- **FR-008**: La solución completa DEBE compilar con cero errores tras la instalación de todas las dependencias.
- **FR-009**: La restauración de paquetes DEBE completarse sin conflictos de versión no resueltos; cualquier conflicto DEBE reportarse explícitamente y bloquear la finalización.
- **FR-010**: La versión mayor de cada dependencia DEBE ser coherente con el stack tecnológico definido en la constitución (framework web 10, driver documental, validación, logging estructurado, framework de pruebas y aserciones fluidas).

### Entidades Clave *(N/A para esta especificación de dependencias de infraestructura)*

---

## Criterios de Éxito *(obligatorio)*

### Resultados Medibles

- **SC-001**: El 100% de las seis dependencias objetivo (framework web 10, driver documental, validación, logging estructurado, framework de pruebas, aserciones fluidas) están referenciadas en sus capas correspondientes.
- **SC-002**: La solución completa compila con cero errores en la primera ejecución tras la instalación.
- **SC-003**: La restauración de paquetes finaliza sin advertencias de conflicto de versión.
- **SC-004**: El runner de pruebas descubre y ejecuta al menos una prueba de verificación con resultado exitoso.
- **SC-005**: La capa Domain permanece sin ninguna dependencia de infraestructura tras la instalación (cero referencias prohibidas).
- **SC-006**: Un desarrollador puede clonar el repositorio y restaurar todas las dependencias backend sin pasos manuales adicionales más allá del comando de restauración estándar.

---

## Suposiciones

- **Prerequisito**: La estructura de proyectos backend de la Fase 0.1 (`001-project-setup`) ya existe con las capas Domain/Application/Infrastructure/Api y el proyecto de Tests.
- **Plataforma**: El SDK de la plataforma backend en su versión mayor objetivo (10) está instalado localmente y disponible para restaurar y compilar.
- **Gestor de paquetes**: Las dependencias se obtienen del repositorio de paquetes estándar de la plataforma backend; existe conectividad de red durante la restauración.
- **Ubicación por capa**: Cada dependencia se referencia en la capa que respeta la dirección de dependencias de Arquitectura Limpia (driver y logging en Infraestructura, validación en Aplicación, framework web y logging en API, framework de pruebas y aserciones en Tests).
- **Alcance**: Esta fase instala y referencia dependencias; la configuración funcional concreta (cadena de conexión, sinks de logging, registro de validadores en el contenedor DI) corresponde a fases posteriores (0.4 en adelante).
- **Versiones**: Se asumen las versiones estables mayores compatibles con el stack de la constitución; pins exactos de versión menor se deciden en la fase de planificación.
