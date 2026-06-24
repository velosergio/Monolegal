# Plan de Implementación: Configuración de Estructura de Proyectos e Infraestructura

**Rama**: `001-project-setup` | **Fecha**: 2026-06-24 | **Spec**: [spec.md](spec.md)

**Entrada**: Especificación de feature de `/specs/001-project-setup/spec.md`

## Resumen

Establecer la infraestructura base de Monolegal mediante creación de estructura de proyectos multi-capa (backend ASP.NET Core con arquitectura limpia, frontend React + Vite, worker dedicado, tipos compartidos) junto con configuración Docker completa para desarrollo local y deployment VPS. Esto habilita desarrollo paralelo de todas las capas manteniendo clara separación de responsabilidades y consistency.

## Contexto Técnico

**Lenguaje/Versión Backend**: ASP.NET Core 10, .NET 10 SDK

**Lenguaje/Versión Frontend**: React 19+, Node.js 18+, TypeScript 5.x

**Dependencias Primarias Backend**: 
- Minimal APIs (built-in .NET)
- MongoDB Driver (no EF Core)
- FluentValidation
- Serilog

**Dependencias Primarias Frontend**: 
- React 19+
- Vite
- shadcn/ui
- TanStack Query
- Motion

**Almacenamiento**: MongoDB (containerizado via Docker)

**Testing Backend**: xUnit + FluentAssertions

**Testing Frontend**: Vitest + Testing Library

**Plataforma Objetivo**: Docker containers (desarrollo local + VPS Linux)

**Tipo de Proyecto**: Web application (backend API + frontend SPA + background worker)

**Objetivos de Performance**: 
- Backend: queries ≤200ms bajo carga normal
- Frontend: TTI < 2s, Lighthouse performance > 90, bundle gzipped < 50KB

**Restricciones**: 
- Imágenes Docker < 500MB tamaño final
- Stateless APIs (escalable horizontalmente)
- Sin secrets embebidos en imágenes
- Connection pooling MongoDB requerido

**Escala/Alcance**: 
- 3+ clientes simultáneos (MVP)
- 8+ facturas en demostración
- Endpoints API ~5 para MVP
- ~4 páginas React para MVP

## Revisión de Constitución

*PUERTA: Debe pasar antes de investigación de Fase 0. Re-chequear después de diseño de Fase 1.*

### Alineación con Principios

✅ **I. Arquitectura Limpia**: 
- Backend especifica capas Domain/Application/Infrastructure/Api separadas
- Frontend organizado por features con límites claros
- **Status**: CUMPLE

✅ **II. Principios SOLID**: 
- DI container manejará inyección en backend
- Separación clara de responsabilidades por proyecto
- **Status**: CUMPLE

✅ **III. Desarrollo Dirigido por Especificaciones (SDD)**: 
- Especificación en GIVEN/WHEN/THEN definida
- Documentación requerida en **español** (verificado)
- **Status**: CUMPLE

✅ **IV. Desarrollo Test-First**: 
- Criterios de éxito medibles y testeables
- Escenarios de aceptación definidos
- **Status**: CUMPLE

✅ **V. Frontend de Calidad Producción**: 
- TypeScript strict mode especificado
- Biome + React Doctor mencionados
- **Status**: CUMPLE

✅ **VI. Código Observable y Mantenible**: 
- Serilog para logging estructurado especificado
- Documentación clara en comentarios (por definir en implementación)
- **Status**: CUMPLE

### Verificación de Gates

| Gate | Criterio | Status |
|------|----------|--------|
| Requisitos Testeables | Todos los FR tienen AC claros | ✅ PASA |
| Alcance Delimitado | 5 historias P1/P2 priorizadas | ✅ PASA |
| Dependencias Identificadas | Stack tecnológico locked | ✅ PASA |
| Compliance Constitucional | Todos los 6 principios alineados | ✅ PASA |
| Documentación Idioma | Especificación 100% español | ✅ PASA |

**Veredicto**: ✅ **APROBADO PARA FASE 0**

---

## Estructura del Proyecto

### Documentación (esta feature)

```text
specs/001-project-setup/
├── spec.md                  # Especificación (DONE)
├── plan.md                  # Este archivo (plan de implementación)
├── research.md              # OUTPUT Fase 0 (decisiones tecnológicas)
├── data-model.md            # OUTPUT Fase 1 (N/A para infraestructura)
├── quickstart.md            # OUTPUT Fase 1 (guía de validación)
├── contracts/               # OUTPUT Fase 1 (interfaces externas)
│   └── docker-compose.yml   # Contrato: servicios, puertos, volúmenes
└── checklists/
    └── requirements.md      # Validación de calidad (DONE)
```

### Código Fuente (raíz del repositorio)

```text
.
├── backend/                           # ASP.NET Core (.NET 10)
│   ├── Domain/                        # Entidades, interfaces, lógica de negocio
│   ├── Application/                   # Casos de uso, DTOs, servicios
│   ├── Infrastructure/                # MongoDB, email, logging
│   ├── Api/                           # Minimal APIs, controllers, middleware
│   ├── backend.csproj
│   └── Tests/                         # xUnit tests
│
├── frontend/                          # React + Vite + TypeScript
│   ├── src/
│   │   ├── components/                # Componentes React reutilizables
│   │   ├── pages/                     # Páginas (invoices, dashboard, settings)
│   │   ├── hooks/                     # Custom hooks
│   │   └── services/                  # API client, state management
│   ├── public/                        # Assets estáticos
│   ├── package.json
│   ├── vite.config.ts
│   ├── tsconfig.json
│   └── tests/                         # Vitest tests
│
├── worker/                            # ASP.NET Core Hosted Service
│   ├── Services/                      # Servicios de background job
│   ├── Configuration/                 # Appsettings, DI setup
│   ├── worker.csproj
│   └── Tests/                         # xUnit tests
│
├── packages/
│   └── shared/                        # DTOs, Enums, Interfaces compartidas
│       ├── Models/
│       ├── Dtos/
│       └── shared.csproj
│
├── docker-compose.yml                 # Orquestación de servicios
├── Dockerfile                         # Multi-stage build
├── .dockerignore
└── .gitignore
```

**Decisión de Estructura**: Multi-proyecto monorepo con separación clara:
- Backend como API REST stateless (escalable)
- Frontend como SPA React (bundled estáticamente, servido desde backend o CDN)
- Worker como servicio independiente (escalable horizontalmente)
- Tipos compartidos en paquete NuGet para máxima type safety
- Docker Compose orquesta todos los servicios para desarrollo local

---

## Fase 0: Investigación & Decisiones Arquitectónicas

### Tareas de Investigación

1. **Validar Disponibilidad de .NET 10 SDK**
   - Verificar versión mínima requerida
   - Confirmar soporte de Minimal APIs en .NET 10
   - Documentar en research.md

2. **Evaluar Opciones de Dockerfile Multi-Stage**
   - Comparar estrategias: node builder → backend runtime
   - Seleccionar optimización de tamaño final (<500MB)
   - Documentar decisión

3. **Validar MongoDB en Docker**
   - Persistencia de volúmenes
   - Connection pooling desde .NET driver
   - Estrategia de inicialización (seed data)

4. **Stack de Testing**
   - xUnit + FluentAssertions configuración
   - Vitest + Testing Library setup
   - Playwright para E2E

5. **Herramientas de Calidad**
   - Biome configuración (frontend)
   - React Doctor setup
   - dotnet format configuration

### Salida Esperada: `research.md`

Documento que capture:
- Decisiones tecnológicas justificadas
- Alternativas consideradas y rechazadas
- Configuración recomendada para cada stack
- Links a documentación relevante

---

## Fase 1: Diseño & Contratos

### T1.1: Modelo de Datos

**Archivo**: `data-model.md`

Para esta Fase 0 (infraestructura), el modelo de datos es **mínimo**:

- **Project** (conceptual): Backend, Frontend, Worker, Shared
- **Relaciones**: Shared → Backend, Shared → Worker, Backend → Frontend (SPA static served)
- **Configuración**: docker-compose.yml define servicios y networking

*Nota: Modelo de datos completo (Invoice, Client, etc.) será en Fase 1 Domain implementation*

### T1.2: Contratos (Interfaces Externas)

**Archivo**: `contracts/docker-compose.yml`

Define contrato de deployment:

```yaml
version: '3.8'
services:
  frontend:
    build: ./frontend
    ports: [5173]
    environment: [VITE_API_URL=http://backend:5000]
    
  backend:
    build: ./backend
    ports: [5000]
    environment: [MONGODB_URI=mongodb://mongo:27017/monolegal_dev]
    depends_on: [mongo]
    
  worker:
    build: ./worker
    environment: [MONGODB_URI=mongodb://mongo:27017/monolegal_dev]
    depends_on: [mongo]
    
  mongo:
    image: mongo:7
    ports: [27017]
    volumes: [mongo_data:/data/db]

volumes:
  mongo_data:
```

**Contrato de Health Checks**:
- Backend: `GET /health` → `{ "status": "healthy" }` (HTTP 200)
- Frontend: Accesible en `http://localhost:5173` (HTML 200)
- MongoDB: Conexión exitosa desde backend en startup

### T1.3: Guía de Validación Rápida

**Archivo**: `quickstart.md`

```markdown
# Validación Rápida: Estructura de Proyectos

## Prerequisitos
- Docker Desktop ejecutándose
- .NET 10 SDK instalado
- Node.js 18+ instalado
- Git inicializado en repo

## Setup Inicial (< 2 min)
1. Clonar repositorio
2. Ejecutar: `docker-compose up -d --build`
3. Esperar 30s para que servicios inicien

## Validación

### Backend
\`\`\`bash
curl http://localhost:5000/health
# Respuesta esperada: {"status":"healthy"}
\`\`\`

### Frontend
\`\`\`bash
curl http://localhost:5173
# Respuesta esperada: HTML content (React app)
\`\`\`

### MongoDB
\`\`\`bash
docker exec monolegal-mongo mongosh --eval "db.adminCommand('ping')"
# Respuesta esperada: { ok: 1 }
\`\`\`

### Estructura Local
Verificar directorios existentes:
- ✅ \`backend/Domain\`, \`backend/Application\`, \`backend/Infrastructure\`, \`backend/Api\`
- ✅ \`frontend/src\`, \`frontend/public\`
- ✅ \`worker/Services\`, \`worker/Configuration\`
- ✅ \`packages/shared\`
```

---

## Actualización de Contexto de Agente

Después de completar Fase 1, ejecutar:

```bash
/speckit.agent-context.update
```

Esto actualizará [.github/copilot-instructions.md](../../.github/copilot-instructions.md) con referencia a este plan.

---

## Re-evaluación de Constitución (Post-Diseño)

✅ **Verificación Final**: Todos los gates pasan post-Fase 1
- ✅ Arquitectura limpia estructura confirmada
- ✅ SOLID principles aplicables a cada capa
- ✅ Testing strategy documentada
- ✅ Documentación 100% español
- ✅ Infrastructure production-ready

**Status**: ✅ **LISTO PARA FASE 2: GENERACIÓN DE TAREAS**

---

## Próximos Pasos

1. **Ejecutar Fase 0**: Completar investigaciones y generar `research.md`
2. **Ejecutar Fase 1**: Generar `data-model.md`, `contracts/docker-compose.yml`, `quickstart.md`
3. **Ejecutar `/speckit.tasks`**: Generar tareas ordenadas por dependencia desde este plan
4. **Ejecutar `/speckit.implement`**: Crear estructura real, archivos, configuración

---

## Anexo: Stack Tecnológico Confirmado

| Aspecto | Selección |
|--------|-----------|
| Backend Runtime | .NET 10 ASP.NET Core |
| Backend Patrón | Minimal APIs + Clean Architecture |
| DB | MongoDB (containerizado) |
| Validación | FluentValidation |
| Logging | Serilog (structured JSON) |
| Frontend Framework | React 19+ |
| Frontend Build Tool | Vite |
| Frontend Language | TypeScript strict |
| Frontend UI | shadcn/ui |
| Frontend Data | TanStack Query |
| Frontend Animations | Motion |
| Testing Backend | xUnit + FluentAssertions |
| Testing Frontend | Vitest + Testing Library |
| Testing E2E | Playwright |
| Linting Frontend | Biome |
| Container Orchestration | Docker Compose |
| Target Environment | Linux VPS (Ubuntu 22.04+) |
