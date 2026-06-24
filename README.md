# Monolegal - Plataforma de Gestión de Cobranza

Solución modular para gestión de facturas, seguimiento de pagos y automatización de recordatorios de cobranza.

## Quick Start

```bash
# Requisitos previos
# - Docker Desktop ejecutándose
# - .NET 10 SDK instalado
# - Node.js 18+ instalado

# Setup inicial (< 2 minutos)
docker-compose up -d --build

# Esperar ~30 segundos para que todos los servicios inicien

# Validar que todo funciona
docker-compose ps          # Verificar 4 servicios "Up (healthy)"
curl http://localhost:5000/health
curl http://localhost:5173
```

## Arquitectura

### Estructura de Proyectos

```
.
├── backend/                    # ASP.NET Core 10 (Minimal APIs)
│   ├── Domain/                # Entidades de negocio
│   ├── Application/           # Casos de uso, DTOs
│   ├── Infrastructure/        # MongoDB, email, logging
│   ├── Api/                   # Endpoints HTTP
│   └── Tests/                 # xUnit tests
│
├── frontend/                  # React 19+ + Vite + TypeScript
│   ├── src/
│   │   ├── components/        # Componentes reutilizables
│   │   ├── pages/            # Páginas (invoices, dashboard)
│   │   ├── hooks/            # Custom hooks (data fetching)
│   │   └── services/         # API client, state management
│   └── tests/                # Vitest + Testing Library
│
├── worker/                    # ASP.NET Core Hosted Service
│   ├── Services/             # Background jobs
│   └── Configuration/        # DI setup, Serilog
│
├── packages/shared/          # DTOs, Enums, Interfaces compartidas
│
├── docker-compose.yml        # Orquestación local
├── Dockerfile               # Multi-stage build
└── .dockerignore
```

### Stack Tecnológico

**Backend**:
- ASP.NET Core 10 (Minimal APIs)
- MongoDB Driver
- FluentValidation
- Serilog (structured logging)

**Frontend**:
- React 19+
- Vite (build tool)
- TypeScript strict
- shadcn/ui (components)
- TanStack Query (server state)
- Biome (linting/formatting)

**Infrastructure**:
- Docker Compose (dev + prod parity)
- MongoDB 8
- Multi-stage Dockerfile

## Documentación

- **[Plan de Implementación](specs/001-project-setup/plan.md)** - Decisiones arquitectónicas
- **[Investigación Técnica](specs/001-project-setup/research.md)** - Justificaciones y alternativas
- **[Guía Rápida de Validación](specs/001-project-setup/quickstart.md)** - 30+ validaciones post-setup
- **[Constitución del Proyecto](.specify/memory/constitution.md)** - Principios de desarrollo
- **[Roadmap](roadmap.md)** - 7 fases, 28+ especificaciones

## Desarrollo

### Instalar Dependencias

```bash
# Backend: Se instalan automáticamente via docker-compose
# Frontend: Instaladas durante docker build

# Localmente (opcional):
cd frontend && npm ci
cd backend && dotnet restore
```

### Ejecutar Tests

```bash
# Backend
cd backend && dotnet test

# Frontend
cd frontend && npm run test -- --run
```

### Linting y Formatting

```bash
# Frontend - Biome
cd frontend && npm run lint   # Check
cd frontend && npm run format # Fix

# Backend - dotnet format
cd backend && dotnet format --verify-no-changes  # Check
cd backend && dotnet format                      # Fix
```

### Desarrollo Local con Watchdog

```bash
# Terminal 1: Frontend dev server
cd frontend && npm run dev

# Terminal 2: Backend (debug)
cd backend && dotnet run

# Terminal 3: MongoDB (ya corriendo en docker-compose)
```

## Deployment

### VPS/Producción

```bash
# Build imagen multi-stage
docker build -t monolegal:latest .

# Push a registry
docker tag monolegal:latest your-registry/monolegal:latest
docker push your-registry/monolegal:latest

# Deploy con docker-compose en VPS
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Variables de Entorno

```bash
# .env (desarrollo)
ASPNETCORE_ENVIRONMENT=Development
MONGODB_URI=mongodb://root:example_dev_password@mongo:27017/monolegal_dev
VITE_API_URL=http://localhost:5000
LOG_LEVEL=Debug

# .env.production (producción)
ASPNETCORE_ENVIRONMENT=Production
MONGODB_URI=mongodb+srv://user:pass@mongodb-cluster.mongodb.net/monolegal_prod
VITE_API_URL=https://api.monolegal.com
LOG_LEVEL=Information
```

## Principios de Desarrollo

- **Arquitectura Limpia**: Separación estricta por capas (Domain → Application → Infrastructure → Api)
- **Test-First**: Red-Green-Refactor; >85% cobertura
- **Especificación Dirigida**: GIVEN/WHEN/THEN; especificaciones viven junto al código
- **Observable**: Serilog JSON logging, error boundaries, documentación clara

Ver [Constitución](.specify/memory/constitution.md) para detalles completos.

## Troubleshooting

### "Connection refused" en localhost:5000

```bash
docker-compose ps backend
docker-compose logs backend
docker-compose restart backend
```

### "MongoDB connection timeout"

```bash
# Esperar a que healthcheck pase
sleep 30
docker-compose ps mongo

# Si persiste:
docker-compose restart mongo
```

### Port ya en uso

```bash
# macOS/Linux
lsof -i :5173
kill -9 <PID>

# O cambiar en docker-compose.yml:
# ports:
#   - "5174:5173"  # Usar 5174 localmente
```

## Contributing

1. Crear rama: `git checkout -b feature/tu-feature`
2. Implementar tests primero (TDD)
3. Verificar linting: `npm run lint` (frontend), `dotnet format` (backend)
4. Ejecutar suite completa: `docker-compose up -d && ./run-tests.sh`
5. Push y crear PR

## License

Proprietario - Monolegal 2026
Api/
```

## Domain

* Entidades
* Reglas de negocio
* Interfaces

## Application

* Casos de uso
* DTOs
* Servicios

## Infrastructure

* Mongo
* Email
* Scheduler

## API

* Endpoints
* Swagger
* Auth

---

# Persistencia

Colecciones:

```text
clientes
facturas
settings
plantillas
envios
usuarios
```

---

# Reglas de negocio

## Estados

```text
primerrecordatorio
↓

segundorecordatorio
↓

desactivado
```

Estados fuera del flujo no generan acciones.

---

# Worker

Procesamiento automático mediante:

```text
BackgroundService
```

Características:

* Lee configuración desde Mongo
* Cron configurable
* Pausar/reanudar
* Reintentos configurables

---

# Correos

Proveedores:

* Resend
* SMTP

Plantillas:

* HTML
* WYSIWYG
* Variables dinámicas

---

# Frontend

## Dashboard

* Resumen facturas
* Actividad reciente
* Estado del worker

## Facturas

CRUD completo.

## Configuración

* Correo
* Scheduler
* Reintentos

## Auditoría

* Historial
* Reenvíos

---

# Seguridad

Autenticación:

```text
JWT
```

Rol único:

```text
Administrador
```

---

# Calidad

Frontend:

```text
Biome
React Doctor
```

Backend:

```text
dotnet format
Roslyn
```

Cobertura:

```text
>80%
```

---

# Docker

Servicios:

```text
frontend
backend
worker
mongo
```

---

# Inicio

```bash
docker compose up -d --build
```

URLs:

```text
Frontend:
http://localhost:5173

API:
http://localhost:5000

Swagger:
http://localhost:5000/swagger
```
