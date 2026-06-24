# Tareas: Configuración de Estructura de Proyectos e Infraestructura

**Entrada**: Documentos de diseño de `/specs/001-project-setup/`

**Prerequisitos**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Organización**: Tareas agrupadas por user story (US1-US5) para permitir implementación e testing independiente de cada historia.

**Objetivo**: Completar Fase 0.1 en < 2 minutos; validar con `docker-compose up -d --build` en < 30 segundos.

---

## Formato: `[ID] [P?] [Story?] Descripción con archivo`

- **[P]**: Puede ejecutarse en paralelo (archivos diferentes, sin dependencias bloqueantes)
- **[Story]**: A cuál user story pertenece ([US1], [US2], [US3], [US4], [US5])
- Paths exactos incluidos en descripciones

---

## Fase 1: Setup (Infraestructura Compartida)

**Propósito**: Inicialización de proyecto y estructura base

- [X] T001 Crear estructura de directorios raíz según plan de implementación: `backend/`, `frontend/`, `worker/`, `packages/shared/`, `scripts/`, `docs/`
- [X] T002 Crear `.gitignore` con exclusiones para .NET y Node.js (node_modules/, bin/, obj/, dist/, .env.local)
- [X] T003 [P] Crear archivo `README.md` raíz con instrucciones de setup inicial y referencia a documentación SDD
- [X] T004 [P] Crear directorio `.specify/scripts/` para scripts de automatización del setup

---

## Fase 2: Foundational (Prerequisitos Bloqueantes)

**Propósito**: Configuración base necesaria antes de implementar cualquier user story

**⚠️ CRÍTICO**: Ningún trabajo de user story puede comenzar hasta que esta fase esté completa

- [X] T005 Validar que .NET 10 SDK está disponible; crear `global.json` en raíz fijando versión .NET 10
- [X] T006 [P] Validar que Node.js 18+ está disponible; crear `.node-version` o `nvmrc` si necesario
- [X] T007 [P] Crear archivo `docker-compose.yml` en raíz usando contrato de deployment de `specs/001-project-setup/contracts/docker-compose.yml`
- [X] T008 [P] Crear `Dockerfile` multi-stage (etapa 1: Node frontend build, etapa 2: .NET backend build, etapa 3: ASP.NET runtime) en raíz
- [X] T009 [P] Crear `.dockerignore` en raíz excluyendo build artifacts, node_modules/, bin/, obj/, .git/
- [X] T010 Crear `scripts/init-mongo.js` para seed data inicial de MongoDB en docker-compose

**Checkpoint**: Infraestructura foundational lista - ahora se pueden implementar user stories en paralelo

---

## Fase 3: User Story 1 - Creación de Estructura del Proyecto Backend (Prioridad: P1)

**Objetivo**: Backend ASP.NET Core con capas Clean Architecture (Domain/Application/Infrastructure/Api) compilable y estructuralmente correcto

**Test Independiente**: 
- Verificar directorios `backend/Domain`, `backend/Application`, `backend/Infrastructure`, `backend/Api` existen
- Ejecutar `dotnet build` exitosamente sin errores
- Ejecutar `dotnet test` para validar estructura

### Implementación para User Story 1

- [x] T011 [P] [US1] Crear directorio backend con subcarpetas: `Domain/`, `Application/`, `Infrastructure/`, `Api/`, `Tests/` en `backend/`
- [x] T012 [P] [US1] Crear archivo `backend.csproj` con referencia a .NET 10 en `backend/backend.csproj` con TargetFramework net10.0
- [x] T013 [P] [US1] Crear proyecto Domain: crear `backend/Domain/Domain.csproj` (capa más interna, sin dependencias externas)
- [x] T014 [P] [US1] Crear proyecto Application: crear `backend/Application/Application.csproj` con referencia a Domain.csproj
- [x] T015 [P] [US1] Crear proyecto Infrastructure: crear `backend/Infrastructure/Infrastructure.csproj` con referencias a Domain y Application; agregar dependencias: MongoDB.Driver, Serilog, FluentValidation
- [x] T016 [P] [US1] Crear proyecto Api: crear `backend/Api/Api.csproj` (Minimal APIs) con referencias a todos; agregar dependencia: Microsoft.AspNetCore.OpenApi
- [x] T017 [US1] Crear archivo `backend/Domain/Interfaces/IRepository.cs` como interfaz base para repositorios genéricos
- [x] T018 [US1] Crear archivo `backend/Application/Services/AppService.cs` como clase base para servicios de aplicación
- [x] T019 [US1] Crear archivo `backend/Infrastructure/Configuration/DependencyInjection.cs` con setup de DI container (MongoClient, logging)
- [x] T020 [US1] Crear archivo `backend/Api/Program.cs` con setup mínimo: DI container, middleware base, health check endpoint `/health`
- [x] T021 [US1] Crear archivo `backend/Tests/UnitTests/SampleTest.cs` con un test simple para validar xUnit setup
- [x] T022 [US1] Crear archivo `backend/.csproj` a nivel raíz (solución) que agrupa Domain, Application, Infrastructure, Api, Tests en una sola compilación
- [x] T023 [US1] Ejecutar `dotnet build` en `backend/` y validar compilación exitosa sin errores ni warnings

**Checkpoint**: User Story 1 completo - Backend estructura lista, compilable, con health check disponible

---

## Fase 4: User Story 2 - Creación de Estructura del Proyecto Frontend (Prioridad: P1)

**Objetivo**: Frontend React 19+ + Vite + TypeScript strict, inicializado y dev server funcional

**Test Independiente**: 
- Verificar directorios `frontend/src`, `frontend/public`, `frontend/dist`
- Verificar `npm run dev` inicia server en puerto 5173 sin errores
- Ejecutar `npm run build` y generar assets en `dist/`

### Implementación para User Story 2

- [x] T024 [P] [US2] Crear directorio `frontend/` con subdirectorios: `src/`, `public/`, `tests/` en `frontend/`
- [x] T025 [P] [US2] Crear archivo `frontend/package.json` con dependencies: react@19, react-dom@19, vite, typescript, y dev dependencies: @vitejs/plugin-react, @testing-library/react, vitest, biome
- [x] T026 [US2] Ejecutar `npm ci` en directorio `frontend/` para instalar dependencias
- [x] T027 [P] [US2] Crear archivo `frontend/vite.config.ts` con plugin React, puerto 5173, entrada `src/main.tsx`
- [x] T028 [P] [US2] Crear archivo `frontend/tsconfig.json` con `strict: true`, `noImplicitAny: true`, module resolucion, lib: es2020/dom
- [x] T029 [P] [US2] Crear archivo `frontend/biome.json` con reglas linting y formatting estándar (indentWidth: 2, lineWidth: 100)
- [x] T030 [P] [US2] Crear archivo `frontend/vitest.config.ts` con environment jsdom, globals true, coverage v8
- [x] T031 [P] [US2] Crear archivo `frontend/src/main.tsx` (entry point) que importa App, renderiza en #root
- [x] T032 [P] [US2] Crear archivo `frontend/src/App.tsx` (componente raíz) con React.FC, estructura básica, estilos placeholder
- [x] T033 [P] [US2] Crear archivo `frontend/src/index.css` con estilos base (reset CSS, variables CSS para dark mode)
- [x] T034 [P] [US2] Crear archivo `frontend/public/index.html` con `<div id="root"></div>`, script src="src/main.tsx" type="module"
- [x] T035 [US2] Crear archivo `frontend/.env.example` con `VITE_API_URL=http://localhost:5000`
- [x] T036 [US2] Agregar scripts en `frontend/package.json`: `dev` (vite), `build` (vite build), `test` (vitest), `lint` (biome check), `format` (biome format --write)
- [x] T037 [US2] Ejecutar `npm run build` en `frontend/` y validar que `dist/` se crea exitosamente
- [x] T038 [US2] Crear archivo `frontend/tests/App.test.tsx` con test simple que renderiza App y verifica que monta sin errores

**Checkpoint**: User Story 2 completo - Frontend estructura lista, dev server funcional en 5173

---

## Fase 5: User Story 3 - Creación de Estructura del Proyecto Worker (Prioridad: P1)

**Objetivo**: Worker ASP.NET Core como Hosted Service, compilable, referencia a tipos compartidos

**Test Independiente**: 
- Verificar directorio `worker/` con estructura apropiada
- Ejecutar `dotnet build` exitosamente
- Verificar referencias a shared.csproj están configuradas

### Implementación para User Story 3

- [x] T039 [P] [US3] Crear directorio `worker/` con subdirectorios: `Services/`, `Configuration/`, `Tests/` en `worker/`
- [x] T040 [P] [US3] Crear archivo `worker/worker.csproj` con TargetFramework net10.0, dependencias: Microsoft.Extensions.Hosting, MongoDB.Driver, Serilog
- [x] T041 [P] [US3] Crear archivo `worker/Configuration/appsettings.json` con configuración MongoDB URI y Serilog logging (JSON)
- [x] T042 [P] [US3] Crear archivo `worker/Configuration/appsettings.Development.json` con overrides para desarrollo
- [x] T043 [P] [US3] Crear archivo `worker/Services/BackgroundWorker.cs` implementando `IHostedService` con StartAsync/StopAsync stub methods
- [x] T044 [P] [US3] Crear archivo `worker/Program.cs` con:
  - AddHostedService<BackgroundWorker>()
  - Configuración Serilog
  - Setup MongoDB client
  - Referencia a shared types
- [x] T045 [US3] Agregar referencia a `packages/shared/shared.csproj` en `worker/worker.csproj`
- [x] T046 [US3] Crear archivo `worker/Tests/WorkerTests.cs` con test simple que valida Hosted Service setup
- [x] T047 [US3] Crear solución file `worker.sln` que agrupa worker.csproj y shared.csproj
- [x] T048 [US3] Ejecutar `dotnet build` en `worker/` y validar compilación exitosa sin errores

**Checkpoint**: User Story 3 completo - Worker estructura lista, compilable

---

## Fase 6: User Story 4 - Paquete de Tipos Compartidos (Prioridad: P2)

**Objetivo**: Paquete compartido con DTOs/Enums/Interfaces, referenceable desde backend y worker

**Test Independiente**: 
- Verificar directorio `packages/shared/` existe
- Ejecutar `dotnet build` de shared.csproj
- Verificar que backend y worker pueden referenciar tipos compartidos

### Implementación para User Story 4

- [x] T049 [P] [US4] Crear directorio `packages/shared/` con subdirectorios: `Models/`, `Dtos/`, `Enums/` en `packages/shared/`
- [x] T050 [P] [US4] Crear archivo `packages/shared/shared.csproj` con TargetFramework net10.0, sin dependencias externas (puro DTOs/enums)
- [x] T051 [P] [US4] Crear archivo `packages/shared/Enums/InvoiceStatus.cs` con enum: Pending, Paid, Overdue, Cancelled
- [x] T052 [P] [US4] Crear archivo `packages/shared/Dtos/InvoiceDto.cs` con propiedades: Id, ClientId, Amount, Status, CreatedAt, UpdatedAt
- [x] T053 [P] [US4] Crear archivo `packages/shared/Dtos/CreateInvoiceDto.cs` con propiedades: ClientId, Amount, DueDate
- [x] T054 [P] [US4] Crear archivo `packages/shared/Models/Invoice.cs` como modelo compartido básico
- [x] T055 [US4] Agregar referencia a `packages/shared/shared.csproj` en `backend/backend.csproj`
- [x] T056 [US4] Ejecutar `dotnet build` en `packages/shared/` y validar compilación sin errores
- [x] T057 [US4] Ejecutar `dotnet build` en `backend/` y validar que puede referenciar tipos desde shared

**Checkpoint**: User Story 4 completo - Tipos compartidos disponibles, referenceable desde backend y worker

---

## Fase 7: User Story 5 - Configuración de Infraestructura Docker (Prioridad: P1)

**Objetivo**: Docker Compose y Dockerfile totalmente funcionales; `docker-compose up` inicia todos 4 servicios en < 30s

**Test Independiente**: 
- Ejecutar `docker-compose up -d --build` 
- Verificar `docker-compose ps` muestra 4 servicios "Up (healthy)"
- Ejecutar validaciones health check desde quickstart.md

### Implementación para User Story 5

- [x] T058 [US5] Crear Dockerfile producción-ready:
  - Etapa 1 (frontend builder): Node 22-alpine, instala deps frontend, `npm ci && npm run build`
  - Etapa 2 (backend builder): dotnet:10-sdk, construye backend.sln con `dotnet build -c Release`
  - Etapa 3 (runtime): aspnet:10-alpine runtime, copia assets frontend a wwwroot, expone puerto 5000
- [x] T059 [P] [US5] Crear `docker-compose.yml` según contrato en `specs/001-project-setup/contracts/docker-compose.yml`:
  - Servicio frontend: build ./frontend, puerto 5173, env VITE_API_URL=http://backend:5000
  - Servicio backend: build ., puerto 5000, env MONGODB_URI, depends_on mongo
  - Servicio worker: build ./worker, env MONGODB_URI, depends_on mongo
  - Servicio mongo: image mongo:7, puerto 27017, volumen mongo_data, healthcheck
- [x] T060 [P] [US5] Crear `scripts/init-mongo.js` para inicialización de MongoDB en docker-compose (seed data minimal)
- [x] T061 [US5] Actualizar `.dockerignore` con reglas finales: node_modules/, .git/, .env, **/*.pdb
- [x] T062 [US5] Actualizar `.gitignore` con entradas: .env.local, dist/, obj/, bin/, mongo_data/
- [x] T063 [US5] Crear archivo `.env.example` con variables:
  - ASPNETCORE_ENVIRONMENT=Development
  - MONGODB_URI=mongodb://root:example_dev_password@mongo:27017/monolegal_dev
  - VITE_API_URL=http://localhost:5000
  - LOG_LEVEL=Debug
- [x] T064 [US5] Crear `docker-compose.override.yml` para development overrides (volumes, debug config)
- [x] T065 [US5] Ejecutar `docker-compose up -d --build` y esperar 30s
- [x] T066 [US5] Ejecutar `docker-compose ps` y verificar 4 servicios "Up (healthy)"
- [x] T067 [US5] Ejecutar health checks: `curl http://localhost:5000/health`, `curl http://localhost:5173`
- [x] T068 [US5] Ejecutar `docker-compose logs mongo` y validar MongoDB está aceptando conexiones

**Checkpoint**: User Story 5 completo - Infraestructura Docker totalmente funcional

---

## Fase 8: Polish & Cross-Cutting Concerns

**Propósito**: Validación final, documentación, cleanup

- [~] T069 [P] Ejecutar `docker-compose down -v` para limpiar volúmenes y verificar cleanup sin error
- [~] T070 [P] Ejecutar checklist de validación de `specs/001-project-setup/quickstart.md` - verificar todas las 30+ validaciones PASAN
- [~] T071 [P] Actualizar `README.md` raíz con:
  - Links a specs, plan, research
  - Instrucciones quick start: `docker-compose up -d --build`
  - Link a quickstart.md para validación completa
- [~] T072 [P] Crear archivo `DEVELOPMENT.md` con guía de desarrollo local:
  - Cómo agregar dependencias a cada proyecto
  - Cómo ejecutar tests localmente vs en Docker
  - Troubleshooting común
- [~] T073 Ejecutar `dotnet build` y `npm run build` en paralelo desde raíz y validar cero errores
- [~] T074 Ejecutar todos los tests: `dotnet test` (backend), `npm run test -- --run` (frontend)
- [~] T075 Validar que 100% de archivos siguen naming conventions per constitución (PascalCase para C#, camelCase para TypeScript)
- [~] T076 Ejecutar Biome linting: `npm run lint` en frontend - 0 errors
- [~] T077 Crear `CLEANUP.md` documentando qué remover post-MVP:
  - Test fixtures temporales
  - Health check stub si no es usado
  - Documentación de desarrollo si movida a wiki
- [~] T078 Commit inicial: "feat(spec-001): Configuración estructura de proyectos e infraestructura Docker" 

**Checkpoint**: Phase 0.1 COMPLETO - Estructura totalmente validada, lista para Phase 1

---

## Dependencias & Secuencia de Ejecución

### Fase 1 (Setup) - Prerequisito para todo
- T001 → T002 → T003 → T004 (secuencial)

### Fase 2 (Foundational) - Bloquea todas las user stories
- T005 ✓ T006 ✓ T007 ✓ T008 ✓ T009 (paralelo)
- T010 (secuencial después de T009)

### Fases 3, 4, 5, 6 (User Stories 1-4) - Pueden ejecutarse en PARALELO
- **US1 (Backend)**: T011-T023 (paralelo: T011-T016, luego secuencial T017-T023)
- **US2 (Frontend)**: T024-T038 (paralelo: T024-T034, luego secuencial T035-T038)
- **US3 (Worker)**: T039-T048 (paralelo: T039-T043, luego secuencial T044-T048)
- **US4 (Shared)**: T049-T057 (paralelo: T049-T054, secuencial T055-T057)

### Fase 7 (US5 - Docker) - Bloqueado en T001-T004, pero puede ejecutarse DESPUÉS de US1-US4 (no dependencia estricta)
- T058-T068 (secuencial pero rápido)

### Fase 8 (Polish) - FINAL, después de todas
- T069-T078 (mayormente paralelo, secuencial T073+)

---

## Tiempo Estimado por Fase

| Fase | Descripción | Tiempo Estimado | Serial/Paralelo |
|------|-------------|-----------------|-----------------|
| 1 | Setup | 2 min | Serial (4 tareas rápidas) |
| 2 | Foundational | 10 min | Paralelo (5 tareas, 1 bloqueante) |
| 3 | US1 Backend | 15 min | Paralelo (16 tareas, 6 paralelo) |
| 4 | US2 Frontend | 15 min | Paralelo (15 tareas, 11 paralelo) |
| 5 | US3 Worker | 10 min | Paralelo (10 tareas, 5 paralelo) |
| 6 | US4 Shared | 5 min | Paralelo (9 tareas, 6 paralelo) |
| 7 | US5 Docker | 20 min | Secuencial (11 tareas, validación) |
| 8 | Polish | 10 min | Paralelo (10 tareas) |
| **TOTAL** | | **85 min serial / ~40 min paralelo** | |

**Objetivo SDD**: Completar < 120 min serial, < 45 min paralelo ✓

---

## Criterios de Aceptación (Completitud)

✅ **Setup**: Directorios existen, gitignore en lugar, herramientas de build configuradas  
✅ **Foundational**: .NET 10 validado, Node validado, Docker setup completo  
✅ **US1**: Backend compila, health check responde, arquitectura limpia verificada  
✅ **US2**: Frontend compila, dev server en 5173, TypeScript strict, Biome pass  
✅ **US3**: Worker compila, referencias a shared funcionales  
✅ **US4**: Shared tipos disponibles, backend y worker pueden usarlos  
✅ **US5**: `docker-compose up -d --build` inicia 4 servicios healthy en < 30s  
✅ **Polish**: Validaciones 30+ de quickstart.md TODAS PASAN, 0 errores en linting/testing  

---

## Próximos Pasos (Fase 1)

Una vez completadas todas las tareas de esta Fase 0.1:

1. **Validar**: Ejecutar quickstart.md checklist completo
2. **Commit**: Push a rama `001-project-setup` 
3. **Merge**: PR a main después de code review
4. **Próxima Fase**: Comenzar Spec 0.2 (Backend Dependencies) del roadmap

---

**Status**: ✅ **TAREAS GENERADAS - LISTO PARA IMPLEMENTACIÓN**

Ejecutar con: `/speckit.implement`
