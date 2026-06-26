# Implementation Plan: CRUD de Facturas y Clientes

**Branch**: `018-crud-facturas-clientes` | **Date**: 2026-06-26 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/018-crud-facturas-clientes/spec.md`

## Summary

Esta funcionalidad habilita la gestión completa (alta, edición, baja) de **facturas** y la creación desde cero de un módulo CRUD de **clientes**. Dos cambios estructurales la definen:

1. **Ampliación del modelo `Invoice`**: se añaden líneas de detalle (items con descripción, cantidad y precio unitario) y una fecha de vencimiento. El `Amount` deja de ser un valor capturado y pasa a derivarse (suma de subtotales de los items). Se incorporan endpoints `POST/PUT/DELETE /api/invoices` con bloqueo de edición en estados terminales (`pagado`/`desactivado`) y borrado permanente permitido en cualquier estado.
2. **Nueva entidad `Client`**: hoy el cliente es solo un `ClientId` string. Se construye la entidad de dominio, su repositorio Mongo (con índice único por email), endpoints `GET/POST/PUT/DELETE /api/clients` con listado paginado + búsqueda, y la pantalla `/clientes` en el frontend.

El enfoque respeta la Arquitectura Limpia existente (Domain → Application → Infrastructure → Api) y los patrones ya establecidos: endpoints minimal API como clases estáticas con métodos de extensión `Map*`, validadores FluentValidation en `Application/Validation`, repositorios singleton inyectados por interfaz, índices Mongo en `MongoIndexBuilder`, y en el frontend hooks de TanStack Query (`useQuery`/`useMutation`) con invalidación dirigida de claves y toasts vía `ToastProvider`.

## Technical Context

**Language/Version**: C# / .NET 10 (backend); TypeScript strict + React 19 (frontend)

**Primary Dependencies**: ASP.NET Core 10 Minimal APIs, MongoDB.Driver, FluentValidation, Serilog (backend); Vite, TanStack Query, shadcn/ui, react-router-dom v7, Motion (frontend)

**Storage**: MongoDB — colección existente `Invoices` (ampliada) y nueva colección `Clients`

**Testing**: xUnit + Shouldly (backend: dominio, contrato de repositorio, integración de endpoints con `WebApplicationFactory<Program>`); Vitest + Testing Library (frontend); Playwright (E2E jornadas críticas)

**Target Platform**: API contenedorizada en VPS (Docker Compose); SPA servida estáticamente

**Project Type**: Aplicación web (backend + frontend separados)

**Performance Goals**: Consultas API ≤200ms bajo carga normal (constitución); refresco de UI tras operación exitosa percibido como inmediato (CE-001 <5s, CE-005 búsqueda <1s)

**Constraints**: Paginación forzada (sin queries sin límite); índices en campos consultados (`Email`, `ClientId`); validación frontend espejo de la del backend; dark mode built-in; TS strict sin `any`; Biome + React Doctor 100% compliant; cobertura ≥85%

**Scale/Scope**: Panel administrativo de un solo rol (Admin). Volumen moderado (cientos–miles de facturas/clientes). 2 entidades, ~9 endpoints nuevos, 2 pantallas frontend (facturas ampliada + clientes nueva)

## Constitution Check

*GATE: Debe pasar antes de Phase 0. Re-evaluado tras Phase 1.*

| Principio | Gate | Evaluación inicial |
|-----------|------|--------------------|
| I. Arquitectura Limpia | Entidades y contratos en Domain; lógica en Application; Mongo en Infrastructure; endpoints en Api. Cambios de persistencia no se filtran a capas superiores. | ✅ PASS — `Client` y `IClientRepository` en Domain; `MongoClientRepository` en Infrastructure; validadores en Application; endpoints en Api. |
| II. SOLID | Una responsabilidad por validador/endpoint/repositorio; dependencias por interfaz inyectadas en constructor. | ✅ PASS — sin service locators; repos por interfaz; validadores segregados por operación. |
| III. SDD (specs en español) | Spec GIVEN/WHEN/THEN previa; todos los artefactos en español. | ✅ PASS — spec con clarificaciones cerradas; artefactos de plan en español. |
| IV. Test-First (≥85%) | Tests primero: dominio (cálculo de monto, bloqueo terminal), contrato de repositorio, integración de endpoints, frontend, E2E. | ✅ PASS (compromiso) — descrito en quickstart.md y tasks futuras. |
| V. Frontend Producción | TS strict, Biome, React Doctor 0 warnings, dark mode, a11y WCAG A, TTI<2s, code splitting por ruta (lazy). | ✅ PASS — ruta `/clientes` lazy; formularios accesibles; reutiliza primitives shadcn/ui. |
| VI. Observable/Mantenible | Serilog structured logging en cada operación (clientId/invoiceId/resultado); DI centralizada. | ✅ PASS — logging por operación en endpoints; registro en `DependencyInjection`. |
| Seguridad | Admin-only; FluentValidation en todos los inputs; unicidad de email; sin secretos. | ✅ PASS. |
| Performance | Índice único `Email`; índice `ClientId` ya existe; paginación forzada en `/api/clients`. | ✅ PASS. |

**Resultado**: Sin violaciones. La sección *Complexity Tracking* queda vacía.

## Project Structure

### Documentation (this feature)

```text
specs/018-crud-facturas-clientes/
├── plan.md              # Este archivo (/speckit-plan)
├── research.md          # Phase 0 — decisiones técnicas
├── data-model.md        # Phase 1 — entidades, campos, reglas, migración
├── quickstart.md        # Phase 1 — guía de validación end-to-end
├── contracts/           # Phase 1 — contratos de API
│   ├── invoices-crud.md
│   └── clients-crud.md
├── checklists/
│   └── requirements.md  # Checklist de calidad de la spec (ya existente)
└── tasks.md             # Phase 2 (/speckit-tasks — NO lo crea este comando)
```

### Source Code (repository root)

```text
backend/
├── Domain/
│   ├── Entities/
│   │   ├── Invoice.cs            # AMPLIAR: items + dueDate + monto derivado
│   │   ├── InvoiceItem.cs        # NUEVO: value object (descripción, cantidad, precioUnitario)
│   │   └── Client.cs             # NUEVO: entidad Cliente
│   └── Repositories/
│       ├── IInvoiceRepository.cs # AMPLIAR: DeleteAsync(id)
│       └── IClientRepository.cs  # NUEVO
├── Application/
│   └── Validation/
│       ├── CreateInvoiceValidator.cs   # NUEVO
│       ├── UpdateInvoiceValidator.cs   # NUEVO
│       ├── CreateClientValidator.cs    # NUEVO
│       └── UpdateClientValidator.cs    # NUEVO
├── Infrastructure/
│   ├── Repositories/
│   │   ├── MongoInvoiceRepository.cs   # AMPLIAR: DeleteAsync
│   │   └── MongoClientRepository.cs    # NUEVO
│   ├── Persistence/
│   │   └── MongoIndexBuilder.cs        # AMPLIAR / NUEVO builder para índice único Email
│   ├── Hosting/
│   │   └── InvoiceItemsBackfillMigration.cs  # NUEVO: backfill legacy (item sintético + cliente)
│   └── Configuration/
│       └── DependencyInjection.cs      # AMPLIAR: registrar IClientRepository, resolver, migración
└── Api/
    └── Endpoints/
        ├── Invoices/
        │   ├── CreateInvoice.cs        # NUEVO
        │   ├── UpdateInvoice.cs        # NUEVO
        │   ├── DeleteInvoice.cs        # NUEVO
        │   └── InvoiceDtos.cs          # AMPLIAR: items, dueDate, request DTOs
        └── Clients/                    # NUEVO directorio
            ├── ListClients.cs
            ├── GetClientById.cs
            ├── CreateClient.cs
            ├── UpdateClient.cs
            ├── DeleteClient.cs
            └── ClientDtos.cs

frontend/
└── src/
    ├── App.tsx                         # AMPLIAR: ruta /clientes (lazy)
    └── features/
        ├── invoices/
        │   ├── api/                    # NUEVO: useCreateInvoice, useUpdateInvoice, useDeleteInvoice
        │   ├── components/             # NUEVO: InvoiceFormModal, InvoiceItemsEditor, DeleteInvoiceDialog
        │   └── types.ts               # AMPLIAR: items, dueDate
        └── clients/                    # NUEVO feature
            ├── api/                    # getClients, useClients, useCreate/Update/DeleteClient
            ├── components/             # ClientsPage, ClientsTable, ClientFormModal, DeleteClientDialog, ClientsSearch, ClientsPagination
            └── types.ts
```

**Structure Decision**: Aplicación web con backend y frontend separados (Opción 2). Se reutilizan los directorios reales ya existentes (`backend/{Domain,Application,Infrastructure,Api}`, `frontend/src/features/*`). El módulo de clientes sigue el patrón por feature del frontend, espejando la estructura de `features/invoices`.

## Complexity Tracking

> Sin violaciones a la constitución. No aplica.
