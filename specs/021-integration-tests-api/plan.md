# Implementation Plan: Tests de Integración de la API

**Branch**: `021-integration-tests-api` | **Date**: 2026-06-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/021-integration-tests-api/spec.md`

## Summary

Consolidar y completar la cobertura de **tests de integración HTTP** de los endpoints principales de facturas (spec 009) ejercitándolos de extremo a extremo a través del pipeline real de ASP.NET Core con `WebApplicationFactory<Program>`. Hoy existen tests que **replican** la orquestación de los handlers (p. ej. `TransitionInvoiceTests` reimplementa el flujo en un método privado) o prueban componentes aislados (validador + repositorio en memoria); estos validan lógica pero **no** atraviesan el enrutamiento, el binding, la negociación de contenido ni la traducción de resultados a códigos HTTP. Esta feature añade pruebas que arrancan la app en memoria, sustituyen las dependencias de infraestructura (Mongo, hosted services, notificador, resolución de email) por dobles en memoria ya existentes, y verifican el **contrato HTTP observable**: listado (`200` + filtro + paginación inválida `400`), detalle (`200`/`404`), transición (`200` permitida / `400` prohibida / `404` inexistente / `400` cuerpo inválido) y estadísticas (incl. base vacía).

El enfoque reutiliza el patrón ya probado en `InvoiceCrudEndpointsTests` (spec 018): una `WebApplicationFactory<Program>` que reemplaza repositorios y servicios por implementaciones en memoria del directorio `Support/`. No se modifica el comportamiento del API; sólo se añaden pruebas.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (`net10.0`)

**Primary Dependencies**: xUnit 2.9, Shouldly 4.3, `Microsoft.AspNetCore.Mvc.Testing` 10.0 (`WebApplicationFactory<Program>`), `System.Net.Http.Json`, `System.Text.Json` (aserciones sobre el JSON de respuesta)

**Storage**: Ninguno en estas pruebas. Se sustituye `IInvoiceRepository`/`IClientRepository`/`ISystemSettingsRepository` por dobles en memoria (`Support/InMemory*`); **no** se requiere MongoDB para esta suite (a diferencia de los tests de `MongoIntegrationFixture`).

**Testing**: xUnit + Shouldly; ejecución vía `dotnet test backend/Tests/Tests.csproj`. Categorización con `[Trait("Category", "Application")]` (sin dependencia externa) coherente con la suite existente.

**Target Platform**: Backend ASP.NET Core 10 (Linux/Windows). Host de pruebas en memoria (`TestServer`), sin puerto de red real.

**Project Type**: Web service (backend con capas Domain/Application/Infrastructure/Api). Las pruebas viven en el proyecto `backend/Tests/Tests.csproj`.

**Performance Goals**: La suite de integración del API se ejecuta en segundos (sin E/S de red ni base de datos); cada caso arranca un host ligero en memoria. Objetivo: corrida completa de esta clase < 15 s en máquina de desarrollo estándar.

**Constraints**: Determinismo y aislamiento por test (estado de datos limpio); sin pruebas omitidas (`[Fact(Skip=...)]`/`[Ignore]`), conforme al Principio IV; los endpoints protegidos deben poder ejercitarse sin credenciales de producción (entorno `Development` + dobles).

**Scale/Scope**: 4 endpoints bajo prueba (`GET /api/invoices`, `GET /api/invoices/{id}`, `POST /api/invoices/transition/{id}`, `GET /api/invoices/stats`); ~14–18 casos de prueba nuevos agrupados por endpoint.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Evaluación |
|-----------|------------|
| **I. Arquitectura Limpia** | ✅ Las pruebas viven en la capa de Tests y consumen el API real sin acoplarse a internals; sustituyen Infrastructure por dobles vía DI (inversión de dependencias respetada). No se introduce lógica de negocio en los tests. |
| **II. SOLID** | ✅ Se reutilizan abstracciones (`IInvoiceRepository`, etc.) y dobles existentes; los tests dependen de interfaces, no de concreciones de Mongo. |
| **III. SDD** | ✅ Feature nace de spec GIVEN/WHEN/THEN (spec.md 021); criterios de aceptación trazables a casos de prueba (ver contracts/). Documentación en español. |
| **IV. Test-First** | ✅ La feature **es** cobertura de pruebas. Integration tests de endpoints API exigidos explícitamente por el Principio IV. Sin skips; resultado consumible por CI. |
| **V. Frontend Calidad** | ➖ No aplica (feature exclusivamente backend). |
| **VI. Observable/Mantenible** | ✅ Se reutiliza el patrón de fábrica existente; los tests documentan el contrato HTTP y sirven de regresión. No introducen acoplamiento oculto. |

**Resultado**: PASS. Sin violaciones que requieran justificación en Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/021-integration-tests-api/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Fase 0: decisiones técnicas
├── data-model.md        # Fase 1: entidades de datos de prueba y formas de respuesta
├── quickstart.md        # Fase 1: cómo ejecutar y validar la suite
├── contracts/
│   └── api-test-matrix.md  # Fase 1: matriz endpoint → casos → estado esperado
├── checklists/
│   └── requirements.md  # Checklist de calidad de la spec (de /speckit-specify)
└── tasks.md             # Fase 2 (/speckit-tasks — NO creado aquí)
```

### Source Code (repository root)

```text
backend/
├── Api/
│   ├── Program.cs                         # Expone `public partial class Program {}` (host de pruebas)
│   └── Endpoints/Invoices/                # Endpoints bajo prueba (sin cambios)
│       ├── ListInvoices.cs                # GET /api/invoices
│       ├── GetInvoiceById.cs              # GET /api/invoices/{id}
│       ├── TransitionInvoice.cs           # POST /api/invoices/transition/{id}
│       └── GetInvoiceStats.cs             # GET /api/invoices/stats
└── Tests/
    └── Infrastructure/
        ├── Support/                       # Dobles reutilizados (sin cambios salvo helpers menores)
        │   ├── InMemoryInvoiceRepository.cs
        │   ├── InMemoryClientRepository.cs
        │   ├── InMemorySystemSettingsRepository.cs
        │   ├── FakeClientEmailResolver.cs
        │   ├── FakeTransitionNotifier.cs
        │   └── InvoiceTestFactory.cs
        └── InvoiceApiEndpointsTests.cs    # NUEVO: suite de integración HTTP (US1–US4)
```

**Structure Decision**: Web service con backend en capas. Las nuevas pruebas se añaden a `backend/Tests/Tests.csproj` (proyecto que ya referencia `Api.csproj` y `Microsoft.AspNetCore.Mvc.Testing`). Se introduce una clase de pruebas nueva, `InvoiceApiEndpointsTests`, que define su propia `WebApplicationFactory<Program>` interna (patrón idéntico a `InvoiceCrudEndpointsTests`) y agrupa los casos por endpoint/historia. No se crean nuevos proyectos ni se modifican los endpoints de producción.

## Complexity Tracking

> No aplica. La Constitution Check pasa sin violaciones; no se introducen patrones adicionales ni proyectos nuevos.
