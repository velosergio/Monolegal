# Implementation Plan: Repositorio MongoDB de Facturas

**Branch**: `007-invoice-mongo-repository` | **Date**: 2026-06-24 | **Spec**: [spec.md](./spec.md)

**Input**: Especificación de funcionalidad desde `specs/007-invoice-mongo-repository/spec.md`

## Summary

Proveer el acceso a datos de la entidad `Invoice` desde la capa de infraestructura, implementando el contrato `IInvoiceRepository` sobre MongoDB. Las capacidades requeridas son: consultar por estado (`GetByStatusAsync`), consultar por cliente (`GetByClientIdAsync`), cambiar estado de forma atómica (`UpdateStatusAsync`), crear facturas (`AddAsync`, semántica de `InsertAsync`) e índices sobre `Status` y `ClientId`.

**Estado real del código**: gran parte de esta funcionalidad ya existe en el repositorio (`MongoInvoiceRepository`, `MongoIndexBuilder`, registro DI). Este plan formaliza los requisitos GIVEN/WHEN/THEN, valida el cumplimiento del código actual contra la spec y la constitución, e identifica la única brecha real: **tests de integración del repositorio contra un MongoDB real** (actualmente solo existe un test de contrato contra un fake en memoria), exigidos por el Principio IV (Integration Tests para contratos de repositorio).

## Technical Context

**Language/Version**: C# / .NET 10 (net10.0)

**Primary Dependencies**: MongoDB.Driver (sin EF), Microsoft.Extensions.DependencyInjection, Microsoft.Extensions.Logging (Serilog backend)

**Storage**: MongoDB 8 — colección `Invoices` en base `monolegal_dev` (dev)

**Testing**: xUnit + Shouldly. Tests de integración contra MongoDB real vía variable de entorno `MONGODB_URI` (docker-compose), marcados con `[Trait("Category", "Integration")]` (patrón ya establecido en `MongoConnectionTests`)

**Target Platform**: Servicio backend en Linux (contenedor Docker), VPS de producción

**Project Type**: Web service (backend ASP.NET Core con capas Domain/Application/Infrastructure/Api) + frontend separado

**Performance Goals**: Consultas por `Status` y `ClientId` ≤200 ms bajo carga normal, apoyadas en índices simples sobre esos campos

**Constraints**: Sin fugas de detalles de persistencia fuera de Infrastructure (Principio I); operaciones de cambio de estado deben actualizar solo los campos de estado/auditoría; creación de índices idempotente en el arranque

**Scale/Scope**: Volumen de facturas moderado por cliente; colección única `Invoices`; 5 historias de usuario (4×P1, 1×P2), 11 requisitos funcionales

## Constitution Check

*GATE: Debe pasar antes de Phase 0. Re-evaluado tras Phase 1.*

| Principio | Evaluación | Estado |
|-----------|-----------|--------|
| **I. Arquitectura Limpia** | El acceso a datos queda encapsulado en `Infrastructure/Repositories`; el dominio solo expone la abstracción `IInvoiceRepository`. Un cambio de motor (Mongo → otro) no se propaga. | ✅ PASS |
| **II. SOLID** | `IInvoiceRepository` es un contrato cohesivo; `MongoInvoiceRepository` tiene única responsabilidad (persistencia de facturas); inyección por constructor de `IMongoDatabase`. | ✅ PASS |
| **III. SDD (docs en español)** | Spec y todos los artefactos de este plan en español, formato GIVEN/WHEN/THEN. | ✅ PASS |
| **IV. Test-First (Integration para repos)** | **Brecha**: solo existe test de contrato contra fake en memoria. Falta integration test del repositorio Mongo real. Se añade como trabajo de este plan. | ⚠️ A RESOLVER |
| **V. Frontend producción** | No aplica (feature de backend puro). | ➖ N/A |
| **VI. Observabilidad** | El builder de índices ya loguea con Serilog. Las operaciones del repositorio son finas; logging significativo ocurre en capas superiores (worker/endpoints). | ✅ PASS |
| **Performance (índices)** | Índices sobre `Status`, `ClientId` (y `LastStatusTransitionAt`) ya creados por `MongoIndexBuilder`. | ✅ PASS |

**Resultado del gate**: PASS con una brecha de tests de integración a cerrar en la implementación. No hay violaciones que justificar en Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/007-invoice-mongo-repository/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Phase 0 (/speckit-plan)
├── data-model.md        # Phase 1 (/speckit-plan)
├── quickstart.md        # Phase 1 (/speckit-plan)
├── contracts/           # Phase 1 (/speckit-plan)
│   └── IInvoiceRepository.md
├── checklists/
│   └── requirements.md  # Creado por /speckit-specify
└── tasks.md             # Phase 2 (/speckit-tasks — NO creado por /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── Domain/
│   ├── Entities/Invoice.cs               # Entidad (spec 005) — ya existe
│   ├── Enums/InvoiceStatus.cs            # Enum de estados — ya existe
│   └── Repositories/IInvoiceRepository.cs# Contrato del repositorio — ya existe
├── Infrastructure/
│   ├── Repositories/
│   │   └── MongoInvoiceRepository.cs     # Implementación Mongo — ya existe
│   ├── Persistence/
│   │   └── MongoIndexBuilder.cs          # Índices Status/ClientId/... — ya existe
│   └── Configuration/
│       └── DependencyInjection.cs        # Registro DI del repositorio — ya existe
└── Tests/
    └── Infrastructure/
        ├── InvoiceRepositoryContractTests.cs   # Contra fake en memoria — ya existe
        └── MongoInvoiceRepositoryIntegrationTests.cs  # NUEVO — contra Mongo real
```

**Structure Decision**: Estructura de servicio web con capas. La feature toca exclusivamente las capas `Domain` (contrato, ya existente) e `Infrastructure` (implementación, índices, DI, ya existentes) más el proyecto `Tests`. El único artefacto nuevo es la suite de integración `MongoInvoiceRepositoryIntegrationTests.cs` que valida la implementación real de Mongo contra el contrato.

## Complexity Tracking

> No aplica — la Constitution Check pasa sin violaciones que justificar. El patrón Repository es exigido por la constitución (Principio I), no una complejidad opcional.
