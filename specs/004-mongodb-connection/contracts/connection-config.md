# Contrato — Configuración de Conexión MongoDB

**Feature**: `004-mongodb-connection` | **Plan**: [../plan.md](../plan.md)

Define el contrato de configuración que API y Worker DEBEN respetar para conectarse a MongoDB de forma unificada y sin secrets embebidos (FR-004, FR-010, research D3/D4).

## Fuente de configuración

| Clave | Fuente canónica | Default | Notas |
|-------|-----------------|---------|-------|
| Cadena de conexión | Var. entorno `MONGODB_URI` | — (obligatoria) | Inyectada por `docker-compose.yml`; nunca hardcodeada en código |
| Nombre de base | `MongoDbOptions:DatabaseName` o derivado de la URI | `monolegal_dev` | Debe resolver a `monolegal_dev` en desarrollo |
| Tamaño máximo de pool | `MongoDbOptions:MaxConnectionPoolSize` | `100` | Valor no sensible; puede vivir en `appsettings.json` |
| Timeout selección servidor | `MongoDbOptions:ServerSelectionTimeout` | `00:00:05` | Corto, para diagnóstico rápido de indisponibilidad |

### Formato de `MONGODB_URI` (entorno de desarrollo, desde compose)

```
mongodb://<usuario>:<password>@mongo:27017/monolegal_dev
```

- En `docker-compose`, el host es `mongo` (nombre de servicio en la red `monolegal-network`); desde el host de desarrollo directo, `localhost:27017`.
- Las credenciales provienen de variables de entorno (`MONGO_INITDB_ROOT_*`) — no se versionan valores reales (`.env` ignorado; `.env.example` documenta).

## Contrato de binding (`MongoDbOptions`)

```text
MongoDbOptions
├─ ConnectionString : string   // = MONGODB_URI   (obligatorio, no logueado)
├─ DatabaseName     : string   // = "monolegal_dev" (default)
├─ MaxConnectionPoolSize : int // = 100
└─ ServerSelectionTimeout : TimeSpan // = 5s
```

- El registro DI (`AddInfrastructure`) DEBE construir `MongoClient` desde `MongoClientSettings` derivado de `ConnectionString`, aplicando `MaxConnectionPoolSize` y `ServerSelectionTimeout`.
- `IMongoClient` se registra como **singleton** (el driver gestiona el pool). `IMongoDatabase` se resuelve con `DatabaseName`.

## Unificación API ↔ Worker

| Servicio | Estado actual (bug) | Estado objetivo |
|----------|---------------------|-----------------|
| API (`backend/Api`) | lee `ConnectionStrings:MongoDB` → `MONGODB_URI` → fallback `localhost` | lee `MongoDbOptions` (fuente `MONGODB_URI`) |
| Worker (`worker`) | lee `MongoDB:ConnectionString` (no resuelve `MONGODB_URI` de compose) | lee la **misma** fuente `MONGODB_URI` |

> Resolver esta divergencia es parte del contrato: ambos servicios DEBEN obtener la cadena de la misma variable de entorno (`MONGODB_URI`).

## Criterios de aceptación verificables

- El binding produce `DatabaseName == "monolegal_dev"` por defecto (SC-002).
- Ninguna credencial ni cadena aparece hardcodeada en el código; el 100% proviene de entorno (SC-007).
- API y Worker resuelven la cadena desde `MONGODB_URI` (FR-004; elimina el fallo silencioso del Worker).
