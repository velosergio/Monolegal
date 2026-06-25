# Plan de Implementación: Endpoints API de Facturas

**Branch**: `009-invoice-api-endpoints` | **Date**: 2026-06-25 | **Spec**: [spec.md](./spec.md)

**Input**: Especificación de funcionalidad desde `specs/009-invoice-api-endpoints/spec.md`

## Summary

Exponer cuatro endpoints HTTP (Minimal APIs) sobre la entidad `Invoice` ya existente: listado paginado con filtro por estado (`GET /api/invoices`), detalle (`GET /api/invoices/{id}`), transición manual de estado (`POST /api/invoices/transition/{id}`) y estadísticas agregadas de dashboard (`GET /api/invoices/stats`).

Enfoque técnico: reutilizar `Invoice`, `InvoiceStatus`, `IInvoiceRepository`, `MongoInvoiceRepository` e `InvoiceTransitionService` (specs 005/006/007). Se añaden: (1) métodos de acceso a datos para paginación con conteo total y agregaciones por estado/cliente en la capa Infrastructure; (2) una operación de dominio de transición manual con validación de transiciones permitidas; (3) una representación de `InvoiceStatus` como cadena en minúscula en el contrato HTTP (`pending`, `primerrecordatorio`, `segundorecordatorio`, `desactivado`, `pagado`); (4) validación de entradas con FluentValidation; (5) los cuatro endpoints siguiendo el patrón `static class + MapXxx` ya usado por `PayInvoice`.

## Technical Context

**Language/Version**: C# / .NET 10 (`net10.0`, SDK 10.0.301)

**Primary Dependencies**: ASP.NET Core 10 Minimal APIs, MongoDB.Driver, FluentValidation (ya referenciada en `Application.csproj`), Serilog.AspNetCore

**Storage**: MongoDB (colección `Invoices`); índices sobre `Status`, `ClientId`, `CreatedAt` (spec 007)

**Testing**: xUnit + Shouldly (tests de aplicación con repositorio en memoria, patrón de `PayInvoiceTests`); fixture de integración MongoDB opcional (`MongoIntegrationFixture`)

**Target Platform**: Servicio web Linux (contenedor Docker), backend administrativo

**Project Type**: Web service (backend ASP.NET Core con capas Domain/Application/Infrastructure/Api)

**Performance Goals**: Endpoints stateless; respuesta ≤200 ms bajo carga normal; paginación forzada (`pageSize` máximo 50)

**Constraints**: Sin queries sin límite; serialización de estado en minúscula en el contrato; acceso a datos encapsulado en Infrastructure; logging estructurado por acción

**Scale/Scope**: 4 endpoints, ~1 servicio de dominio extendido, ~4 métodos de repositorio nuevos, validadores FluentValidation y suite de tests asociada

## Constitution Check

*GATE: Debe pasar antes de Phase 0. Re-evaluado tras Phase 1.*

| Principio | Evaluación | Estado |
|-----------|------------|--------|
| I. Arquitectura Limpia | Acceso a datos (paginación, agregaciones) añadido a `IInvoiceRepository` (Domain) + `MongoInvoiceRepository` (Infrastructure); reglas de transición en `InvoiceTransitionService` (Domain); endpoints en Api. Sin fuga de MongoDB a capas superiores. | ✅ PASS |
| II. SOLID | Métodos de repositorio cohesivos; transición manual añadida por extensión al servicio existente (OCP); endpoints dependen de abstracciones inyectadas por constructor/parámetro. | ✅ PASS |
| III. SDD (specs en español) | Spec 009 clarificada; todos los artefactos de este plan en español. | ✅ PASS |
| IV. Test-First (≥85%) | Tests de contrato/aplicación se escriben antes de la implementación (Red-Green-Refactor), siguiendo el patrón de `PayInvoiceTests`. | ✅ PASS (compromiso) |
| V. Frontend Producción | No aplica: esta feature es solo backend (el consumo frontend se aborda aparte). | ➖ N/A |
| VI. Observable y Mantenible | Cada endpoint loguea con Serilog (acción, filtros, resultado, id). DI por constructor/parámetro. | ✅ PASS |
| Stack tecnológico | ASP.NET Core 10 Minimal APIs, MongoDB.Driver, FluentValidation, Serilog — todos ya presentes. | ✅ PASS |
| Seguridad (JWT Admin-only) | **Diferido**: autenticación es transversal y se aborda en una spec de seguridad independiente (documentado en Assumptions de la spec). Los endpoints se registrarán tras el pipeline de auth cuando exista. No es violación del alcance de esta feature. | ⚠️ DEFERRED |
| Performance | Paginación forzada, `pageSize` ≤ 50, índices existentes en `Status`/`ClientId`/`CreatedAt`. | ✅ PASS |

**Resultado del gate**: PASS. Único elemento diferido (autenticación) está justificado y fuera del alcance de esta spec; ningún principio NO NEGOCIABLE se incumple.

## Project Structure

### Documentation (this feature)

```text
specs/009-invoice-api-endpoints/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Phase 0 — decisiones técnicas
├── data-model.md        # Phase 1 — entidades, DTOs, contratos de repositorio
├── quickstart.md        # Phase 1 — guía de validación end-to-end
├── contracts/           # Phase 1 — contratos HTTP de los 4 endpoints
│   ├── list-invoices.md
│   ├── get-invoice-by-id.md
│   ├── transition-invoice.md
│   └── get-invoice-stats.md
├── checklists/
│   └── requirements.md  # Checklist de calidad (ya existente)
└── tasks.md             # Phase 2 (/speckit-tasks — NO creado por /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── Domain/
│   ├── Repositories/
│   │   └── IInvoiceRepository.cs          # + GetPagedAsync, CountAsync(status?),
│   │                                       #   CountByStatusAsync, CountByClientAsync
│   └── Services/
│       └── InvoiceTransitionService.cs    # + ApplyManualTransition(invoice, newStatus)
├── Application/
│   └── Validation/                        # (nuevo) validadores FluentValidation
│       ├── ListInvoicesQueryValidator.cs
│       └── TransitionInvoiceRequestValidator.cs
├── Infrastructure/
│   └── Repositories/
│       └── MongoInvoiceRepository.cs      # implementación de los nuevos métodos
└── Api/
    ├── Endpoints/Invoices/
    │   ├── ListInvoices.cs                # (nuevo) GET /api/invoices
    │   ├── GetInvoiceById.cs              # (nuevo) GET /api/invoices/{id}
    │   ├── TransitionInvoice.cs           # (nuevo) POST /api/invoices/transition/{id}
    │   ├── GetInvoiceStats.cs             # (nuevo) GET /api/invoices/stats
    │   └── InvoiceStatusApi.cs            # (nuevo) mapeo enum ↔ cadena de API + DTOs
    └── Program.cs                          # registro de los 4 endpoints + JSON enum config

backend/Tests/Monolegal.Application.Tests/Endpoints/
├── ListInvoicesTests.cs                   # (nuevo)
├── GetInvoiceByIdTests.cs                 # (nuevo)
├── TransitionInvoiceTests.cs              # (nuevo)
└── GetInvoiceStatsTests.cs               # (nuevo)
```

**Structure Decision**: Web service por capas ya establecido en `backend/`. Esta feature añade exclusivamente código en `Domain` (contrato e regla de transición), `Application` (validadores), `Infrastructure` (implementación de acceso a datos) y `Api` (endpoints + mapeo de estado), respetando la dirección de dependencias de la Arquitectura Limpia.

## Complexity Tracking

> Sin violaciones de la Constitución que requieran justificación. El único punto diferido (autenticación JWT) corresponde a una spec de seguridad independiente y no introduce complejidad adicional en esta feature.
