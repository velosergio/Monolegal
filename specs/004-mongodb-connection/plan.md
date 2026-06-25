# Plan de Implementación: Conexión MongoDB

**Rama**: `004-mongodb-connection` | **Fecha**: 2026-06-24 | **Spec**: [spec.md](spec.md)

**Entrada**: Especificación de feature de `/specs/004-mongodb-connection/spec.md`

## Resumen

Garantizar que, al levantar el entorno con `docker-compose up`, el servicio MongoDB 8 esté corriendo en el puerto 27017, la base `monolegal_dev` exista, y el **backend verifique la conexión al arranque de forma observable** (log estructurado) y la exponga mediante un **health check de conectividad real**. El scaffolding de la Fase 0.1 ya dejó el servicio `mongo` en `docker-compose.yml` (con volúmenes, healthcheck e `init-mongo.js`) y la Fase 0.2 referenció `MongoDB.Driver`. Esta fase **cierra las brechas de verificación y observabilidad** y formaliza la configuración tipada de conexión como contrato verificable.

**Brechas detectadas en el estado actual** (auditoría de `docker-compose.yml`, `backend/Api/Program.cs`, `backend/Infrastructure/Configuration/DependencyInjection.cs`, `worker/Program.cs`):

1. **Sin verificación de conexión al arranque (FR-005, FR-006, HU3)**: el `MongoClient` se crea de forma perezosa en `AddInfrastructure`; no se ejecuta ningún `ping` ni se registra log de éxito/fallo al iniciar. Un fallo de conectividad pasaría desapercibido hasta la primera query real.
2. **Health check de Mongo ausente (FR-008, HU3)**: `Program.cs` registra `AddHealthChecks()` y expone `/health`, pero **no hay ningún check específico de MongoDB**; el endpoint responde "Healthy" aunque la base de datos esté caída.
3. **Sin reporte diferenciado de fallo (FR-007)**: no existe manejo que distinga "servicio no disponible" de "credenciales incorrectas" con un mensaje claro.
4. **Configuración por strings mágicos e inconsistente (FR-004, FR-010)**: la API lee `ConnectionStrings:MongoDB` → `MONGODB_URI` → fallback `localhost`, mientras que el Worker lee `MongoDB:ConnectionString`. `docker-compose` inyecta `MONGODB_URI`, que **el Worker no resuelve** bajo la clave que espera. No hay configuración tipada (`MongoDbOptions`) ni pooling/timeout explícito.
5. **Pooling y cierre limpio no explícitos (FR-010)**: el driver aplica pooling por defecto y `Log.CloseAndFlush()` ya existe, pero no se configuran `MongoClientSettings` (tamaño de pool, timeout de selección de servidor) ni un apagado coordinado de la conexión.

**Ya presente y solo a verificar**: servicio `mongo:8` en puerto 27017 (FR-001, FR-002, FR-011), `init-mongo.js` que crea `monolegal_dev` con colecciones/índices/seed (FR-003), volúmenes `mongo_data`/`mongo_config` para persistencia (FR-009), healthcheck de contenedor (`mongosh ping`), `MongoDB.Driver 3.4.0` referenciado, registro de `IMongoClient`/`IMongoDatabase` en DI.

## Contexto Técnico

**Lenguaje/Versión**: C# / .NET 10 (`net10.0`), ASP.NET Core 10 Minimal APIs (backend); MongoDB 8 (motor).

**Dependencias Primarias** (objetivo de esta fase, por propósito):

- **Driver de base de datos**: `MongoDB.Driver` (3.4.0) — presente, verificar.
- **Verificación de salud**: health check de conectividad sobre Mongo. Opción A: paquete `AspNetCore.HealthChecks.MongoDb`; Opción B: `IHealthCheck` custom que ejecuta `ping`. **Decisión en research.md** (se favorece check custom para minimizar dependencias y controlar el comando `ping`).
- **Logging estructurado**: `Serilog` (4.3.0) + `Serilog.AspNetCore` (9.0.0) — presente; se usa para el log observable de verificación de conexión (FR-006).
- **Configuración tipada**: `Microsoft.Extensions.Options` (incluido en el framework) para `MongoDbOptions` (FR-004).
- **Verificación al arranque**: `IHostedService`/`BackgroundService` ligero (o bloque previo a `app.Run()`) que ejecuta `ping` y registra resultado (FR-005).

**Almacenamiento**: MongoDB 8 (documental), base `monolegal_dev`, accesible en `mongo:27017` dentro de la red `monolegal-network` y expuesto en `localhost:27017` al host. Persistencia vía volúmenes Docker `mongo_data`/`mongo_config`.

**Testing**: xUnit + Shouldly (`backend/Tests/Tests.csproj`). Tests de integración verifican `ping`/conexión y el health check; tests unitarios verifican binding de `MongoDbOptions` y el reporte de error diferenciado. Conforme a Test-First (constitución, Principio IV).

**Plataforma Objetivo**: Contenedores Docker sobre Linux (desarrollo local y VPS producción). Servicios separados frontend/backend/worker/mongo orquestados por Docker Compose.

**Tipo de Proyecto**: Servicio web backend (.NET, carpeta `backend/`) + worker (`worker/`), dentro de un monorepo con frontend separado. Arquitectura Limpia por capas (Domain/Application/Infrastructure/Api).

**Objetivos de Performance**: La verificación de conexión al arranque debe completarse en < 10s en condiciones normales (SC-003). El `ping` de health check debe responder dentro del timeout configurado del healthcheck del contenedor (5s). Pooling de conexiones conforme a la constitución (Persistencia de Datos).

**Restricciones**:

- Sin credenciales hardcodeadas en código; configuración vía variables de entorno (`MONGODB_URI`) / `MongoDbOptions` (constitución, Seguridad).
- El cambio tecnológico de persistencia no debe propagarse más allá de la capa Infrastructure (constitución, Principio I); la verificación y el health check viven en Infrastructure/Api, no en Domain/Application.
- Inyección por constructor explícita, sin service locators; configuración DI centralizada en `AddInfrastructure` (constitución, Principio VI).
- Logging estructurado JSON con contexto del resultado de la verificación (constitución, Principio VI).
- MongoDB 8; sin EF (solo driver nativo).

**Escala/Alcance**: 1 servicio de base de datos; verificación de conexión + health check + configuración tipada en el backend; unificación de la clave de configuración entre API y Worker. Sin modelado de entidades de negocio (corresponde a Fase 1).

## Revisión de Constitución

*PUERTA: Debe pasar antes de investigación de Fase 0. Re-chequear después de diseño de Fase 1.*

### Alineación con Principios

✅ **I. Arquitectura Limpia (NO NEGOCIABLE)**: Toda la lógica de conexión, verificación y health check reside en la capa **Infrastructure** (registro DI, `MongoDbOptions`, health check) y en **Api** (mapeo del endpoint `/health`). Domain y Application no conocen MongoDB. Un cambio de motor (ej. PostgreSQL) quedaría contenido en Infrastructure. **CUMPLE**.

✅ **II. Principios SOLID**: `MongoDbOptions` (config) separada del cliente; el health check implementa la abstracción `IHealthCheck` (ISP/DIP); la verificación de arranque es un `IHostedService` con única responsabilidad. Inversión de dependencias vía interfaces `IMongoClient`/`IMongoDatabase`. **CUMPLE**.

✅ **III. Desarrollo Dirigido por Especificaciones**: Esta fase deriva de la spec 0.4 en formato GIVEN/WHEN/THEN; toda la documentación en español. **CUMPLE**.

✅ **IV. Desarrollo Test-First (NO NEGOCIABLE)**: Tests de integración (ping/conexión, health check) y unitarios (binding de opciones, reporte de error) se escriben antes de la implementación (Red-Green-Refactor) en `backend/Tests`. **CUMPLE**.

➖ **V. Frontend de Calidad Producción**: No aplica (fase de infraestructura backend; sin cambios de UI).

✅ **VI. Código Observable y Mantenible**: El resultado de la verificación de conexión se registra con Serilog structured logging (contexto: host, base, resultado, duración) conforme a FR-006; configuración DI centralizada y documentada; inyección por constructor. **CUMPLE** (refuerza el principio).

✅ **Stack Tecnológico**: MongoDB 8 + `MongoDB.Driver` (sin EF), Serilog structured logs, ASP.NET Core 10, Docker Compose con servicios separados y volúmenes de persistencia, sin secrets embebidos — todo contemplado. **CUMPLE**.

✅ **Seguridad & Persistencia**: Connection pooling configurado vía `MongoClientSettings`; shutdown limpio; credenciales solo por variables de entorno. **CUMPLE**.

### Resultado de la Puerta

**✅ APROBADO** — Sin violaciones. La sección de Seguimiento de Complejidad no requiere justificaciones.

## Estructura del Proyecto

### Documentación (esta feature)

```text
specs/004-mongodb-connection/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Salida Fase 0 — decisiones de verificación/health check (/speckit-plan)
├── data-model.md        # Salida Fase 1 — configuración de conexión y estados (/speckit-plan)
├── quickstart.md        # Salida Fase 1 — guía de verificación end-to-end (/speckit-plan)
├── contracts/           # Salida Fase 1 (/speckit-plan)
│   ├── health-endpoint.md          # Contrato del endpoint /health
│   └── connection-config.md        # Contrato de configuración de conexión
├── checklists/
│   └── requirements.md  # Checklist de calidad de la spec
└── tasks.md             # Salida Fase 2 (/speckit-tasks — NO creado por /speckit-plan)
```

### Código Fuente (raíz del repositorio)

```text
docker-compose.yml                  # presente — verificar servicio mongo:8, puerto, volúmenes, healthcheck
.env.example                        # presente — verificar MONGODB_URI documentado
scripts/
└── init-mongo.js                   # presente — crea monolegal_dev, colecciones, seed

backend/
├── Api/
│   └── Program.cs                  # + registrar health check de Mongo en /health; arranque verifica conexión
├── Infrastructure/
│   ├── Infrastructure.csproj       # presente — MongoDB.Driver 3.4.0 (verificar)
│   └── Configuration/
│       ├── DependencyInjection.cs  # + MongoDbOptions tipado, MongoClientSettings (pooling/timeout), registro health check + hosted service
│       ├── MongoDbOptions.cs       # NUEVO — opciones tipadas (ConnectionString, DatabaseName, pool/timeout)
│       ├── MongoConnectionVerifier.cs   # NUEVO — IHostedService: ping al arranque + log estructurado (FR-005/006/007)
│       └── MongoHealthCheck.cs     # NUEVO — IHealthCheck: ping de conectividad (FR-008)
├── Tests/
│   ├── Tests.csproj                # presente — xUnit + Shouldly
│   └── Infrastructure/
│       ├── MongoConnectionTests.cs      # NUEVO — integración: conexión/ping a monolegal_dev
│       ├── MongoHealthCheckTests.cs     # NUEVO — health check Healthy/Unhealthy
│       └── MongoDbOptionsTests.cs       # NUEVO — binding de configuración
└── ...

worker/
└── Program.cs                      # ajustar clave de configuración para unificar con MONGODB_URI (FR-004)
```

**Decisión de Estructura**: Se mantiene la estructura de Arquitectura Limpia por capas de la Fase 0.1/0.2. Esta fase **no crea proyectos nuevos**; añade en `backend/Infrastructure/Configuration/` la configuración tipada (`MongoDbOptions`), el verificador de arranque (`MongoConnectionVerifier`) y el health check (`MongoHealthCheck`); modifica `Program.cs` (API) para registrar el health check en `/health`; unifica la clave de configuración en `worker/Program.cs`; y añade los tests de integración/unitarios en `backend/Tests/Infrastructure/`. El servicio `mongo` y `init-mongo.js` ya existen y solo se verifican.

## Seguimiento de Complejidad

> Sin violaciones de la Revisión de Constitución. No se requieren justificaciones de complejidad.
