# ADR 0001: Verificación de conexión a MongoDB (fail-soft) y health check

**Estado**: Aceptada
**Fecha**: 2026-06-24
**Spec**: [specs/004-mongodb-connection](../../specs/004-mongodb-connection/spec.md)

## Contexto

La Fase 0.4 exige que, al levantar el entorno con `docker-compose up`, MongoDB 8 esté
corriendo en el puerto 27017, la base `monolegal_dev` exista y la **conexión esté
verificada desde el backend** de forma observable. El estado previo creaba el `MongoClient`
de forma perezosa, sin verificación al arranque ni un health check que reflejara la
conectividad real (`/health` devolvía `Healthy` aunque la base estuviera caída).

Además, el usuario root de MongoDB se crea en la base `admin` (`MONGO_INITDB_ROOT_*`), por
lo que la cadena de conexión requiere `?authSource=admin`; sin él, el `ping` (comando
pre-auth) tiene éxito pero cualquier query real falla la autenticación: un falso positivo.

## Decisión

1. **Verificación al arranque con política fail-soft**: un `IHostedService`
   (`MongoConnectionVerifier`) ejecuta `{ ping: 1 }` con reintentos acotados (~10s) y
   registra el resultado con Serilog estructurado. Si falla, **no se aborta el arranque**;
   se registra un error clasificado y el estado queda observable vía el health check.
2. **Health check real**: un `IHealthCheck` custom (`MongoHealthCheck`) ejecuta `ping` y
   mapea `Healthy`/`Unhealthy` a `GET /health` (`200`/`503`), consumido por el `healthcheck`
   del contenedor backend.
3. **Configuración tipada y unificada**: `MongoDbOptions` (con pooling y
   `ServerSelectionTimeout` corto) toma la cadena de `MONGODB_URI`, fuente unificada entre
   API y Worker. La URI incluye `?authSource=admin`.
4. **Reporte diferenciado**: `MongoErrorClassifier` distingue *no disponible* vs
   *autenticación* para mensajes claros sin filtrar credenciales.

## Alternativas consideradas

- **Fail-fast** (abortar el arranque ante fallo): produce *crash-loops* en orquestación y
  oculta el diagnóstico tras logs de reinicio. Rechazada a favor de fail-soft + health check
  `Unhealthy`, que es observable y diagnosticable.
- **Paquete `AspNetCore.HealthChecks.MongoDb`**: añade una dependencia para ~15 líneas;
  se prefirió un check custom con control directo del comando y el timeout.
- **Mantener strings mágicos por servicio**: perpetuaba la divergencia de claves entre API
  (`MONGODB_URI`) y Worker (`MongoDB:ConnectionString`), dejando al Worker sin cadena válida.

## Consecuencias

- El backend reporta la conectividad de forma observable al arranque y en runtime.
- `docker-compose` puede depender del estado real de la base vía `/health`.
- Un fallo de MongoDB degrada el servicio de forma controlada en lugar de tumbar el arranque.
- La corrección de `authSource=admin` habilita queries autenticadas reales para la Fase 1.
