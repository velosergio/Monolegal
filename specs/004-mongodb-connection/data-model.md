# Modelo de Datos — Conexión MongoDB (Fase 1)

**Feature**: `004-mongodb-connection` | **Fecha**: 2026-06-24 | **Plan**: [plan.md](plan.md)

Esta fase es de **infraestructura de conexión**, no de modelado de entidades de negocio (clientes, facturas, etc. corresponden a la Fase 1 funcional). El "modelo" aquí describe la **configuración de conexión** y los **estados de conectividad** que la feature introduce y verifica.

---

## Entidad de configuración: `MongoDbOptions`

Opciones tipadas que encapsulan la configuración de conexión, con binding desde variables de entorno / configuración. Vive en `backend/Infrastructure/Configuration/`.

| Campo | Tipo | Origen / Default | Reglas de validación |
|-------|------|------------------|----------------------|
| `ConnectionString` | string | Var. entorno `MONGODB_URI` | Obligatorio; no vacío; formato URI `mongodb://...`. Sin credenciales hardcodeadas en código. |
| `DatabaseName` | string | Default `monolegal_dev` (o derivado de la URI) | Obligatorio; debe resolver exactamente a `monolegal_dev` en el entorno de desarrollo (FR-003). |
| `MaxConnectionPoolSize` | int | Default `100` (configurable) | > 0. Configura el pool del driver (FR-010). |
| `ServerSelectionTimeout` | TimeSpan | Default `5s` (configurable) | > 0 y corto, para distinguir indisponibilidad de fallo de auth con rapidez (FR-007, D4). |

**Notas**:
- La `ConnectionString` se obtiene de la variable de entorno que inyecta `docker-compose` (`MONGODB_URI`), unificada entre API y Worker (research D3). No se persiste ni se loguea su contenido (credenciales).
- `MaxConnectionPoolSize` y `ServerSelectionTimeout` pueden vivir en `appsettings.json` (valores no sensibles); la cadena de conexión, solo en entorno.

---

## Estados de conectividad

La verificación de conexión (`MongoConnectionVerifier`) y el health check (`MongoHealthCheck`) operan sobre el siguiente conjunto de estados:

```text
            ┌──────────────────────────────────────────────┐
            │                  Arranque                     │
            └──────────────────────┬───────────────────────┘
                                   │ ping {ping:1} a monolegal_dev
                  ┌────────────────┴─────────────────┐
                  │                                  │
            éxito │                          fallo   │ (reintentos acotados ~10s)
                  ▼                                  ▼
          ┌───────────────┐              ┌────────────────────────┐
          │   CONECTADO   │              │   FALLO DE CONEXIÓN     │
          │ log: éxito    │              │ clasificar excepción:  │
          │ /health:      │              │  - NO DISPONIBLE       │
          │   Healthy     │              │  - AUTENTICACIÓN       │
          └───────┬───────┘              │ log: error estructurado│
                  │                      │ /health: Unhealthy     │
                  │ (runtime, periódico) └────────────────────────┘
                  ▼
          health check periódico (curl /health) → refleja estado actual
```

| Estado | Disparador | Observabilidad | Requisitos |
|--------|-----------|----------------|------------|
| **Conectado** | `ping` responde `ok` | Log estructurado de éxito (host, base, duración); `/health` → `Healthy` | FR-005, FR-006, FR-008 |
| **Fallo — No disponible** | Timeout de selección de servidor / conexión rechazada | Log Error "servicio no disponible"; `/health` → `Unhealthy` | FR-007, SC-004 |
| **Fallo — Autenticación** | `MongoAuthenticationException` y afines | Log Error "credenciales/autenticación"; `/health` → `Unhealthy` | FR-007, SC-004 |

---

## Recurso gestionado: base de datos `monolegal_dev`

No es una entidad de aplicación, pero es el destino verificado por esta feature.

| Atributo | Valor | Origen |
|----------|-------|--------|
| Nombre | `monolegal_dev` | `init-mongo.js` (`MONGO_INITDB_DATABASE`) + `DatabaseName` |
| Motor | MongoDB 8 | `docker-compose.yml` (`image: mongo:8`) |
| Puerto | 27017 (host ↔ contenedor) | `docker-compose.yml` (`ports`) |
| Persistencia | Volúmenes `mongo_data`, `mongo_config` | `docker-compose.yml` (`volumes`) |
| Colecciones iniciales | `clientes`, `facturas`, `plantillas`, `envios`, `usuarios`, `settings` (+ índices) | `scripts/init-mongo.js` |

> Las colecciones y su seed las crea `init-mongo.js` (ya existente). Esta fase **no** modela su esquema de documentos; solo verifica que la base existe y es alcanzable. El modelado de `Cliente`/`Factura` se realiza en la Fase 1 funcional.

---

## Mapeo a requisitos funcionales

| Requisito | Elemento del modelo |
|-----------|---------------------|
| FR-003 | `DatabaseName` = `monolegal_dev`; recurso `monolegal_dev` |
| FR-004 | `MongoDbOptions.ConnectionString` desde `MONGODB_URI` |
| FR-005 / FR-006 | Estado **Conectado** con log de éxito |
| FR-007 | Estados de **Fallo** clasificados (No disponible / Autenticación) |
| FR-008 | Mapeo Conectado→`Healthy`, Fallo→`Unhealthy` en `/health` |
| FR-009 | Recurso `monolegal_dev` con volúmenes de persistencia |
| FR-010 | `MaxConnectionPoolSize`, `ServerSelectionTimeout` |
| FR-011 | Recurso `monolegal_dev` sobre motor MongoDB 8 |
