# Especificación de Feature: Configuración de Estructura de Proyectos e Infraestructura

**Rama Feature**: `001-project-setup`

**Creado**: 2026-06-24

**Estado**: Activo

**Entrada**: Fase 0.1 - Estructura de Proyectos del roadmap.md

## Escenarios de Usuario & Testing *(obligatorio)*

### Historia de Usuario 1 - Creación de Estructura del Proyecto Backend (Prioridad: P1)

El desarrollador configura el backend ASP.NET Core con capas de arquitectura limpia, permitiendo implementación inmediata de lógica de dominio, servicios de aplicación y preocupaciones de infraestructura en capas aisladas.

**Por qué esta prioridad**: Backend es la base para persistencia de datos y lógica de negocio. Sin esto, endpoints API y servicios worker no pueden construirse.

**Test Independiente**: 
- Verificar directorio structure creada (`backend/Domain`, `backend/Application`, `backend/Infrastructure`, `backend/Api`)
- Verificar que `backend.csproj` existe con referencias de proyecto de arquitectura limpia
- Verificar que .NET 10 SDK puede compilar estructura vacía sin errores

**Escenarios de Aceptación**:

1. **Dado** directorio `backend/` vacío, **Cuando** script de setup ejecuta, **Entonces** directorios `Domain/`, `Application/`, `Infrastructure/`, `Api/` se crean
2. **Dado** estructura de proyecto backend existe, **Cuando** `dotnet build` corre, **Entonces** compilación exitosa con cero errores
3. **Dado** estructura de proyecto backend, **Cuando** revisando archivo de proyecto, **Entonces** versión .NET correcta (10) y referencias de proyecto están set

---

### Historia de Usuario 2 - Creación de Estructura del Proyecto Frontend (Prioridad: P1)

El desarrollador configura frontend React + Vite + TypeScript, permitiendo desarrollo de componentes React con tooling apropiado y type safety de TypeScript desde el inicio.

**Por qué esta prioridad**: Frontend es igualmente crítico; corriendo en paralelo con desarrollo de backend requiere fundación sólida.

**Test Independiente**:
- Verificar directorio `frontend/` con subdirectorios `src/`, `public/`, `dist/`
- Verificar que `vite.config.ts` existe con plugin React configurado
- Verificar que `tsconfig.json` con strict mode activado
- Verificar que `npm run dev` inicia dev server exitosamente

**Escenarios de Aceptación**:

1. **Dado** directorio `frontend/` vacío, **Cuando** script de setup ejecuta, **Entonces** estructura de proyecto Vite + React + TypeScript es inicializada
2. **Dado** proyecto frontend, **Cuando** revisando `tsconfig.json`, **Entonces** `strict: true` está set
3. **Dado** proyecto frontend listo, **Cuando** corriendo `npm run dev`, **Entonces** servidor de desarrollo inicia en puerto 5173 sin errores

---

### Historia de Usuario 3 - Creación de Estructura del Proyecto Worker (Prioridad: P1)

El desarrollador crea proyecto dedicado de worker como ASP.NET Core Hosted Service, permitiendo procesamiento de jobs en background (recordatorios email, transiciones de estado) independientemente de la API principal.

**Por qué esta prioridad**: Worker procesa jobs async; sin esta estructura, API principal bloquearía en operaciones largas.

**Test Independiente**:
- Verificar directorio `worker/` creado con estructura apropiada
- Verificar que `worker.csproj` existe con templates Hosted Service
- Verificar que `dotnet build` es exitoso para proyecto worker

**Escenarios de Aceptación**:

1. **Dado** directorio `worker/` vacío, **Cuando** setup ejecuta, **Entonces** estructura de proyecto .NET Hosted Service se crea
2. **Dado** proyecto worker, **Cuando** examinando referencias de proyecto, **Entonces** referencias a tipos compartidos están configuradas correctamente
3. **Dado** worker listo, **Cuando** construyendo proyecto, **Entonces** compilación es exitosa

---

### Historia de Usuario 4 - Paquete de Tipos Compartidos (Prioridad: P2)

El desarrollador crea paquete compartido para DTOs, enums e interfaces usadas a través de backend, worker y frontend, asegurando contratos de datos consistentes a través de límites de servicios.

**Por qué esta prioridad**: Reduce duplicación y asegura type safety a través de límites de servicios; puede construirse después de servicios core pero debe existir antes de que contratos API finalicen.

**Test Independiente**:
- Verificar directorio `packages/shared/` creado
- Verificar que `shared.csproj` o package manifest existe
- Verificar que tipos compartidos pueden ser referenciados desde backend y worker

**Escenarios de Aceptación**:

1. **Dado** directorio `packages/shared/`, **Cuando** solución construye, **Entonces** backend y worker pueden referenciar tipos compartidos
2. **Dado** paquete compartido existe, **Cuando** agregando nuevo DTO, **Entonces** cambios inmediatamente disponibles en proyectos dependientes

---

### Historia de Usuario 5 - Configuración de Infraestructura Docker (Prioridad: P1)

El desarrollador crea archivos de configuración Docker (docker-compose.yml, Dockerfile, .dockerignore) permitiendo deployment containerizado de todos los servicios más MongoDB para desarrollo y producción.

**Por qué esta prioridad**: Containerización Docker requerida para consistencia dev local y deployment VPS; crítica para colaboración en equipo.

**Test Independiente**:
- Verificar `docker-compose.yml` define servicios: frontend, backend, worker, mongodb
- Verificar que `Dockerfile` existe con multi-stage build
- Verificar que `.dockerignore` excluye archivos innecesarios
- Verificar que `docker-compose up` inicia todos los servicios sin errores

**Escenarios de Aceptación**:

1. **Dado** archivos Docker configurados, **Cuando** corriendo `docker-compose up`, **Entonces** todos cuatro servicios (frontend, backend, worker, mongodb) inician exitosamente
2. **Dado** Docker Compose corriendo, **Cuando** revisando logs de contenedor, **Entonces** MongoDB acepta conexiones en puerto 27017
3. **Dado** contenedores corriendo, **Cuando** accesando servicio frontend, **Entonces** dev server es responsive en puerto 5173

---

### Casos Límite

- ¿Qué pasa cuando workspace ya tiene estructura de directorio parcial (ej: frontend existe pero backend falta)? → Setup debe fallar con mensaje claro de empezar fresco
- ¿Cómo se comporta setup si versión .NET SDK no está instalada? → Setup debe validar disponibilidad .NET 10 antes de proceder
- ¿Qué si versión Node.js es incompatible con requisitos Vite? → Setup debe chequear versión Node y reportar si insuficiente

---

## Requisitos *(obligatorio)*

### Requisitos Funcionales

- **FR-001**: Script de setup DEBE crear directorio de proyecto backend con capas de arquitectura limpia: `Domain/`, `Application/`, `Infrastructure/`, `Api/`
- **FR-002**: Script de setup DEBE crear directorio de proyecto frontend con configuración React + Vite + TypeScript
- **FR-003**: Script de setup DEBE crear directorio de proyecto worker como template ASP.NET Core Hosted Service
- **FR-004**: Script de setup DEBE crear directorio `packages/shared/` para tipos compartidos y DTOs
- **FR-005**: Script de setup DEBE generar `.gitignore` apropiado para proyectos .NET + Node.js
- **FR-006**: Script de setup DEBE crear `docker-compose.yml` con cuatro servicios: frontend (puerto 5173), backend (puerto 5000), worker, MongoDB (puerto 27017)
- **FR-007**: Script de setup DEBE crear `Dockerfile` con multi-stage build (assets frontend + runtime backend)
- **FR-008**: Script de setup DEBE crear `.dockerignore` para excluir build artifacts, node_modules, bin/, obj/
- **FR-009**: Todos los archivos de proyecto DEBEN estar correctamente referenciados y ser compilables sin configuración externa
- **FR-010**: Estructura de solución DEBE cumplir con principios de Arquitectura Limpia per constitución

### Entidades Clave *(N/A para esta especificación de infraestructura)*

---

## Criterios de Éxito *(obligatorio)*

### Resultados Medibles

- **SC-001**: Inicialización de estructura de proyecto completa en menos de 2 minutos
- **SC-002**: Todos los servicios construyen exitosamente con cero errores en primer run
- **SC-003**: `docker-compose up` inicia todos cuatro contenedores dentro de 30 segundos sin errores
- **SC-004**: Desarrolladores pueden clonar repo y ejecutar `docker-compose up` sin pasos adicionales de setup manual
- **SC-005**: MongoDB es accesible desde servicio backend con string de conexión predeterminado
- **SC-006**: Frontend dev server es accesible en `http://localhost:5173` y sirve app React
- **SC-007**: Backend API responde a health check en `http://localhost:5000/health` (si implementado)
- **SC-008**: 100% de archivos de proyecto siguen naming conventions y estructura definida en patrón Clean Architecture

---

## Suposiciones

- **Herramientas**: Desarrolladores tienen Docker Desktop, .NET 10 SDK, y Node.js 18+ instalados localmente
- **Repositorio**: Repositorio Git ya está inicializado; script de setup agrega a repo existente
- **Alcance**: Setup es solo infraestructura; sin código más allá de scaffolding de proyecto es committed
- **Herramienta CLI**: Setup será un script PowerShell (`.specify/scripts/powershell/create-new-feature.ps1`) o equivalente bash
- **Gestor de Paquetes**: Frontend usa npm; backend usa NuGet; asume gestores de paquetes estándar disponibles
- **Base de Datos**: MongoDB es containerizado vía Docker; sin requerimiento de instalación local de MongoDB
- **Variables de Entorno**: Configuración de desarrollo predeterminada (puertos, URIs de BD) hardcodeada en docker-compose; overrides de producción vía archivos env
- **VCS**: .gitignore es suficiente para excluir build artifacts; sin Git LFS necesario para esta fase

