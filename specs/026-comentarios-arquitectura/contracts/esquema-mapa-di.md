# Contrato — Esquema del mapa de Inyección de Dependencias

Define la estructura de `docs/dependency-injection.md`. Satisface FR-006 y FR-007.

## Estructura del documento

1. **Introducción**: la configuración DI está centralizada en `backend/Infrastructure/Configuration/DependencyInjection.cs` (`AddInfrastructure`) y complementada en `backend/Api/Program.cs`; el worker registra sus servicios análogamente.
2. **Tabla por área/capa** con las columnas:

   | Abstracción | Implementación | Ciclo de vida | Registrado en |
   |-------------|----------------|---------------|---------------|

3. **Nota de mantenimiento** (documento vivo, FR-012): "Actualizar esta tabla en el mismo PR que modifique `DependencyInjection.cs` o `Program.cs`."

## Reglas

1. Cada **abstracción registrada** en el contenedor DEBE tener una fila. Las implementaciones registradas sin interfaz (tipos concretos) se listan con la abstracción en blanco o el propio tipo.
2. El **ciclo de vida** DEBE reflejar el método de registro real: `AddSingleton` → Singleton, `AddScoped` → Scoped, `AddTransient` → Transient, `AddHostedService` → Hosted (Singleton).
3. La columna **Registrado en** DEBE apuntar al archivo real de registro.
4. Sin entradas faltantes ni obsoletas respecto al registro real (verificado en code review, SC-003).

## Filas de referencia (extraídas del registro actual)

| Abstracción | Implementación | Ciclo de vida | Registrado en |
|-------------|----------------|---------------|---------------|
| `IInvoiceRepository` | `MongoInvoiceRepository` | Singleton | `DependencyInjection.cs` |
| `IClientRepository` | `MongoClientRepository` | Singleton | `DependencyInjection.cs` |
| `ISystemSettingsRepository` | `MongoSystemSettingsRepository` | Singleton | `DependencyInjection.cs` |
| `IEmailProvider` | `SmtpEmailProvider`, `ResendEmailProvider` | Singleton (múltiple) | `DependencyInjection.cs` |
| `IEmailProviderFactory` | `EmailProviderFactory` | Singleton | `DependencyInjection.cs` |
| `IEmailService` | `SettingsBackedEmailService` / `NoOpEmailService` (según config) | Singleton | `DependencyInjection.cs` |
| `IClientEmailResolver` | `ClientRepositoryEmailResolver` | Singleton | `DependencyInjection.cs` |
| `IInvoiceTransitionNotifier` | `InvoiceTransitionNotifier` | Singleton | `DependencyInjection.cs` |
| `IEmailAdminService` | `EmailAdminService` | Singleton | `DependencyInjection.cs` |
| `IInvoiceShipmentService` | `InvoiceShipmentService` | Singleton | `DependencyInjection.cs` |
| `IDevDataSeeder` | `DevDataSeeder` | Singleton | `DependencyInjection.cs` |
| `IMaintenanceService` | `MaintenanceService` | Singleton | `DependencyInjection.cs` |
| `InvoiceTransitionService` | (tipo concreto, servicio de dominio) | Singleton | `DependencyInjection.cs` |
| `IMongoClient` / `IMongoDatabase` | `MongoClient` / `GetDatabase(...)` | Singleton | `DependencyInjection.cs` |
| `MongoConnectionVerifier` | (HostedService) | Hosted | `DependencyInjection.cs` |
| `InvoiceTransitionsWorker` | (HostedService) | Hosted | `DependencyInjection.cs` |
| Migraciones (`StatusHistoryBackfillMigration`, `InvoiceItemsBackfillMigration`) | (HostedService) | Hosted | `DependencyInjection.cs` |

> La tabla final del documento incluirá también los registros de `Program.cs` y del proyecto `worker/`, completados durante la implementación.
