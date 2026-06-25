# Contrato — Endpoint de Salud `/health`

**Feature**: `004-mongodb-connection` | **Plan**: [../plan.md](../plan.md)

Define el contrato del endpoint de verificación de salud que **refleja la conectividad real con MongoDB** (FR-008). El endpoint ya está mapeado en `backend/Api/Program.cs`; esta fase le añade un check específico de MongoDB.

## Endpoint

```
GET /health
```

- **Autenticación**: ninguna (endpoint de liveness/readiness de infraestructura).
- **Consumidores**: healthcheck del contenedor backend en `docker-compose.yml` (`curl -fs http://localhost:5000/health`), `depends_on: condition: service_healthy`, y operadores.

## Respuestas

### 200 OK — Base de datos alcanzable

```
HTTP/1.1 200 OK
Content-Type: text/plain

Healthy
```

Condición: el check `mongodb` ejecutó `{ ping: 1 }` contra `monolegal_dev` con éxito dentro del timeout.

### 503 Service Unavailable — Base de datos no alcanzable

```
HTTP/1.1 503 Service Unavailable
Content-Type: text/plain

Unhealthy
```

Condición: el check `mongodb` falló (servicio no disponible o error de autenticación). El detalle clasificado (No disponible / Autenticación) se registra en los logs estructurados, **no** se expone en el cuerpo para no filtrar información sensible.

## Checks registrados

| Nombre | Tipo | Comportamiento |
|--------|------|----------------|
| `mongodb` | `MongoHealthCheck` (`IHealthCheck`) | Ejecuta `{ ping: 1 }` sobre `IMongoDatabase` con timeout corto; `Healthy`/`Unhealthy` |

> Si en el futuro se requiere un cuerpo JSON detallado por check, se puede usar un `ResponseWriter` personalizado; el contrato mínimo de esta fase es el código de estado HTTP (200/503) que consume el healthcheck del contenedor.

## Criterios de aceptación verificables

- Con MongoDB disponible, `GET /health` → `200 Healthy` (HU3, escenario 1; SC-001).
- Con MongoDB caído o credenciales inválidas, `GET /health` → `503 Unhealthy` (HU3, escenario 3; FR-007).
- El healthcheck del contenedor backend pasa a `healthy` solo cuando la base es alcanzable.
