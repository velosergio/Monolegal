# Especificación de Feature: Dependencias Frontend

**Rama Feature**: `003-frontend-dependencies`

**Creado**: 2026-06-24

**Estado**: Draft

**Entrada**: Fase 0.3 - Dependencias Frontend del roadmap.md

## Escenarios de Usuario & Testing *(obligatorio)*

### Historia de Usuario 1 - Base de UI con React y TypeScript Disponible (Prioridad: P1)

El desarrollador dispone de la librería de UI (React 19+) y del lenguaje tipado en modo estricto (TypeScript) instalados y configurados sobre el proyecto Vite, permitiendo escribir componentes tipados sin volver a tocar la configuración de dependencias.

**Por qué esta prioridad**: Sin la librería de UI y el lenguaje tipado no es posible construir ningún componente del frontend. Es la base sobre la que se apoyan todas las demás dependencias y features funcionales.

**Test Independiente**:
- Verificar que la librería de UI en su versión mayor objetivo (19+) está referenciada en el proyecto frontend.
- Verificar que el lenguaje tipado está configurado en modo estricto (sin `any` implícito permitido).
- Verificar que un componente trivial tipado compila sin errores de tipos.

**Escenarios de Aceptación**:

1. **Dado** el proyecto frontend sin la librería de UI, **Cuando** se completa la instalación de dependencias, **Entonces** la librería de UI en versión mayor 19+ queda referenciada e instalada.
2. **Dado** el lenguaje tipado instalado, **Cuando** se inspecciona la configuración de compilación, **Entonces** el modo estricto está activado sin excepciones.
3. **Dado** la base de UI disponible, **Cuando** se compila un componente tipado trivial, **Entonces** la compilación es exitosa con cero errores de tipos.

---

### Historia de Usuario 2 - Herramienta de Build y Dev Server Disponible (Prioridad: P1)

El desarrollador dispone de la herramienta de build/dev (Vite) operativa, permitiendo levantar el servidor de desarrollo con recarga en caliente y generar un build de producción sin configuración adicional de bundler.

**Por qué esta prioridad**: La constitución exige Vite (sin webpack). Sin la herramienta de build, no es posible servir la aplicación en desarrollo ni generar artefactos de producción, bloqueando toda iteración visual.

**Test Independiente**:
- Verificar que la herramienta de build está referenciada en el proyecto frontend.
- Verificar que el servidor de desarrollo arranca y sirve la aplicación.
- Verificar que el comando de build genera artefactos de producción sin errores.

**Escenarios de Aceptación**:

1. **Dado** el proyecto frontend, **Cuando** se ejecuta el comando del servidor de desarrollo, **Entonces** la aplicación se sirve localmente sin errores de dependencias.
2. **Dado** la herramienta de build disponible, **Cuando** se ejecuta el comando de build de producción, **Entonces** se generan los artefactos sin errores de dependencias no resueltas.

---

### Historia de Usuario 3 - Sistema de Componentes y Estilos Disponible (Prioridad: P1)

El desarrollador dispone del sistema de componentes (shadcn/ui) integrado, permitiendo añadir y componer componentes accesibles y con dark mode desde el primer día.

**Por qué esta prioridad**: La constitución exige componentes shadcn/ui con dark mode built-in desde día uno y accesibilidad WCAG A. Es la dependencia que habilita construir cualquier interfaz consistente y accesible.

**Test Independiente**:
- Verificar que el sistema de componentes está inicializado en el proyecto frontend.
- Verificar que es posible añadir un componente del sistema y renderizarlo.
- Verificar que el soporte de dark mode está disponible desde la configuración inicial.

**Escenarios de Aceptación**:

1. **Dado** el proyecto frontend sin el sistema de componentes, **Cuando** se completa la inicialización, **Entonces** el sistema de componentes queda configurado con su archivo de configuración base.
2. **Dado** el sistema de componentes inicializado, **Cuando** se añade y renderiza un componente de prueba, **Entonces** el componente se muestra sin errores y respeta el tema (claro/oscuro).

---

### Historia de Usuario 4 - Gestión de Estado de Servidor Disponible (Prioridad: P1)

El desarrollador dispone de la librería de estado de servidor (TanStack Query) instalada, permitiendo declarar queries y mutaciones con caché, revalidación y manejo de estados de carga sin reinventar la capa de datos.

**Por qué esta prioridad**: La constitución exige TanStack Query para server state. Es la dependencia que habilita consumir la API backend de forma consistente en toda la aplicación.

**Test Independiente**:
- Verificar que la librería de estado de servidor está referenciada en el proyecto frontend.
- Verificar que es posible envolver la aplicación con el proveedor de la librería.
- Verificar que una query declarada compila y ejecuta sin errores de tipos.

**Escenarios de Aceptación**:

1. **Dado** el proyecto frontend sin la librería de estado de servidor, **Cuando** se completa la instalación de dependencias, **Entonces** la librería queda referenciada e instalada.
2. **Dado** la librería de estado de servidor instalada, **Cuando** se configura su proveedor en el árbol de componentes, **Entonces** el proveedor se inicializa sin errores de tipos faltantes.

---

### Historia de Usuario 5 - Librería de Animaciones Disponible (Prioridad: P2)

El desarrollador dispone de la librería de animaciones (Motion) instalada, permitiendo aplicar transiciones y micro-interacciones declarativas a los componentes.

**Por qué esta prioridad**: La constitución exige Motion para animaciones. Aporta calidad de experiencia, pero no bloquea la construcción de funcionalidad base, por lo que se prioriza por debajo de las dependencias estructurales (P1).

**Test Independiente**:
- Verificar que la librería de animaciones está referenciada en el proyecto frontend.
- Verificar que es posible aplicar una animación trivial a un componente sin errores de tipos.

**Escenarios de Aceptación**:

1. **Dado** el proyecto frontend sin la librería de animaciones, **Cuando** se completa la instalación de dependencias, **Entonces** la librería de animaciones queda referenciada e instalada.
2. **Dado** la librería de animaciones instalada, **Cuando** se anima un componente de prueba, **Entonces** el componente compila y renderiza la animación sin errores.

---

### Historia de Usuario 6 - Framework de Pruebas Frontend Disponible (Prioridad: P1)

El desarrollador dispone del framework de pruebas (Vitest) y la librería de pruebas de componentes (Testing Library) instalados, permitiendo escribir y ejecutar pruebas de componentes y hooks siguiendo el ciclo Red-Green-Refactor desde el inicio.

**Por qué esta prioridad**: La constitución exige desarrollo Test-First con Vitest + Testing Library en el frontend. Sin el framework de pruebas y la librería de componentes no puede escribirse ni ejecutarse ninguna prueba, bloqueando todo el flujo de desarrollo.

**Test Independiente**:
- Verificar que el framework de pruebas y la librería de pruebas de componentes están referenciados en el proyecto frontend.
- Verificar que el runner de pruebas descubre y ejecuta una prueba trivial.
- Verificar que es posible renderizar y consultar un componente en una prueba.

**Escenarios de Aceptación**:

1. **Dado** el proyecto frontend sin framework de pruebas, **Cuando** se completa la instalación de dependencias, **Entonces** el framework de pruebas y la librería de pruebas de componentes quedan referenciados e instalados.
2. **Dado** el framework de pruebas instalado, **Cuando** se ejecuta el comando de pruebas con una prueba trivial, **Entonces** el runner descubre y ejecuta la prueba con resultado verde.
3. **Dado** la librería de pruebas de componentes disponible, **Cuando** una prueba renderiza un componente y consulta un elemento, **Entonces** la consulta resuelve correctamente sin dependencias faltantes.

---

### Historia de Usuario 7 - Herramienta de Linting y Formateo Disponible (Prioridad: P1)

El desarrollador dispone de la herramienta de linting/formateo (Biome) instalada y configurada, permitiendo verificar y formatear el código de forma consistente desde el primer commit.

**Por qué esta prioridad**: La constitución exige Biome con cumplimiento 100% sin excepciones. Es un gate de calidad obligatorio; debe estar disponible antes de escribir código para evitar deuda de estilo desde el inicio.

**Test Independiente**:
- Verificar que la herramienta de linting/formateo está referenciada en el proyecto frontend.
- Verificar que existe su archivo de configuración base.
- Verificar que el comando de verificación se ejecuta sobre el código sin errores de herramienta.

**Escenarios de Aceptación**:

1. **Dado** el proyecto frontend sin la herramienta de linting/formateo, **Cuando** se completa la instalación de dependencias, **Entonces** la herramienta queda referenciada con su archivo de configuración base.
2. **Dado** la herramienta de linting/formateo instalada, **Cuando** se ejecuta el comando de verificación, **Entonces** la herramienta analiza el código y reporta resultados sin fallos de ejecución.

---

### Casos Límite

- ¿Qué pasa si una dependencia se referencia sin respetar la organización por feature exigida por Arquitectura Limpia en frontend (ej: lógica de estado de servidor mezclada en componentes de presentación)? → La instalación solo provee las dependencias; la organización por feature con límites claros de hooks/estado se respeta al implementarlas en fases posteriores.
- ¿Qué pasa si dos paquetes resuelven a versiones incompatibles del runtime de la librería de UI? → La instalación debe fallar de forma explícita y reportar el conflicto de versiones, sin dejar el árbol de dependencias en estado parcialmente instalado.
- ¿Qué pasa si no hay conectividad con el registro de paquetes durante la instalación? → La instalación debe reportar el fallo con un mensaje claro, sin marcar la fase como completada.
- ¿Qué pasa si la configuración del lenguaje tipado no activa el modo estricto? → La fase no se considera completa hasta que el modo estricto esté activado, conforme a la constitución (sin `any`).

---

## Requisitos *(obligatorio)*

### Requisitos Funcionales

- **FR-001**: El proyecto frontend DEBE tener referenciada la librería de UI en su versión mayor objetivo (19+).
- **FR-002**: El proyecto frontend DEBE tener configurado el lenguaje tipado en modo estricto, sin permitir `any` implícito.
- **FR-003**: El proyecto frontend DEBE tener disponible la herramienta de build/dev (Vite, sin webpack) operativa para servidor de desarrollo y build de producción.
- **FR-004**: El proyecto frontend DEBE tener inicializado el sistema de componentes (shadcn/ui) con su archivo de configuración base y soporte de dark mode disponible.
- **FR-005**: El proyecto frontend DEBE tener referenciada la librería de estado de servidor (TanStack Query).
- **FR-006**: El proyecto frontend DEBE tener referenciada la librería de animaciones (Motion).
- **FR-007**: El proyecto frontend DEBE tener referenciado el framework de pruebas (Vitest) y la librería de pruebas de componentes (Testing Library).
- **FR-008**: El proyecto frontend DEBE tener referenciada y configurada la herramienta de linting/formateo (Biome) con su archivo de configuración base.
- **FR-009**: La instalación de dependencias DEBE completarse sin conflictos de versión no resueltos; cualquier conflicto DEBE reportarse explícitamente y bloquear la finalización.
- **FR-010**: La versión mayor de cada dependencia DEBE ser coherente con el stack tecnológico de frontend definido en la constitución (React 19+, Vite, TypeScript strict, shadcn/ui, TanStack Query, Motion, Vitest + Testing Library, Biome).
- **FR-011**: El proyecto frontend DEBE compilar y el servidor de desarrollo DEBE arrancar con cero errores tras la instalación de todas las dependencias.

### Entidades Clave *(N/A para esta especificación de dependencias de frontend)*

---

## Criterios de Éxito *(obligatorio)*

### Resultados Medibles

- **SC-001**: El 100% de las ocho dependencias objetivo (React 19+, TypeScript strict, Vite, shadcn/ui, TanStack Query, Motion, Vitest + Testing Library, Biome) están referenciadas y/o configuradas en el proyecto frontend.
- **SC-002**: El proyecto frontend compila y el servidor de desarrollo arranca con cero errores en la primera ejecución tras la instalación.
- **SC-003**: La instalación de paquetes finaliza sin advertencias de conflicto de versión.
- **SC-004**: El runner de pruebas descubre y ejecuta al menos una prueba de verificación con resultado exitoso.
- **SC-005**: El modo estricto del lenguaje tipado está activado (cero excepciones de `any` implícito permitidas en la configuración).
- **SC-006**: El comando de verificación de la herramienta de linting/formateo se ejecuta correctamente sobre el código base.
- **SC-007**: Un desarrollador puede clonar el repositorio e instalar todas las dependencias frontend sin pasos manuales adicionales más allá del comando de instalación estándar.

---

## Suposiciones

- **Prerequisito**: El proyecto React + Vite de la Fase 0.1 (`001-project-setup`) ya existe como base sobre la que se instalan estas dependencias.
- **Entorno**: El runtime de JavaScript y el gestor de paquetes estándar están instalados localmente y disponibles para instalar y ejecutar el proyecto.
- **Registro de paquetes**: Las dependencias se obtienen del registro de paquetes estándar del ecosistema; existe conectividad de red durante la instalación.
- **Alcance**: Esta fase instala, referencia y deja configurables las dependencias frontend; la configuración funcional concreta (proveedor de TanStack Query en el árbol, tema de dark mode aplicado, reglas específicas de Biome, setup de Testing Library) puede ajustarse en fases posteriores (0.4 en adelante).
- **Organización por feature**: La estructura de componentes por feature con límites claros de hooks/estado (Arquitectura Limpia en frontend) se aplica al implementar features; esta fase solo provee las dependencias.
- **Versiones**: Se asumen las versiones estables mayores compatibles con el stack de la constitución; pins exactos de versión menor se deciden en la fase de planificación.
