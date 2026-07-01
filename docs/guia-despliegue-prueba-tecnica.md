# Guía de despliegue y pruebas — Prueba técnica Monolegal

Esta guía describe cómo **desplegar** y **probar** el sistema desarrollado para la prueba técnica de Monolegal.

El proyecto fue pensado desde el principio para desplegarse con **Docker Compose**, pero también puede ejecutarse **sin contenedores** (servicios .NET y frontend en local). Documentación adicional del repositorio:

- Repositorio: [https://github.com/velosergio/monolegal](https://github.com/velosergio/monolegal)
- [Configuración local](./setup.md)
- [Guía de despliegue a producción](./deployment.md)
- [README — quick start y troubleshooting](../README.md)

---

## Requisitos mínimos

| Recurso | Mínimo recomendado |
|---------|-------------------|
| RAM | 4 GB libres (8 GB recomendado con Docker) |
| Disco | ~2 GB (imágenes Docker + dependencias) |
| Red | Acceso a internet para clonar e instalar dependencias |
| SO | Windows 10/11, macOS 12+, o Linux (Ubuntu 22.04+) |

**Tiempo estimado de puesta en marcha**

- Con Docker: **< 2 minutos** (tras tener Docker en ejecución)
- Sin Docker: **5–10 minutos** (instalar SDKs, dependencias y levantar servicios)

---

## Software necesario

### Camino Docker (recomendado)

| Herramienta | Versión | Verificación |
|-------------|---------|--------------|
| **Docker Desktop** (o Docker Engine + Compose) | Docker 20.10+ / Compose 2.0+ | `docker --version` |
| | | `docker compose version` |
| **Git** | Cualquier versión reciente | `git --version` |

### Camino local (sin contenedores de aplicación)

| Herramienta | Versión | Verificación |
|-------------|---------|--------------|
| **.NET SDK** | **10.0.301** (fijado en `global.json`) | `dotnet --version` |
| **Node.js** | **22+** (`.node-version`: 26.x) | `node --version` |
| **npm** | 9+ | `npm --version` |
| **MongoDB** | 8.x | Instancia local o solo el contenedor `mongo` |
| **Git** | Cualquier versión reciente | `git --version` |

### Para ejecutar pruebas

| Herramienta | Uso |
|-------------|-----|
| .NET SDK 10 | Backend y worker (`dotnet test`) |
| Node.js 22+ | Frontend (`npm run test:run`) y orquestador (`npm run test:all`) |
| Playwright | E2E — instalar navegadores una vez: `cd frontend && npx playwright install` |
| MongoDB en ejecución | Requerido para tests de integración y E2E |

---

## Configuración de entorno

### 1. Clonar el repositorio

```bash
git clone https://github.com/velosergio/monolegal.git
cd monolegal
```

### 2. Variables de entorno

Copia el archivo de ejemplo y ajústalo según el modo de despliegue:

```bash
cp .env.example .env
```

**Variables principales** (raíz del proyecto):

| Variable | Desarrollo (Docker) | Desarrollo (local) | Descripción |
|----------|---------------------|--------------------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Development` | Habilita Swagger en el backend |
| `MONGODB_URI` | `mongodb://root:example_dev_password@mongo:27017/monolegal_dev?authSource=admin` | `mongodb://root:example_dev_password@localhost:27017/monolegal_dev?authSource=admin` | **Obligatoria**. Debe incluir `?authSource=admin` |
| `VITE_API_URL` | `http://localhost:5000` | `http://localhost:5155` | URL de la API para el frontend |
| `LOG_LEVEL` | `Debug` | `Debug` | Nivel de logs (Serilog) |
| `EMAIL_PROVIDER` | `console` | `console` | En desarrollo, los correos se imprimen en consola |

Para el frontend, opcionalmente:

```bash
cp frontend/.env.example frontend/.env
```

> **Importante:** Las credenciales (MongoDB, SMTP, Resend) **solo** deben vivir en variables de entorno. No commitear `.env` con valores reales.

### 3. Servicios del stack

El sistema consta de **cuatro servicios**:

| Servicio | Rol |
|----------|-----|
| **frontend** | Panel React 19 + Vite |
| **backend** | API ASP.NET Core 10 (Minimal APIs) |
| **worker** | Procesamiento automático de recordatorios |
| **mongo** | Base de datos MongoDB 8 |

---

## Paso a paso: Docker

### 1. Iniciar Docker Desktop

Asegúrate de que Docker esté en ejecución antes de continuar.

### 2. Levantar el stack completo

```bash
docker compose up -d --build
```

Docker Compose carga automáticamente `docker-compose.yml` y `docker-compose.override.yml` (puertos publicados y entorno `Development`).

### 3. Esperar a que los servicios estén sanos

Espera **~30 segundos** (MongoDB tarda en pasar el healthcheck).

```bash
docker compose ps
```

**Salida esperada:** 4 servicios con estado `Up (healthy)` (frontend, backend, mongo; worker en `Up`).

### 4. Verificar el despliegue

```bash
# Health check del backend (ping real a MongoDB)
curl http://localhost:5000/health

# API de facturas
curl http://localhost:5000/api/invoices
```

Abre en el navegador:

| Recurso | URL |
|---------|-----|
| **Frontend (panel)** | http://localhost:5173 |
| **API** | http://localhost:5000 |
| **Swagger UI** | http://localhost:5000/swagger |
| **Health check** | http://localhost:5000/health |

### 5. Probar el sistema

**Verificación manual rápida**

1. Abre http://localhost:5173
2. Navega por Dashboard, Facturas, Clientes y Configuración
3. Comprueba que el sidebar muestra el acceso **API (Swagger)**
4. Crea/edita una factura y verifica que persiste tras recargar

**Suite de pruebas automatizadas (con stack Docker)**

```bash
# Todas las suites: backend + worker + frontend + E2E
npm run test:all

# Solo E2E contra el stack Docker (backend en :5000)
cd frontend && npm run test:e2e:docker
```

**Suites por separado**

```bash
cd backend  && dotnet test          # xUnit — dominio, aplicación, infraestructura
cd worker   && dotnet test          # xUnit — worker
cd frontend && npm run test:run     # Vitest — componentes React
cd frontend && npm run test:e2e:docker  # Playwright — E2E
```

### 6. Detener el stack

```bash
docker compose down          # Detiene contenedores
docker compose down -v       # Detiene y elimina volúmenes (borra datos de MongoDB)
```

---

## Paso a paso: Local

Útil para depurar backend, worker o frontend sin reconstruir imágenes Docker.

### 1. MongoDB

Opción mínima: levantar **solo MongoDB** con Docker:

```bash
docker compose up -d mongo
```

Ajusta `MONGODB_URI` en `.env` o en el entorno de la terminal:

```bash
# Windows PowerShell
$env:MONGODB_URI = "mongodb://root:example_dev_password@localhost:27017/monolegal_dev?authSource=admin"
$env:ASPNETCORE_ENVIRONMENT = "Development"

# Linux / macOS
export MONGODB_URI="mongodb://root:example_dev_password@localhost:27017/monolegal_dev?authSource=admin"
export ASPNETCORE_ENVIRONMENT=Development
```

### 2. Instalar dependencias

```bash
cd frontend && npm ci
cd ../backend && dotnet restore
cd ../worker && dotnet restore
```

### 3. Levantar los servicios (3 terminales)

**Terminal 1 — Backend API** (escucha en `:5155`):

```bash
cd backend
dotnet run --project Api
```

**Terminal 2 — Worker**:

```bash
cd worker
dotnet run
```

**Terminal 3 — Frontend** (Vite en `:5173`):

```bash
cd frontend
npm run dev
```

### 4. URLs en modo local

| Recurso | URL |
|---------|-----|
| **Frontend** | http://localhost:5173 |
| **API** | http://localhost:5155 |
| **Swagger UI** | http://localhost:5155/swagger |
| **Health check** | http://localhost:5155/health |

> En local, Vite hace proxy de `/api` y `/swagger` hacia el backend en `:5155`.

### 5. Verificar y probar

```bash
curl http://localhost:5155/health
curl http://localhost:5155/api/invoices
```

**Pruebas en modo local**

```bash
# Backend y worker (requieren MONGODB_URI apuntando a localhost)
cd backend && dotnet test
cd worker && dotnet test

# Frontend (Vitest, sin MongoDB)
cd frontend && npm run test:run

# E2E (requiere backend en :5155 y MongoDB)
cd frontend && npm run test:e2e
```

Para la suite completa con `npm run test:all`, el backend debe estar en **http://localhost:5155** (perfil `dotnet run`).

---

## Notas

### Puertos

| Servicio | Docker | Local (`dotnet run`) |
|----------|--------|----------------------|
| Frontend | 5173 | 5173 |
| Backend | 5000 | 5155 |
| MongoDB | 27017 | 27017 |

### Swagger

- Disponible **solo** con `ASPNETCORE_ENVIRONMENT=Development`
- En producción está deshabilitado por defecto (ver [deployment.md](./deployment.md))
- Documentación estática alternativa: [api-reference.md](./api-reference.md) y colección [postman/](./postman/)

### Correo electrónico

En desarrollo, `EMAIL_PROVIDER=console` imprime los correos en los logs del backend/worker. Para SMTP o Resend, configura las variables `Email__*` descritas en `.env.example`.

### Estado de las pruebas

El repositorio incluye **686 pruebas** (backend, worker, frontend y E2E). El comando unificado `npm run test:all` ejecuta las cuatro suites y devuelve código de salida `0` solo si todas pasan (apta para CI).

### Solución de problemas frecuentes

| Problema | Solución |
|----------|----------|
| `Connection refused` en `:5000` | `docker compose ps backend` → `docker compose logs backend` → `docker compose restart backend` |
| Timeout de MongoDB | Esperar 30 s; `docker compose ps mongo`; `docker compose restart mongo` |
| Puerto en uso (5173, 5000, 27017) | Liberar el proceso o cambiar el mapeo en `docker-compose.override.yml` |
| Falta `MONGODB_URI` | Definir la variable antes de `dotnet run` o en `.env` |
| E2E fallan | Confirmar backend levantado (`:5000` con Docker, `:5155` en local) y MongoDB accesible |
| Playwright sin navegadores | `cd frontend && npx playwright install` |

### Documentación relacionada

- [Arquitectura](./architecture.md)
- [Modelo de datos](./data-model.md)
- [Inyección de dependencias](./dependency-injection.md)
- [Validación post-setup (30+ checks)](../specs/001-project-setup/quickstart.md)
- [Test runner unificado](../specs/024-test-runner-unificado/quickstart.md)

### Producción

Para despliegue en VPS o entorno productivo, consulta [deployment.md](./deployment.md) (`docker-compose.prod.yml`, secretos por entorno, MongoDB gestionado).

---

**Monolegal** — Plataforma de gestión de cobranza · 2026
