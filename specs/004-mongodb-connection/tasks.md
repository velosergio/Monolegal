---
description: "Lista de tareas para la implementación de la feature 004-mongodb-connection"
---

# Tareas: Conexión MongoDB

**Entrada**: Documentos de diseño de `/specs/004-mongodb-connection/`

**Prerrequisitos**: [plan.md](plan.md) (requerido), [spec.md](spec.md) (historias de usuario), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/)

**Tests**: INCLUIDOS. La constitución (Principio IV — Test-First, NO NEGOCIABLE) y la decisión D6 de research lo exigen. Los tests se escriben primero y deben FALLAR antes de implementar (Red-Green-Refactor).

**Organización**: Tareas agrupadas por historia de usuario para implementación y verificación independientes.

## Formato: `[ID] [P?] [Story] Descripción`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias entre sí)
- **[Story]**: Historia de usuario a la que pertenece (US1, US2, US3)
- Cada tarea incluye la ruta de archivo exacta

## Convenciones de Rutas

- Backend (.NET, Arquitectura Limpia): `backend/Api/`, `backend/Infrastructure/`, `backend/Tests/`
- Worker: `worker/`
- Infraestructura: `docker-compose.yml`, `scripts/init-mongo.js`, `.env.example`

---

## Phase 1: Setup (Infraestructura Compartida)

**Propósito**: Preparación del entorno de configuración y de pruebas.

- [x] T001 Crear `.env` a partir de `.env.example` y verificar que `MONGODB_URI` está definido en la raíz del repositorio (referencia: [contracts/connection-config.md](contracts/connection-config.md))
- [x] T002 [P] Crear carpeta `backend/Tests/Infrastructure/` y definir el trait/categoría `Integration` (xUnit `[Trait("Category","Integration")]`) para separar tests que requieren MongoDB real (referencia: research D6)

---

## Phase 2: Foundational (Prerrequisitos Bloqueantes)

**Propósito**: Configuración tipada y de DI que US3 y la unificación del Worker requieren.

**⚠️ CRÍTICO**: Ninguna tarea de US3 puede comenzar hasta completar esta fase.

- [x] T003 [P] Crear opciones tipadas `MongoDbOptions` (`ConnectionString`, `DatabaseName` default `monolegal_dev`, `MaxConnectionPoolSize` default 100, `ServerSelectionTimeout` default 5s) en `backend/Infrastructure/Configuration/MongoDbOptions.cs` (referencia: [data-model.md](data-model.md), [contracts/connection-config.md](contracts/connection-config.md))
- [x] T004 Refactorizar `backend/Infrastructure/Configuration/DependencyInjection.cs` para bindear `MongoDbOptions` desde `MONGODB_URI`, construir `IMongoClient` (singleton) desde `MongoClientSettings` con `MaxConnectionPoolSize` y `ServerSelectionTimeout`, y resolver `IMongoDatabase` con `DatabaseName` (elimina strings mágicos y fallback `localhost`) (depende de T003)

**Checkpoint**: Configuración de conexión tipada y centralizada lista. US3 puede comenzar.

---

## Phase 3: User Story 1 - Servicio de Base de Datos en Ejecución (Priority: P1) 🎯 MVP

**Goal**: MongoDB 8 corriendo y accesible en el puerto 27017 tras `docker-compose up`.

**Independent Test**: `docker-compose up -d mongo` deja el servicio `healthy`; `ping` responde `ok`; el puerto 27017 acepta conexiones desde el host (quickstart escenario 1).

- [x] T005 [US1] Verificar en `docker-compose.yml` el servicio `mongo` (`image: mongo:8`, `ports: 27017:27017`, `volumes: mongo_data`/`mongo_config`, `healthcheck` con `mongosh ping`) y corregir cualquier desviación respecto a [contracts/connection-config.md](contracts/connection-config.md)
- [x] T006 [US1] Ejecutar quickstart escenario 1 (`docker-compose up -d mongo`, `docker-compose ps mongo`, `mongosh --eval "db.runCommand({ping:1})"`) y confirmar estado `healthy` y puerto 27017 accesible (referencia: [quickstart.md](quickstart.md))

**Checkpoint**: Servicio MongoDB activo y verificable de forma independiente.

---

## Phase 4: User Story 2 - Base de Datos de Desarrollo Disponible (Priority: P1)

**Goal**: La base `monolegal_dev` existe y está lista para lectura/escritura sin pasos manuales.

**Independent Test**: `mongosh monolegal_dev --eval "db.getName(); db.getCollectionNames()"` devuelve `monolegal_dev` y las colecciones inicializadas (quickstart escenario 2).

- [x] T007 [US2] Verificar en `scripts/init-mongo.js` que crea la base `monolegal_dev` con las colecciones (`clientes`, `facturas`, `plantillas`, `envios`, `usuarios`, `settings`) e índices, y que `MONGO_INITDB_DATABASE: monolegal_dev` está en `docker-compose.yml`
- [x] T008 [US2] Ejecutar quickstart escenario 2 y confirmar `db.getName()` → `monolegal_dev` con las colecciones presentes sin pasos manuales (referencia: [quickstart.md](quickstart.md))

**Checkpoint**: Base `monolegal_dev` disponible y verificable de forma independiente.

---

## Phase 5: User Story 3 - Conexión Verificada desde el Backend (Priority: P1)

**Goal**: El backend verifica la conexión al arranque de forma observable (log estructurado) y la expone mediante un health check real en `/health`, con reporte de fallo diferenciado.

**Independent Test**: con Mongo arriba, los logs del backend registran conexión exitosa a `monolegal_dev` y `GET /health` → `200 Healthy`; con Mongo caído/credenciales inválidas → `503 Unhealthy` y log de error clasificado (quickstart escenarios 3 y 4).

### Tests para User Story 3 (escribir PRIMERO, deben FALLAR) ⚠️

- [x] T009 [P] [US3] Test unitario de binding de `MongoDbOptions` (mapeo de `MONGODB_URI`, defaults `monolegal_dev`/pool/timeout) en `backend/Tests/Infrastructure/MongoDbOptionsTests.cs` (contrato: [contracts/connection-config.md](contracts/connection-config.md))
- [x] T010 [P] [US3] Test unitario de clasificación de error (disponibilidad vs autenticación) en `backend/Tests/Infrastructure/MongoErrorClassificationTests.cs` (referencia: research D5, FR-007)
- [x] T011 [P] [US3] Test de integración `[Category=Integration]` de conexión/ping a `monolegal_dev` en `backend/Tests/Infrastructure/MongoConnectionTests.cs` (FR-005)
- [x] T012 [P] [US3] Test de integración `[Category=Integration]` del health check (`Healthy` con Mongo disponible; `Unhealthy` apuntando a host inexistente con timeout corto) en `backend/Tests/Infrastructure/MongoHealthCheckTests.cs` (contrato: [contracts/health-endpoint.md](contracts/health-endpoint.md))

### Implementación para User Story 3

- [x] T013 [P] [US3] Implementar clasificación de excepciones del driver (auth vs indisponibilidad) como helper en `backend/Infrastructure/Configuration/MongoErrorClassifier.cs` (research D5; hace pasar T010)
- [x] T014 [P] [US3] Implementar `MongoHealthCheck` (`IHealthCheck`) que ejecuta `{ping:1}` con timeout corto y devuelve `Healthy`/`Unhealthy` en `backend/Infrastructure/Configuration/MongoHealthCheck.cs` (contrato: [contracts/health-endpoint.md](contracts/health-endpoint.md); hace pasar T012)
- [x] T015 [US3] Implementar `MongoConnectionVerifier` (`IHostedService`) que en `StartAsync` ejecuta `ping` con reintentos acotados (~10s), registra resultado con Serilog estructurado (host, base, duración) y usa `MongoErrorClassifier` ante fallo, en `backend/Infrastructure/Configuration/MongoConnectionVerifier.cs` (research D1; FR-005/006/007; depende de T013)
- [x] T016 [US3] Registrar `MongoHealthCheck` (`AddCheck<MongoHealthCheck>("mongodb")`) y el `MongoConnectionVerifier` (`AddHostedService`) en `backend/Infrastructure/Configuration/DependencyInjection.cs` (depende de T014, T015)
- [x] T017 [US3] Ajustar `backend/Api/Program.cs` para que `AddHealthChecks()` incluya el check `mongodb` y `/health` devuelva 200/503 según conectividad (contrato: [contracts/health-endpoint.md](contracts/health-endpoint.md); depende de T016)
- [x] T018 [US3] Unificar `worker/Program.cs` para resolver la cadena desde `MONGODB_URI` (misma fuente que la API) en lugar de `MongoDB:ConnectionString`, eliminando el fallo silencioso del Worker (FR-004; contrato: [contracts/connection-config.md](contracts/connection-config.md))

**Checkpoint**: Backend verifica y expone la conexión; los tests de US3 pasan en verde.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Propósito**: Verificación end-to-end, calidad y documentación.

- [x] T019 Ejecutar quickstart escenarios 3 y 4 (conexión verificada, `/health` 200/503, fallo diferenciado) y escenario 5 (persistencia tras reinicio) confirmando SC-003/004/005 (referencia: [quickstart.md](quickstart.md))
- [x] T020 [P] Ejecutar `dotnet format` en `backend/` y confirmar 0 warnings de estilo (constitución, Calidad)
- [x] T021 [P] Documentar la verificación de conexión y el health check de Mongo en `README.md` (sección de infraestructura/observabilidad) y registrar ADR si la política fail-soft amerita decision record (constitución, Principio VI)
- [x] T022 Ejecutar la suite completa (`dotnet test`) y confirmar todas las suites en verde con cobertura ≥85% sin drops (constitución, CI Gate)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias — puede comenzar de inmediato.
- **Foundational (Phase 2)**: depende de Setup; BLOQUEA a US3.
- **US1 (Phase 3)** y **US2 (Phase 4)**: verificación de infraestructura ya existente; dependen solo de Setup (no de Foundational). Pueden ejecutarse en paralelo entre sí.
- **US3 (Phase 5)**: depende de Foundational (Phase 2). Su test de integración real se apoya en US1/US2 (servicio + base disponibles), pero el código es independiente.
- **Polish (Phase 6)**: depende de que US1, US2 y US3 estén completas.

### User Story Dependencies

- **US1 (P1)**: independiente — verificación del servicio Docker.
- **US2 (P1)**: independiente — verificación de `init-mongo.js`.
- **US3 (P1)**: requiere Foundational (config tipada/DI). Verificable de forma aislada con los tests; el escenario end-to-end usa US1+US2 activas.

### Within Each User Story

- Los tests (US3) se escriben y FALLAN antes de implementar.
- `MongoErrorClassifier` (T013) antes del verifier (T015).
- Health check + verifier (T014, T015) antes de registrarlos en DI (T016) y exponerlos en `Program.cs` (T017).

### Parallel Opportunities

- T002 en paralelo con T001.
- T003 en paralelo (archivo nuevo) dentro de Foundational.
- US1 (T005–T006) y US2 (T007–T008) en paralelo entre sí.
- Tests de US3 (T009, T010, T011, T012) en paralelo.
- Implementaciones de archivos distintos (T013, T014) en paralelo; T015–T017 son secuenciales por dependencia.
- Polish T020 y T021 en paralelo.

---

## Parallel Example: User Story 3 (Tests)

```bash
# Lanzar los tests de US3 juntos (deben fallar antes de implementar):
Task: "MongoDbOptionsTests en backend/Tests/Infrastructure/MongoDbOptionsTests.cs"
Task: "MongoErrorClassificationTests en backend/Tests/Infrastructure/MongoErrorClassificationTests.cs"
Task: "MongoConnectionTests en backend/Tests/Infrastructure/MongoConnectionTests.cs"
Task: "MongoHealthCheckTests en backend/Tests/Infrastructure/MongoHealthCheckTests.cs"
```

---

## Implementation Strategy

### MVP First (US1 + US2)

1. Phase 1: Setup.
2. US1 (servicio en 27017) + US2 (`monolegal_dev`): verificación de infraestructura ya presente → MVP demostrable con `docker-compose up`.
3. **PARAR y VALIDAR**: quickstart escenarios 1 y 2.

### Incremental Delivery

1. Setup → Foundational (config tipada).
2. US1 + US2 → verificación infra → demo (MVP).
3. US3 → verificación de conexión + health check + unificación Worker → demo completa.
4. Polish → quickstart end-to-end, formato, docs, cobertura.

---

## Notes

- [P] = archivos distintos, sin dependencias.
- La etiqueta [Story] mapea cada tarea a su historia para trazabilidad.
- Verificar que los tests fallan antes de implementar (Red-Green-Refactor).
- Sin skips de tests permitidos (constitución).
- Commit tras cada tarea o grupo lógico, referenciando `spec-0.4`.
- US1 y US2 son mayormente verificación: el scaffolding de Fase 0.1 ya dejó el servicio y el seed; el grueso del trabajo nuevo está en US3.
