# Implementation Plan: Vista de Envíos — Estado de notificaciones por factura y acciones manuales

**Branch**: `019-vista-envios` | **Date**: 2026-06-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/019-vista-envios/spec.md`

## Summary

Añadir la vista `/envios` al panel de administración: una tabla por factura con su estado de envío de notificación (pendiente, enviado, fallido, reintentando transitorio, omitido), columnas (ID, Cliente, Email, Estado, Último intento, Reintentos), filtro por estado y búsqueda por cliente/correo, y acciones manuales por factura (reenviar, cancelar = marcar omitido) más una acción global "Reintentar fallidos" que reutiliza la herramienta masiva existente.

Enfoque técnico: extender el dominio `Invoice` con un **contador de reintentos persistido** (`NotificationRetryCount`) que se reinicia al entrar en un nuevo estado notificable; añadir un endpoint de **listado de envíos** (`GET /api/invoices/shipments`) con filtro/búsqueda y resolución de correo, un endpoint de **reenvío por factura** (`POST /api/invoices/{id}/resend`) y uno de **cancelación** (`POST /api/invoices/{id}/cancel-notification`), todos reutilizando `IInvoiceTransitionNotifier` y los patrones de las specs 009/014/017. En el frontend, una feature `shipments` con TanStack Query (listado + mutaciones), insignias por estado, skeletons, empty states y toasts accesibles, siguiendo el estándar de la Fase 4.

## Technical Context

**Language/Version**: Backend C# / .NET 10 (ASP.NET Core 10, Minimal APIs); Frontend TypeScript strict, React 19 + Vite

**Primary Dependencies**: Backend — MongoDB.Driver, FluentValidation, Serilog. Frontend — TanStack Query, shadcn/ui, Motion, react-router-dom v7

**Storage**: MongoDB (estado de notificación embebido en el documento `Invoice`; sin colección nueva)

**Testing**: Backend — xUnit + Shouldly (Domain/Application/Infrastructure). Frontend — Vitest + Testing Library; MSW para mocks de API

**Target Platform**: Panel web admin (Linux server backend + worker en Docker; SPA servida estática)

**Project Type**: Web application (backend + frontend + worker), arquitectura limpia por capas

**Performance Goals**: API ≤200ms bajo carga normal con paginación forzada; Frontend TTI < 2s, listado sin parpadeo (keepPreviousData)

**Constraints**: Sin secretos en BD/UI; paginación obligatoria (sin queries sin límite); dark mode built-in; WCAG A; React Doctor 100/100 honesto

**Scale/Scope**: Una vista nueva (`/envios`), 1 feature frontend, 1 endpoint de listado + 2 endpoints de acción nuevos, 1 campo de dominio nuevo, reutilización de 1 endpoint masivo existente

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principio | Cumplimiento en este plan |
|-----------|---------------------------|
| **I. Arquitectura Limpia** | El contador de reintentos vive en `Domain.Invoice`; la orquestación de reenvío/cancelación en `Application`; los endpoints en `Api`; el proveedor de correo permanece confinado en `Infrastructure` detrás de `IEmailService`/`IInvoiceTransitionNotifier`. La vista organiza una feature `shipments` con límites claros (api/hooks/components). ✅ |
| **II. SOLID** | Reuso de `IInvoiceTransitionNotifier` (DIP, OCP: se añade reenvío sin tocar el envío); nuevos contratos pequeños (`IInvoiceShipmentService`) por segregación de interfaz; una responsabilidad por handler/endpoint. ✅ |
| **III. SDD** | Plan derivado de spec con 6 clarificaciones resueltas; sin ambigüedades pendientes; documentación en español. ✅ |
| **IV. Test-First** | Tests primero por capa: dominio (reset/incremento del contador), aplicación (reenvío/cancelación/listado), endpoints (contratos), frontend (tabla/filtros/mutaciones/estados). Cobertura ≥85%. ✅ |
| **V. Frontend Producción** | TS strict sin `any`, Biome, React Doctor 100/100, dark mode, a11y por teclado, toasts accesibles, "reducir movimiento". ✅ |
| **VI. Observable/Mantenible** | Serilog estructurado en los nuevos handlers (invoiceId, outcome, retryCount, duración); error boundaries y degradación elegante en frontend; DI por constructor. ✅ |

**Resultado del gate (pre-research)**: PASA. Sin violaciones; no se requiere Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/019-vista-envios/
├── plan.md              # Este archivo
├── research.md          # Fase 0
├── data-model.md        # Fase 1
├── quickstart.md        # Fase 1
├── contracts/           # Fase 1
│   ├── get-shipments.md
│   ├── resend-invoice.md
│   └── cancel-notification.md
└── tasks.md             # Fase 2 (/speckit-tasks — no lo crea /speckit-plan)
```

### Source Code (repository root)

```text
backend/
├── Domain/
│   └── Entities/Invoice.cs                 # + NotificationRetryCount, reset en UpdateStatus, incremento
├── Domain/Repositories/
│   └── IInvoiceRepository.cs               # + GetShipmentsPagedAsync(...)
├── Application/
│   ├── Abstractions/IInvoiceShipmentService.cs   # NUEVO: reenvío + cancelación por factura
│   └── Services/InvoiceShipmentService.cs        # NUEVO: orquesta notifier + contador + persistencia
├── Infrastructure/
│   └── Repositories/MongoInvoiceRepository.cs     # implementación de GetShipmentsPagedAsync + índices
├── Api/Endpoints/Invoices/
│   ├── ListShipments.cs                    # NUEVO: GET /api/invoices/shipments
│   ├── ResendInvoice.cs                    # NUEVO: POST /api/invoices/{id}/resend
│   ├── CancelInvoiceNotification.cs        # NUEVO: POST /api/invoices/{id}/cancel-notification
│   └── InvoiceDtos.cs                      # + ShipmentListItemDto, SendStatus mapping
└── Tests/                                  # tests por capa (xUnit)

frontend/src/
├── features/shipments/                     # NUEVA feature
│   ├── api/{getShipments.ts,useShipments.ts,shipmentMutations.ts,useShipmentMutations.ts}
│   ├── components/{ShipmentsPage.tsx,ShipmentsTable.tsx,ShipmentsTableSkeleton.tsx,
│   │              ShipmentStatusBadge.tsx,ShipmentsEmptyState.tsx,ShipmentsFilters.tsx,
│   │              CancelNotificationDialog.tsx}
│   ├── hooks/useShipmentsViewState.ts
│   └── types.ts
├── components/layout/navigation.ts         # + entrada /envios
└── App.tsx                                 # + ruta /envios (lazy)
```

**Structure Decision**: Web application (Opción 2). Se reutiliza la arquitectura limpia por capas del backend y la organización por feature del frontend (espejo de `features/invoices`). No se introduce ningún proyecto nuevo ni colección de BD nueva; el estado de envío sigue embebido en `Invoice`.

## Complexity Tracking

> No aplica — la Constitution Check pasa sin violaciones que justificar.
