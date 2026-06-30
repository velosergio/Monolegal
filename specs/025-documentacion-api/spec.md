# Feature Specification: Documentación de API y del Proyecto

**Feature Branch**: `025-documentacion-api`

**Created**: 2026-06-30

**Status**: Activo

**Input**: Roadmap Spec 6.1 — "API Documentation": dado el proyecto finalizado, cuando alguien lee la documentación, entonces incluye: panorama de arquitectura (architecture overview), diagramas de relación de entidades (ERD), referencia de endpoints de la API, instrucciones de configuración (setup), guía de despliegue (deployment), colección de Postman y Swagger UI accesible mediante un botón en el sidebar.

## Contexto

El proyecto Monolegal está finalizado en cuanto a funcionalidad: backend ASP.NET Core 10 con Minimal APIs (facturas, clientes, transiciones de estado, envíos), worker de transiciones, frontend React 19 y una batería completa de pruebas en los tres niveles de la pirámide más un test runner unificado (Spec 5.5 / 024). La documentación de Swagger/OpenAPI a nivel de endpoints ya se introdujo en la Spec 010, y el frontend cuenta con un sidebar colapsable con rutas como `/facturas` y `/configuracion`.

Sin embargo, no existe una **documentación consolidada del proyecto** que permita a una persona nueva (desarrollador que se incorpora, integrador externo, o el propio equipo en el futuro) entender la arquitectura, el modelo de datos, cómo poner el proyecto en marcha localmente, cómo desplegarlo y cómo consumir la API sin leer el código fuente. Además, aunque Swagger UI existe a nivel de backend, no es **descubrible** desde la interfaz: quien usa el panel de administración no tiene una forma directa de llegar a la documentación interactiva de la API.

Esta feature establece el conjunto de **artefactos de documentación del proyecto finalizado** y los hace **accesibles**: documentación escrita (arquitectura, modelo de entidades, referencia de endpoints, configuración y despliegue), una colección de Postman lista para importar, y un punto de acceso visible a Swagger UI desde el sidebar del frontend. El alcance es exclusivamente documentación y su descubribilidad; no se modifica el comportamiento de los endpoints, el modelo de datos ni la lógica de negocio existentes.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Entender y poner en marcha el proyecto con la documentación escrita (Priority: P1)

Como desarrollador que se incorpora al proyecto (o como integrante del equipo retomando el proyecto tiempo después), quiero leer una documentación consolidada que explique la arquitectura, el modelo de datos, los endpoints disponibles, cómo configurar el entorno local y cómo desplegar, de modo que pueda entender el sistema y ponerlo en marcha sin tener que leer el código fuente ni preguntar al autor original.

**Why this priority**: Es el núcleo de la feature. Sin la documentación escrita, los demás artefactos (Postman, botón a Swagger) carecen de contexto. Entrega valor por sí sola: con solo este story, una persona nueva puede comprender y arrancar el proyecto. Cumple directamente el requisito constitucional de "Diagramas de arquitectura en README" (Principio VI).

**Independent Test**: Una persona ajena al desarrollo sigue únicamente la documentación escrita para (a) describir la arquitectura por capas y componentes, (b) identificar las entidades principales y sus relaciones, (c) localizar los endpoints y su propósito, (d) levantar el proyecto en local, y (e) entender los pasos de despliegue. Se considera superado si logra arrancar el entorno local guiándose solo por el documento.

**Acceptance Scenarios**:

1. **Given** la documentación del proyecto, **When** se busca el panorama de arquitectura, **Then** se encuentra una descripción de las capas (Domain/Application/Infrastructure/Api), los componentes (backend, worker, frontend, base de datos) y cómo se relacionan, incluyendo al menos un diagrama.
2. **Given** la documentación del proyecto, **When** se busca el modelo de datos, **Then** se encuentra un diagrama de relación de entidades (ERD) con las entidades principales (p. ej. Factura, Cliente) y sus relaciones y atributos clave.
3. **Given** la documentación del proyecto, **When** se busca cómo consumir la API, **Then** se encuentra una referencia de los endpoints disponibles con su método, ruta, propósito y forma de petición/respuesta.
4. **Given** un entorno limpio, **When** se siguen las instrucciones de configuración (setup), **Then** es posible levantar backend, worker, frontend y base de datos en local con los pasos indicados.
5. **Given** la documentación del proyecto, **When** se busca cómo desplegar, **Then** se encuentra una guía de despliegue con los pasos y requisitos para llevar el proyecto a un entorno productivo.

---

### User Story 2 - Acceder a Swagger UI desde el sidebar (Priority: P2)

Como administrador que usa el panel, quiero un botón o enlace visible en el sidebar que me lleve a Swagger UI, de modo que pueda explorar y probar la API de forma interactiva sin tener que conocer ni teclear manualmente la URL del backend.

**Why this priority**: Hace descubrible la documentación interactiva ya existente directamente desde la interfaz, mejorando notablemente la experiencia de quien consume la API. Depende de que Swagger UI ya esté disponible (Spec 010), por eso es secundario respecto a la documentación escrita.

**Independent Test**: Con el frontend en ejecución, se localiza en el sidebar un elemento (botón/enlace) etiquetado para documentación de API; al activarlo, se abre Swagger UI funcional mostrando los endpoints del backend.

**Acceptance Scenarios**:

1. **Given** el panel de administración cargado, **When** se observa el sidebar, **Then** existe un elemento claramente identificado que da acceso a la documentación de la API (Swagger UI).
2. **Given** el sidebar visible, **When** se activa el elemento de documentación de API, **Then** se abre Swagger UI mostrando la especificación interactiva de los endpoints del backend.
3. **Given** el sidebar colapsado o expandido, **When** se busca el acceso a Swagger UI, **Then** el acceso sigue siendo alcanzable de forma coherente con el resto de elementos del sidebar.

---

### User Story 3 - Probar la API con una colección de Postman (Priority: P3)

Como integrador o desarrollador, quiero una colección de Postman lista para importar que contenga las peticiones a los endpoints de la API, de modo que pueda probar la API rápidamente sin construir cada petición a mano.

**Why this priority**: Acelera la prueba e integración con la API, pero es complementario: la referencia escrita (P1) y Swagger UI (P2) ya permiten conocer y probar la API. Aporta comodidad adicional para flujos de prueba repetibles.

**Independent Test**: Se importa el archivo de colección de Postman provisto y se verifica que contiene peticiones para los endpoints principales de la API, organizadas y con la configuración (URL base, método, cuerpo de ejemplo) necesaria para ejecutarlas contra una instancia local.

**Acceptance Scenarios**:

1. **Given** el repositorio del proyecto, **When** se busca la colección de Postman, **Then** existe un archivo de colección importable que cubre los endpoints principales de la API.
2. **Given** la colección importada en Postman, **When** se revisa su contenido, **Then** las peticiones incluyen método, ruta y, cuando aplica, cuerpo de ejemplo, agrupadas de forma comprensible.
3. **Given** una instancia local del backend en ejecución, **When** se ejecuta una petición de la colección configurada con la URL base local, **Then** la petición alcanza el endpoint correspondiente y devuelve una respuesta válida.

---

### Edge Cases

- ¿Qué ocurre si la documentación de endpoints queda desactualizada respecto al backend? → La referencia de endpoints y la colección de Postman deben reflejar el estado real de la API en el momento de finalización; se documenta cómo regenerarlas/actualizarlas a partir de la especificación OpenAPI para evitar divergencias.
- ¿Cómo se accede a Swagger UI si el backend no está en ejecución? → El botón del sidebar apunta a la URL de Swagger del backend; si el backend está caído, Swagger no carga. La documentación debe indicar que requiere el backend activo (la URL base es configurable por entorno).
- ¿La colección de Postman debe traer secretos o credenciales reales? → No; usa variables de entorno/placeholders (URL base, token) sin credenciales embebidas, conforme al principio de no incluir secretos.
- ¿Qué pasa si la URL del backend difiere entre local y producción? → Tanto el acceso a Swagger desde el sidebar como la colección de Postman deben permitir configurar la URL base por entorno y no asumir un host fijo.
- ¿La documentación está en español? → Sí; toda la documentación de especificaciones y la guía del proyecto se redactan en español conforme al Principio III (textos de la API/OpenAPI pueden conservar términos técnicos).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: La documentación del proyecto DEBE incluir un panorama de arquitectura (architecture overview) que describa las capas del backend (Domain/Application/Infrastructure/Api), los componentes del sistema (backend, worker, frontend, base de datos) y sus relaciones, acompañado de al menos un diagrama.
- **FR-002**: La documentación DEBE incluir un diagrama de relación de entidades (ERD) con las entidades principales del dominio (p. ej. Factura, Cliente), sus atributos clave y las relaciones entre ellas.
- **FR-003**: La documentación DEBE incluir una referencia de los endpoints de la API que liste, por cada endpoint, su método, ruta, propósito y forma de petición/respuesta (parámetros y cuerpos relevantes).
- **FR-004**: La documentación DEBE incluir instrucciones de configuración (setup) que permitan a una persona nueva levantar el proyecto en local (backend, worker, frontend y base de datos), incluyendo prerrequisitos y variables de entorno necesarias.
- **FR-005**: La documentación DEBE incluir una guía de despliegue (deployment) con los pasos y requisitos para llevar el proyecto a un entorno productivo.
- **FR-006**: El proyecto DEBE incluir una colección de Postman importable que cubra los endpoints principales de la API, con método, ruta y cuerpos de ejemplo cuando apliquen, y sin credenciales o secretos embebidos.
- **FR-007**: El frontend DEBE exponer en el sidebar un elemento (botón/enlace) claramente identificado que dé acceso a Swagger UI de la API.
- **FR-008**: Al activar el elemento del sidebar, el sistema DEBE abrir Swagger UI mostrando la especificación interactiva de los endpoints del backend.
- **FR-009**: El acceso a Swagger UI desde el sidebar y la colección de Postman DEBEN permitir configurar la URL base del backend por entorno (local/producción), sin asumir un host fijo ni incluir secretos.
- **FR-010**: La documentación DEBE indicar cómo mantener actualizadas la referencia de endpoints y la colección de Postman respecto a la especificación OpenAPI del backend, para evitar divergencias con la API real.
- **FR-011**: Toda la documentación de la feature y del proyecto DEBE estar redactada en español, conforme al Principio III (se permiten términos técnicos y el contenido propio de la especificación OpenAPI en su forma original).
- **FR-012**: La feature NO DEBE modificar el comportamiento de los endpoints, el modelo de datos ni la lógica de negocio existentes; su alcance es documentar el sistema y hacer descubrible la documentación de la API.

### Key Entities *(include if feature involves data)*

- **Documento de arquitectura**: Artefacto escrito que describe capas, componentes y sus relaciones, con al menos un diagrama; base para comprender el sistema.
- **Diagrama de entidades (ERD)**: Representación de las entidades del dominio (Factura, Cliente, etc.), sus atributos clave y relaciones.
- **Referencia de endpoints**: Listado estructurado de los endpoints de la API (método, ruta, propósito, petición/respuesta), derivable de la especificación OpenAPI.
- **Guía de configuración y despliegue**: Conjunto de instrucciones para levantar el proyecto en local y desplegarlo en producción, incluyendo prerrequisitos y variables de entorno.
- **Colección de Postman**: Archivo importable con las peticiones a los endpoints principales, parametrizado por URL base y sin secretos.
- **Acceso a Swagger UI**: Elemento del sidebar del frontend que enlaza a la documentación interactiva (Swagger UI) del backend.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Una persona ajena al desarrollo puede levantar el proyecto en local siguiendo únicamente las instrucciones de configuración, sin asistencia adicional.
- **SC-002**: La documentación incluye los seis artefactos requeridos —architecture overview, ERD, referencia de endpoints, instrucciones de setup, guía de despliegue y colección de Postman— verificables por inspección.
- **SC-003**: El 100% de los endpoints expuestos por el backend está representado en la referencia de endpoints y disponible en Swagger UI.
- **SC-004**: Desde el sidebar del frontend se llega a Swagger UI en una sola acción (un clic), y Swagger UI carga la especificación de los endpoints cuando el backend está activo.
- **SC-005**: La colección de Postman se importa sin errores y al menos las peticiones a los endpoints principales se ejecutan correctamente contra una instancia local configurada por variable de URL base.
- **SC-006**: La documentación no contiene credenciales ni secretos embebidos (verificable por inspección).
- **SC-007**: No se introduce ningún cambio en el comportamiento de los endpoints, el modelo de datos ni la lógica de negocio existentes (solo documentación y un acceso de navegación en el sidebar).

## Assumptions

- Swagger UI / OpenAPI ya está disponible en el backend (Spec 010); esta feature lo hace descubrible desde el sidebar y lo referencia, sin reimplementarlo.
- La documentación escrita se materializa como documentos versionados en el repositorio (por ejemplo, README y/o una carpeta de documentación con diagramas en formato apto para control de versiones); el formato concreto se decide en la fase de plan.
- Los diagramas (arquitectura y ERD) pueden expresarse en un formato basado en texto y versionable (p. ej. diagramas como código) o como imágenes incluidas en el repositorio; la elección concreta se hace en el plan.
- La colección de Postman se entrega como archivo exportado importable; puede generarse a partir de la especificación OpenAPI del backend para mantener coherencia con la API real.
- El botón del sidebar para Swagger UI abre la URL de Swagger del backend, cuya base es configurable por entorno; se asume que el frontend ya conoce o puede configurar la URL base del backend.
- "Proyecto finalizado" implica que los endpoints, entidades y componentes a documentar son los existentes al momento de esta feature; la documentación refleja ese estado.
- El público objetivo de la documentación incluye desarrolladores que se incorporan, el equipo a futuro e integradores externos de la API; no se asume audiencia no técnica para la documentación de endpoints.
