# Modelo de Datos: Configuración de Estructura de Proyectos e Infraestructura

**Fase**: Fase 1 - Diseño  
**Entrada**: [plan.md](plan.md), [research.md](research.md)

---

## Descripción

Para la Fase 0 (Infraestructura), el "modelo de datos" es conceptual—define la **estructura organizacional** de proyectos y sus relaciones, no entidades de negocio. El modelo de entidades (Invoice, Client, etc.) se define en Fase 1.2 (Dominio).

---

## Componentes Estructurales

### C1: Proyecto Backend

**Responsabilidad**: API REST stateless para lógica de negocio (cobranza)

**Estructura Interna**:
```
backend/
├── Domain/
│   ├── Entities/           # Entidades de negocio (Invoice, Client)
│   ├── ValueObjects/       # Objetos de valor (Money, InvoiceStatus)
│   ├── Interfaces/         # Contratos (IRepository, IEmailService)
│   └── Exceptions/         # Excepciones custom (DomainException)
│
├── Application/
│   ├── Services/           # Casos de uso (ListInvoices, TransitionState)
│   ├── Dtos/              # Transfer objects
│   └── Mappings/          # AutoMapper profiles
│
├── Infrastructure/
│   ├── Persistence/       # MongoDB repositories
│   ├── Email/            # SMTP, SendGrid, etc.
│   └── Configuration/     # DI setup
│
└── Api/
    ├── Endpoints/         # Minimal API route handlers
    ├── Middleware/        # Error handling, logging
    └── Program.cs         # DI container, middleware
```

**Responsabilidades por Capa**:

| Capa | Importa De | Expone |
|------|-----------|--------|
| Domain | Nada | Entities, Interfaces |
| Application | Domain | Services, DTOs |
| Infrastructure | Domain, Application | Concrete implementations |
| Api | All | HTTP endpoints |

**Dependencias Externas**:
- MongoDB (connectionstring via env var)
- Email provider (configurable)
- Logging (Serilog)

**Validación**:
- Cada clase en Domain tiene test unitario
- Application services inyectan dependencies (no Service Locator)
- Infrastructure no filtra a través de API (interface boundary)

---

### C2: Proyecto Frontend

**Responsabilidad**: Single Page App (React) para visualizar y transicionar facturas

**Estructura Interna**:
```
frontend/
├── src/
│   ├── components/
│   │   ├── InvoiceList.tsx        # Tabla de facturas
│   │   ├── InvoiceDetail.tsx      # Modal detalle
│   │   ├── StateTransitionForm.tsx # Formulario transición
│   │   └── common/                # Componentes reutilizables
│   │
│   ├── pages/
│   │   ├── InvoicesPage.tsx       # Página principal
│   │   └── SettingsPage.tsx       # Configuración
│   │
│   ├── hooks/
│   │   ├── useInvoices.ts        # Query invoices
│   │   └── useTransition.ts      # Mutation transition
│   │
│   ├── services/
│   │   └── api.ts                 # API client (axios + TanStack Query)
│   │
│   ├── App.tsx                    # Root component
│   └── main.tsx                   # Entry point
│
├── vite.config.ts
├── tsconfig.json (strict: true)
└── package.json
```

**Build Output**:
- Vite genera `dist/` con assets bundeado (HTML + JS + CSS)
- Assets servidos vía backend (`/wwwroot`) o CDN

**Responsabilidades**:
- Presentación (componentes visuales)
- Interacción usuario (forms, clicks)
- Sincronización estado servidor (TanStack Query)
- Logging errores (error boundaries)

**Dependencias Externas**:
- Backend API (env var `VITE_API_URL`)

**Validación**:
- TypeScript strict (sin `any`)
- Biome lint/format 100% compliant
- React Doctor zero warnings
- >85% test coverage

---

### C3: Proyecto Worker

**Responsabilidad**: Servicio background para jobs async (recordatorios email, transiciones automáticas)

**Estructura Interna**:
```
worker/
├── Services/
│   ├── EmailReminderService.cs    # Envía recordatorios
│   ├── StateTransitionService.cs  # Transiciones automáticas
│   └── HealthCheckService.cs      # Liveness probe
│
├── Configuration/
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   └── Program.cs                  # Hosted Service setup
│
└── Tests/
    └── Services/
        ├── EmailReminderServiceTests.cs
        └── StateTransitionServiceTests.cs
```

**Patrón**:
- Implementa `IHostedService` (ASP.NET Core)
- Corre loop background (con delay configurable)
- Sin estado en memoria (toda persistencia en MongoDB)
- Escalable horizontalmente (múltiples instancias simultáneas)

**Responsabilidades**:
- Lectura de facturas en estado "pendiente"
- Cálculo de recordatorios (based on fecha, cliente)
- Envío de emails
- Actualización de estado en MongoDB

**Dependencias Externas**:
- MongoDB (misma conexión que backend)
- Email provider

**Validación**:
- Tests inyectan mock MongoDB
- Sin race conditions (transacciones MongoDB)

---

### C4: Paquete Tipos Compartidos

**Responsabilidad**: DTOs, Enums, Interfaces usadas por backend, worker, frontend

**Estructura Interna**:
```
packages/shared/
├── Models/
│   ├── Invoice.cs
│   ├── Client.cs
│   └── Email.cs
│
├── Dtos/
│   ├── InvoiceDto.cs
│   ├── CreateInvoiceDto.cs
│   └── TransitionInvoiceDto.cs
│
├── Enums/
│   └── InvoiceStatus.cs
│
└── shared.csproj
```

**Referenciado por**:
- `backend` (project reference)
- `worker` (project reference)
- Frontend (TypeScript equivalents generadas o manuales)

**Validación**:
- Sin dependencias externas (zero NuGet packages)
- Pure DTOs/enums (no logic)
- Compilable de forma independiente

---

### C5: Docker Infrastructure

**Responsabilidad**: Orquestar todos los servicios en desarrollo y producción

**Estructura Interna**:
```
.
├── docker-compose.yml    # Orquestación
├── Dockerfile            # Multi-stage build
└── .dockerignore         # Exclusiones
```

**Servicios Orquestados**:

| Servicio | Puerto | Imagen/Build | Volumen |
|----------|--------|--------------|---------|
| frontend | 5173 | `./frontend` (build) | - |
| backend | 5000 | `./backend` (build) | - |
| worker | - | `./worker` (build) | - |
| mongo | 27017 | `mongo:8` | `mongo_data:/data/db` |

**Networking**:
- Docker Compose crea network `monolegal_default`
- Backend accede MongoDB via `mongodb://mongo:27017` (DNS interno)
- Frontend accede Backend via env var `VITE_API_URL=http://backend:5000`

**Health Checks**:
- Backend: `curl http://localhost:5000/health`
- Frontend: `curl http://localhost:5173`
- MongoDB: Healthcheck en contenedor

---

## Relaciones Estructurales

```
Frontend (React SPA)
       ↓ HTTP (TanStack Query)
    Backend (Minimal API)
       ↓ MongoDB Driver
    MongoDB (containerizado)
       ↑
    Worker (background service)
```

**Flujo de Datos**:
1. Frontend renderiza lista de facturas
2. Frontend ejecuta `GET /api/invoices` → Backend
3. Backend query MongoDB, retorna DTO
4. Frontend renderiza tabla
5. Usuario clica "Transicionar"
6. Frontend ejecuta `POST /api/invoices/{id}/transition` → Backend
7. Backend valida, actualiza MongoDB
8. Worker detecta cambio, envía email si corresponde
9. Frontend actualiza UI (via TanStack Query revalidation)

---

## Configuración Compartida

### Ambiente Development
```
MONGODB_URI=mongodb://root:example_dev_password@mongo:27017/monolegal_dev
VITE_API_URL=http://localhost:5000
EMAIL_PROVIDER=console  # Solo log, no envía real
LOG_LEVEL=Debug
```

### Ambiente Production
```
MONGODB_URI=mongodb+srv://user:pass@mongodb-cluster.mongodb.net/monolegal_prod
VITE_API_URL=https://api.monolegal.com
EMAIL_PROVIDER=sendgrid
LOG_LEVEL=Information
```

---

## Validación Estructural

### Verificaciones de Compilación
- ✅ Backend: `dotnet build` sin errores
- ✅ Worker: `dotnet build` sin errores
- ✅ Shared: `dotnet build` sin errores
- ✅ Frontend: `npm run build` sin errores

### Verificaciones de Runtime
- ✅ `docker-compose up -d --build` inicia todos los servicios
- ✅ MongoDB accesible desde backend
- ✅ Frontend servido en puerto 5173
- ✅ Backend responde a health check

### Verificaciones Arquitectónicas
- ✅ Backend Domain capa no importa desde Infrastructure
- ✅ Frontend componentes no hacen API calls directas (solo via hooks)
- ✅ Worker es stateless (todos los datos en MongoDB)
- ✅ Shared tiene cero dependencias externas

---

**Status**: ✅ **MODELO ESTRUCTURAL DEFINIDO**
