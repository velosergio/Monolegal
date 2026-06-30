# Configuración local — Monolegal

Guía para levantar el proyecto completo (backend, worker, frontend y MongoDB) en un entorno de
desarrollo. Al terminar tendrás la API en marcha, el panel en el navegador y Swagger disponible.

## Prerrequisitos

- **Docker Desktop** en ejecución (para el camino con Docker Compose).
- **.NET 10 SDK** (`global.json` fija la versión exacta) — para ejecutar backend/worker sin Docker.
- **Node.js 22+** (`.node-version`) — para el frontend y el tooling.

## Opción A — Todo con Docker Compose (recomendada)

```bash
# 1. Copia las variables de entorno y ajústalas
cp .env.example .env

# 2. Levanta los cuatro servicios (frontend, backend, worker, mongo)
docker-compose up -d --build

# 3. Espera ~30 s y verifica que están sanos
docker-compose ps          # 4 servicios "Up (healthy)"
curl http://localhost:5000/health
```

URLs resultantes:

| Servicio | URL |
|----------|-----|
| Frontend | http://localhost:5173 |
| API | http://localhost:5000 |
| Swagger UI | http://localhost:5000/swagger |
| Health | http://localhost:5000/health |

## Opción B — Servicios en local (sin contenedores)

Útil para depurar. Requiere una instancia de MongoDB accesible (puedes levantar solo Mongo con
`docker-compose up -d mongo`).

```bash
# Terminal 1 — Backend API (Development, escucha en :5155)
cd backend && dotnet run --project Api

# Terminal 2 — Worker
cd worker && dotnet run

# Terminal 3 — Frontend (Vite dev server en :5173)
cd frontend && npm ci && npm run dev
```

En esta opción Swagger queda en http://localhost:5155/swagger y el frontend lo abre vía el proxy de
desarrollo (`/swagger`).

## Variables de entorno

Definidas en `.env` (raíz) y, para el frontend, en `frontend/.env` (ver `frontend/.env.example`).
Las credenciales **solo** viven en variables de entorno, nunca en la base de datos.

| Variable | Ejemplo | Descripción |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Development` | Entorno del backend/worker. Swagger se habilita solo en `Development` |
| `MONGODB_URI` | `mongodb://root:...@mongo:27017/monolegal_dev?authSource=admin` | **Obligatoria**. URI de conexión (incluye `?authSource=admin` para el usuario root) |
| `VITE_API_PROXY_TARGET` | `http://backend:5000` | Destino del proxy `/api` del frontend (Docker) |
| `VITE_SWAGGER_URL` | `/swagger` | URL del botón de Swagger en el sidebar (por defecto `/swagger`) |
| `Email__*` / `Email__Resend__*` | — | Configuración y **secretos** del proveedor de correo (ver `.env.example`) |

## Verificación rápida

```bash
curl http://localhost:5000/health                 # 200 Healthy
curl http://localhost:5000/api/invoices           # lista paginada de facturas
```

Abre http://localhost:5173 y comprueba que el sidebar muestra el acceso **API (Swagger)**.

## Pruebas

```bash
npm run test:all      # backend + worker + frontend + E2E (ver specs/024-test-runner-unificado)
```

## Solución de problemas

- **"La cadena de conexión a MongoDB es obligatoria"**: falta `MONGODB_URI` en el entorno.
- **Swagger no carga**: solo está habilitado en `Development`; en producción está deshabilitado por
  defecto (ver [deployment.md](./deployment.md)).
- Más casos en la sección *Troubleshooting* del [README](../README.md).
