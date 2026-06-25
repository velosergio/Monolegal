# Implementation Plan: Seed Data - 3 Clientes Mínimo

**Branch**: `008-seed-data-clientes` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/008-seed-data-clientes/spec.md`

## Summary

Implementar un proceso de siembra (seeder) de datos de desarrollo que, partiendo de una base de datos MongoDB vacía, cree un conjunto mínimo, predecible y representativo: **3 clientes** (A, B, C) con una distribución fija de **8 facturas** (3 / 2 / 3) cubriendo estados variados y garantizando al menos una factura en `primerrecordatorio` y otra en `segundorecordatorio`.

Enfoque técnico: orquestación de siembra en la capa **Application** (`DevDataSeeder`), reutilizando la entidad de dominio `Invoice` y sus métodos (`UpdateStatus`, `RecordReminderSent`) y persistiendo vía `IInvoiceRepository`. Se añade una capacidad de conteo (`CountAsync`) al repositorio para soportar la verificación de "base vacía" e idempotencia. El disparo ocurre en el arranque de la **Api**, restringido a entorno de desarrollo, mediante un `IHostedService` que invoca al seeder y registra el resultado con Serilog (sembrado/omitido + conteos).

**Nota de dominio**: No existe una entidad ni colección `Cliente` independiente en el modelo actual; los clientes se representan por valores distintos de `ClientId` en las facturas. Los "3 clientes" son tres identificadores estables y distintos. Ver `research.md` (D1).

## Technical Context

**Language/Version**: C# 13 / .NET 10 (ASP.NET Core 10, Minimal APIs)

**Primary Dependencies**: MongoDB.Driver, Serilog (structured logging), Microsoft.Extensions.Hosting (IHostedService). Reutiliza `Invoice`, `InvoiceStatus`, `IInvoiceRepository`, `InvoiceTransitionService` existentes.

**Storage**: MongoDB — colección `Invoices` (sin colección `Clients`; clientes representados por `ClientId`).

**Testing**: xUnit + Shouldly (unit, capa Application/Domain); tests de integración con `MongoIntegrationFixture` (base efímera por GUID) sobre MongoDB real, patrón `[Trait("Category", "Integration")]`.

**Target Platform**: Contenedor Linux / VPS producción (el seeder NO debe ejecutarse en producción; gate por entorno).

**Project Type**: Web service backend multicapa (Domain / Application / Infrastructure / Api) + worker; esta feature es exclusivamente backend.

**Performance Goals**: Operación de arranque de una sola vez; inserción de 8 documentos. Sin impacto en SLA de API (<200ms por operación de dominio). El conteo de verificación usa el índice existente.

**Constraints**: Idempotencia obligatoria (no duplicar en reejecución); siembra sólo si la colección está vacía; ejecución restringida a entorno Development; sin secrets embebidos.

**Scale/Scope**: Conjunto fijo y mínimo: 3 clientes, 8 facturas. No es un generador de volumen.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación | Cumple |
|-----------|------------|--------|
| **I. Arquitectura Limpia** | Orquestación de siembra en Application (`DevDataSeeder`); persistencia en Infrastructure (`MongoInvoiceRepository.CountAsync`); disparo en Api (hosted service dev-only). Dominio inalterado salvo reutilización. Cambio de almacenamiento no se propaga fuera de Infrastructure. | ✅ |
| **II. SOLID** | `IDevDataSeeder` (abstracción) inyectada por constructor; SRP: el seeder sólo define/siembra el dataset; el hosted service sólo orquesta el disparo. Repositorio extendido vía nuevo método cohesivo. | ✅ |
| **III. SDD (specs en español)** | Spec GIVEN/WHEN/THEN existe (`008`); plan, research, data-model y quickstart en español. | ✅ |
| **IV. Test-First** | Tests unitarios (distribución, idempotencia con repo fake) + integración (siembra real, doble ejecución sin duplicar). Red-Green-Refactor; cobertura ≥85%. | ✅ |
| **V. Frontend Producción** | N/A — feature exclusivamente backend (sin componentes UI). | ✅ (N/A) |
| **VI. Observable y Mantenible** | Serilog structured logging del resultado (sembrado/omitido, conteos por cliente/estado); DI por constructor centralizada en `DependencyInjection`. | ✅ |
| **Seguridad** | Seeder dev-only mediante gate `IHostInfrastructure`/`IHostEnvironment.IsDevelopment()`; nunca corre en producción; sin credenciales hardcodeadas. | ✅ |
| **Performance** | Una inserción de 8 documentos al arranque; conteo apoyado por índices existentes; sin queries sin límite. | ✅ |

**Resultado del gate**: PASS. Sin violaciones; sección Complexity Tracking vacía.

## Project Structure

### Documentation (this feature)

```text
specs/008-seed-data-clientes/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Fase 0 (/speckit-plan)
├── data-model.md        # Fase 1 (/speckit-plan)
├── quickstart.md        # Fase 1 (/speckit-plan)
├── contracts/           # Fase 1 (/speckit-plan)
│   └── dev-data-seeder.md
└── tasks.md             # Fase 2 (/speckit-tasks - NO creado aquí)
```

### Source Code (repository root)

```text
backend/
├── Domain/
│   ├── Entities/Invoice.cs              # Reutilizado (sin cambios)
│   ├── Enums/InvoiceStatus.cs           # Reutilizado (sin cambios)
│   └── Repositories/IInvoiceRepository.cs   # + CountAsync(...)
├── Application/
│   ├── Abstractions/IDevDataSeeder.cs   # NUEVO — contrato del seeder
│   └── Seeding/
│       ├── DevDataSeeder.cs             # NUEVO — orquestación de siembra
│       └── SeedDataDefinition.cs        # NUEVO — definición fija del dataset (3/2/3)
├── Infrastructure/
│   ├── Repositories/MongoInvoiceRepository.cs   # + CountAsync(...)
│   └── Hosting/DevDataSeederHostedService.cs    # NUEVO — disparo dev-only al arranque
├── Api/
│   └── Program.cs                       # Registro condicional (Development)
└── Tests/
    ├── Monolegal.Application.Tests/Seeding/
    │   ├── DevDataSeederDistributionTests.cs    # NUEVO — unit
    │   └── DevDataSeederIdempotencyTests.cs     # NUEVO — unit
    └── Infrastructure/
        └── DevDataSeederIntegrationTests.cs     # NUEVO — integración (Mongo real)
```

**Structure Decision**: Se mantiene la estructura multicapa existente del backend (`backend/Domain`, `backend/Application`, `backend/Infrastructure`, `backend/Api`, `backend/Tests`). La lógica de definición/siembra es orquestación de aplicación (Application); la verificación de vacuidad y persistencia son detalles de Infrastructure; el disparo condicional por entorno vive en Api/Infrastructure como `IHostedService`. No se introducen nuevos proyectos.

## Complexity Tracking

> Sin violaciones de la Constitución que justificar. Sección no aplicable.
