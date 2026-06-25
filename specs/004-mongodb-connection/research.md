# Research — Conexión MongoDB (Fase 0)

**Feature**: `004-mongodb-connection` | **Fecha**: 2026-06-24 | **Plan**: [plan.md](plan.md)

Resolución de las incógnitas técnicas del Contexto Técnico del plan. Cada decisión es coherente con la constitución (Arquitectura Limpia, observabilidad, sin secrets embebidos) y con el estado real auditado del repositorio.

---

## D1 — Mecanismo de verificación de conexión al arranque

- **Decisión**: Implementar un `IHostedService` (`MongoConnectionVerifier`) registrado en `AddInfrastructure`, que en `StartAsync` ejecuta el comando `{ ping: 1 }` contra la base `monolegal_dev` y registra el resultado con Serilog (éxito/fallo, duración, host, base). Política **fail-soft con reintentos acotados**: reintenta el `ping` con backoff durante una ventana corta (hasta ~10s, alineado con SC-003) antes de declarar fallo; el fallo se registra como error estructurado y deja la app arrancada pero con el health check en estado `Unhealthy`.
- **Rationale**: Un `IHostedService` tiene responsabilidad única y se integra al ciclo de vida del host (SOLID/Clean Architecture). El `depends_on: condition: service_healthy` de `docker-compose` ya garantiza que Mongo está sano antes de arrancar el backend, pero los reintentos cubren el caso de ejecución fuera de Compose (dev local directo) y carreras de arranque. Fail-soft evita un crash-loop del contenedor y permite que `/health` reporte el problema de forma observable, en lugar de un fallo opaco.
- **Alternativas consideradas**:
  - *Ping inline antes de `app.Run()`*: más simple, pero mezcla responsabilidades en `Program.cs` y complica el testeo aislado. Rechazado por testabilidad.
  - *Fail-fast (lanzar excepción y abortar arranque)*: más estricto, pero produce crash-loops en orquestación y oculta el diagnóstico tras logs de reinicio. Rechazado a favor de fail-soft + health check `Unhealthy`, que es observable y diagnosticable (FR-007).
  - *Sin verificación (lazy)*: estado actual; viola FR-005/FR-006. Rechazado.

---

## D2 — Health check de conectividad MongoDB

- **Decisión**: Implementar un `IHealthCheck` custom (`MongoHealthCheck`) que ejecuta `{ ping: 1 }` con un timeout corto y devuelve `Healthy`/`Unhealthy`, registrado vía `AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb")`. El endpoint `/health` ya mapeado en `Program.cs` pasa a reflejar el estado real de la base de datos.
- **Rationale**: Un check custom evita añadir el paquete `AspNetCore.HealthChecks.MongoDb` (menos superficie de dependencias y control directo del comando y del timeout). Reutiliza `IMongoDatabase` ya registrado por inyección. Cumple FR-008 y hace que el healthcheck de `docker-compose` del backend (`curl /health`) sea representativo de la conectividad a datos.
- **Alternativas consideradas**:
  - *Paquete `AspNetCore.HealthChecks.MongoDb`*: funcional y mantenido, pero introduce una dependencia adicional para algo que son ~15 líneas. Rechazado por simplicidad; reconsiderable si se requieren chequeos más ricos.
  - *Dejar `AddHealthChecks()` sin check de Mongo*: estado actual; `/health` da falsos positivos. Rechazado (viola FR-008).

---

## D3 — Configuración tipada y unificación de la clave de conexión

- **Decisión**: Introducir `MongoDbOptions` (`ConnectionString`, `DatabaseName`, `MaxConnectionPoolSize`, `ServerSelectionTimeout`) con binding desde configuración. Unificar la fuente: aceptar la variable de entorno `MONGODB_URI` (la que inyecta `docker-compose`) como `ConnectionString`, con `DatabaseName` por defecto `monolegal_dev` (derivable de la URI si la incluye). Alinear `worker/Program.cs` para que resuelva la **misma** clave (`MONGODB_URI`) en lugar de `MongoDB:ConnectionString`, eliminando la inconsistencia detectada.
- **Rationale**: La constitución exige configuración por variables de entorno sin secrets embebidos e inyección por constructor con DI centralizada. El binding tipado reemplaza los strings mágicos dispersos (`configuration["MONGODB_URI"]`, fallback `localhost`, clave divergente en Worker) por un único contrato. Resuelve un bug real: hoy el Worker buscaría `MongoDB:ConnectionString` mientras Compose provee `MONGODB_URI`, dejando al Worker sin cadena válida.
- **Alternativas consideradas**:
  - *Mantener strings mágicos por servicio*: menor cambio, pero perpetúa la inconsistencia y el riesgo de fallo silencioso en el Worker. Rechazado.
  - *`appsettings.json` con valores por defecto de conexión*: útil para defaults locales, pero la fuente canónica en contenedores es la variable de entorno; se mantiene `appsettings` solo para valores no sensibles (pool/timeout) y la URI llega por entorno. Aceptado parcialmente (sin credenciales en `appsettings`).

---

## D4 — Pooling de conexiones y cierre limpio

- **Decisión**: Construir el `MongoClient` desde `MongoClientSettings` derivado de la URI, fijando explícitamente `MaxConnectionPoolSize` (p. ej. 100, configurable) y `ServerSelectionTimeout` corto (p. ej. 5s) para que los fallos de disponibilidad se reporten rápido y de forma diferenciable. El `IMongoClient` permanece como singleton (el driver gestiona el pool internamente); el apagado limpio se apoya en el ciclo de vida del host y en `Log.CloseAndFlush()` ya presente.
- **Rationale**: La constitución exige "MongoDB connection pooling configurado; shutdown hooks limpios". Un `ServerSelectionTimeout` corto es clave para FR-007: distingue rápidamente "servicio no disponible" (timeout de selección de servidor) de "credenciales incorrectas" (error de autenticación del driver), permitiendo mensajes diferenciados.
- **Alternativas consideradas**:
  - *Settings por defecto del driver*: el pooling existe por defecto, pero el timeout de selección por defecto (30s) ralentiza el diagnóstico de fallos y empuja contra SC-003. Rechazado a favor de timeouts explícitos.

---

## D5 — Reporte de errores diferenciado (disponibilidad vs autenticación)

- **Decisión**: En `MongoConnectionVerifier` y en los mensajes del health check, capturar y clasificar las excepciones del driver: `MongoAuthenticationException` (y afines) → mensaje de "credenciales/autenticación"; `TimeoutException`/`MongoConnectionException`/`MongoServerException` por selección de servidor → mensaje de "servicio no disponible/inalcanzable". Cada caso se registra con Serilog en un nivel apropiado (Error) con contexto estructurado, sin volcar credenciales.
- **Rationale**: Cumple FR-007 y SC-004 (mensaje claro y diferenciable en el 100% de los fallos simulados). La clasificación por tipo de excepción del driver es estable y testeable.
- **Alternativas consideradas**:
  - *Mensaje genérico único*: insuficiente para FR-007. Rechazado.

---

## D6 — Estrategia de testing (Test-First)

- **Decisión**: Tres niveles en `backend/Tests/Infrastructure/`:
  1. **Unit** — `MongoDbOptionsTests`: el binding mapea `MONGODB_URI` y defaults (`monolegal_dev`, pool/timeout) correctamente; valida la clasificación de error de D5 con dobles/entradas controladas.
  2. **Integración** — `MongoConnectionTests`: contra una instancia real de MongoDB (la de `docker-compose` o un contenedor efímero), el `ping` a `monolegal_dev` resuelve y la base existe/escribe-lee.
  3. **Integración** — `MongoHealthCheckTests`: `Healthy` con Mongo disponible; `Unhealthy` (con timeout corto) cuando apunta a un host/puerto inexistente.
- **Rationale**: La constitución fuerza Test-First y cobertura de contratos de repositorio/endpoints. Los tests se escriben antes de la implementación (Red-Green-Refactor).
- **Gating de integración**: los tests que requieren Mongo real se marcan por categoría/trait (`Integration`) para poder ejecutarse en CI con el servicio levantado y omitirse en entornos sin Mongo, **sin** usar skips prohibidos por la constitución para tests unitarios. La URI de prueba proviene de variable de entorno (misma fuente que la app).
- **Alternativas consideradas**:
  - *Solo mocks de `IMongoClient`*: rápido pero no prueba conectividad real (objetivo central de la spec). Se combina con integración real, no se sustituye.
  - *Mongo en memoria*: no hay equivalente oficial fiable para el driver; se usa contenedor real. Rechazado.

---

## Resumen de decisiones

| ID | Decisión | Requisitos cubiertos |
|----|----------|----------------------|
| D1 | `IHostedService` de verificación con ping + reintentos acotados, fail-soft | FR-005, FR-006, SC-003 |
| D2 | `IHealthCheck` custom de Mongo en `/health` | FR-008, SC-001 |
| D3 | `MongoDbOptions` tipado + unificación de `MONGODB_URI` (API y Worker) | FR-004, SC-007 |
| D4 | `MongoClientSettings` con pooling y `ServerSelectionTimeout` corto | FR-010, FR-007 |
| D5 | Clasificación de error disponibilidad vs autenticación | FR-007, SC-004 |
| D6 | Tests unitarios + integración (ping, health check, binding) | Test-First, SC-002/004/005 |

**Sin NEEDS CLARIFICATION pendientes.** Listo para Fase 1.
