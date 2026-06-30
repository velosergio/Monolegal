# Inyección de Dependencias — Monolegal

La configuración de DI está **centralizada** (Principio VI de la [Constitución](../.specify/memory/constitution.md)):

- **Backend/Worker compartido**: `backend/Infrastructure/Configuration/DependencyInjection.cs`
  (método de extensión `AddInfrastructure`) registra repositorios, servicios de email, resolvers,
  notificador, seeder, mantenimiento, cliente/base de MongoDB y los *hosted services*.
- **API**: `backend/Api/Program.cs` invoca `AddInfrastructure` y añade los servicios propios del host
  (p. ej. el sembrado automático en Development).
- **Worker**: `worker/Program.cs` registra su cliente/base de MongoDB y el `BackgroundWorker`.

La inyección es por **constructor** (sin *service locators*). Este documento refleja el registro real;
es la referencia rápida de "qué abstracción resuelve a qué implementación y con qué ciclo de vida".

> **Mantenimiento (documento vivo)**: actualizar esta tabla en el **mismo PR** que modifique
> `DependencyInjection.cs`, `backend/Api/Program.cs` o `worker/Program.cs`.

## Backend — `AddInfrastructure` (`DependencyInjection.cs`)

| Abstracción | Implementación | Ciclo de vida | Registrado en |
|-------------|----------------|---------------|---------------|
| `IOptions<MongoDbOptions>` | `MongoDbOptions` (desde configuración) | Singleton | `DependencyInjection.cs` |
| `InvoiceTransitionService` | (servicio de dominio, tipo concreto) | Singleton | `DependencyInjection.cs` |
| `ISystemSettingsRepository` | `MongoSystemSettingsRepository` | Singleton | `DependencyInjection.cs` |
| `IInvoiceRepository` | `MongoInvoiceRepository` | Singleton | `DependencyInjection.cs` |
| `IClientRepository` | `MongoClientRepository` | Singleton | `DependencyInjection.cs` |
| `IMongoClient` | `MongoClient` (pooling + timeout explícitos) | Singleton | `DependencyInjection.cs` |
| `IMongoDatabase` | `client.GetDatabase(...)` (factory) | Singleton | `DependencyInjection.cs` |
| `MongoIndexBuilder` | (tipo concreto) | Singleton | `DependencyInjection.cs` |
| `MongoConnectionVerifier` | `IHostedService` (verificación al arranque) | Hosted | `DependencyInjection.cs` |
| `StatusHistoryBackfillMigration` | `IHostedService` (migración idempotente) | Hosted | `DependencyInjection.cs` |
| `InvoiceItemsBackfillMigration` | `IHostedService` (migración idempotente) | Hosted | `DependencyInjection.cs` |
| `IOptions<InvoiceTransitionsWorkerOptions>` | `InvoiceTransitionsWorkerOptions` (configuración) | Singleton | `DependencyInjection.cs` |
| `IOptions<EmailOptions>` | `EmailOptions` (secretos/defaults de entorno) | Singleton | `DependencyInjection.cs` |
| `EmailTemplateProvider` | (tipo concreto) | Singleton | `DependencyInjection.cs` |
| `HttpClient` (cliente con nombre `"resend"`) | `AddHttpClient("resend")` | Transient (fábrica) | `DependencyInjection.cs` |
| `IEmailProvider` | `SmtpEmailProvider` **y** `ResendEmailProvider` (múltiple) | Singleton | `DependencyInjection.cs` |
| `IEmailProviderFactory` | `EmailProviderFactory` | Singleton | `DependencyInjection.cs` |
| `IEmailCredentialStatus` | `EmailCredentialStatusService` | Singleton | `DependencyInjection.cs` |
| `IEmailService` | `SettingsBackedEmailService` *(si hay proveedor configurado)* / `NoOpEmailService` *(Dev/CI sin proveedor)* | Singleton | `DependencyInjection.cs` |
| `ConfiguredClientEmailResolver` | (tipo concreto, usado como fallback) | Singleton | `DependencyInjection.cs` |
| `IClientEmailResolver` | `ClientRepositoryEmailResolver` (con fallback al anterior) | Singleton | `DependencyInjection.cs` |
| `IInvoiceTransitionNotifier` | `InvoiceTransitionNotifier` | Singleton | `DependencyInjection.cs` |
| `IEmailAdminService` | `EmailAdminService` | Singleton | `DependencyInjection.cs` |
| `IInvoiceShipmentService` | `InvoiceShipmentService` | Singleton | `DependencyInjection.cs` |
| `IDevDataSeeder` | `DevDataSeeder` | Singleton | `DependencyInjection.cs` |
| `IMaintenanceService` | `MaintenanceService` | Singleton | `DependencyInjection.cs` |
| `InvoiceTransitionsWorker` | `IHostedService` (ciclo periódico de transiciones) | Hosted | `DependencyInjection.cs` |
| Health check `"mongodb"` | `MongoHealthCheck` | (health checks) | `DependencyInjection.cs` |

> Nota sobre los repositorios `Singleton`: el driver de MongoDB es *thread-safe* y el `IMongoClient`
> gestiona su propio *connection pool*, por lo que repositorios y cliente se registran como Singleton
> (ver [ADR 0003](./adr/0003-repositorios-singleton-mongodb.md)).

## API — host (`backend/Api/Program.cs`)

| Abstracción | Implementación | Ciclo de vida | Registrado en |
|-------------|----------------|---------------|---------------|
| (todo lo anterior) | `AddInfrastructure(builder.Configuration)` | — | `Program.cs` |
| `DevDataSeederHostedService` | `IHostedService` (sembrado automático, **solo Development**) | Hosted | `Program.cs` |

## Worker (`worker/Program.cs`)

| Abstracción | Implementación | Ciclo de vida | Registrado en |
|-------------|----------------|---------------|---------------|
| `IMongoClient` | `MongoClient` (desde `MONGODB_URI`) | Singleton | `worker/Program.cs` |
| `IMongoDatabase` | `client.GetDatabase(...)` (factory) | Singleton | `worker/Program.cs` |
| `BackgroundWorker` | `IHostedService` (trabajos de fondo) | Hosted | `worker/Program.cs` |

## Cómo sustituir una implementación

1. Localizar el registro en `DependencyInjection.cs` (o `Program.cs` del host correspondiente).
2. Cambiar la implementación concreta manteniendo la **abstracción** (interfaz) — los consumidores no se
   modifican (Principio de Inversión de Dependencias).
3. Actualizar la fila correspondiente de este documento en el mismo PR.

## Documentación relacionada

- [Arquitectura](./architecture.md) — capas y dirección de dependencias.
- [Registro de decisiones (ADR)](./adr/README.md).
