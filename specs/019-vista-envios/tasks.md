---
description: "Task list — Vista de Envíos (spec 019)"
---

# Tasks: Vista de Envíos — Estado de notificaciones por factura y acciones manuales

**Input**: Design documents from `specs/019-vista-envios/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUIDOS — la Constitución (Principio IV, NO NEGOCIABLE) exige desarrollo Test-First (Red-Green-Refactor, cobertura ≥85%).

**Organization**: Tareas agrupadas por historia de usuario para implementación y prueba independientes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Puede ejecutarse en paralelo (archivos distintos, sin dependencias pendientes)
- **[Story]**: Historia de usuario asociada (US1–US4)

## Path Conventions (web app — ver plan.md)

- Backend: `backend/{Domain,Application,Infrastructure,Api,Tests}/...`
- Frontend: `frontend/src/...`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Estructura de la feature frontend y tipos compartidos.

- [x] T001 Crear estructura de la feature `frontend/src/features/shipments/{api,components,hooks}` y tipos compartidos en `frontend/src/features/shipments/types.ts` (`Shipment`, `SendStatus = 'pending'|'sent'|'failed'|'skipped'|'retrying'`, `GetShipmentsParams`, `PagedShipments`)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Cambio de dominio (contador de reintentos) y DTO/serialización que TODAS las historias necesitan.

**⚠️ CRITICAL**: Ninguna historia de usuario puede comenzar hasta completar esta fase.

- [x] T002 [P] Test de dominio del contador en `backend/Tests/Monolegal.Domain.Tests/InvoiceNotificationRetryTests.cs`: `UpdateStatus` a estado notificable reinicia `NotificationRetryCount` a 0; `RecordNotificationRetry()` incrementa; `RecordNotificationResult(...)` NO toca el contador
- [x] T003 Añadir campo `NotificationRetryCount` (int, default 0), método `RecordNotificationRetry()` y reset condicional en `UpdateStatus` (cuando el destino es notificable) en `backend/Domain/Entities/Invoice.cs`
- [x] T004 Garantizar persistencia/serialización de `NotificationRetryCount` (default 0 para documentos existentes) en `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs` (depende de T003)
- [x] T005 [P] Añadir `ShipmentListItemDto` y el mapeo `LastNotificationOutcome → sendStatus` (None→pending, Sent→sent, Failed→failed, Skipped→skipped) en `backend/Api/Endpoints/Invoices/InvoiceDtos.cs`
- [x] T006 [P] Test del mapeo del DTO (outcome→sendStatus, `clientEmail` null, `lastError` solo en failed, `retryCount`) en `backend/Tests/Infrastructure/ShipmentDtoTests.cs`

**Checkpoint**: Dominio y DTO listos — las historias de usuario pueden comenzar.

---

## Phase 3: User Story 1 - Ver y entender el estado de envío de cada factura (Priority: P1) 🎯 MVP

**Goal**: Tabla `/envios` con ID, Cliente, Email, Estado de envío (insignia + etiqueta), Último intento y Reintentos; skeletons y empty state.

**Independent Test**: Abrir `/envios` con facturas notificables en distintos estados y verificar columnas, insignias con etiqueta textual, último intento y reintentos; skeleton durante la carga; empty state sin envíos.

### Tests for User Story 1 ⚠️ (escribir primero, deben fallar)

- [x] T007 [P] [US1] Contract test `GET /api/invoices/shipments` (sin params, `sendStatus`, `search`, 400 inválido, página vacía) en `backend/Tests/Infrastructure/ListShipmentsEndpointTests.cs`
- [x] T008 [P] [US1] Test de repositorio `GetShipmentsPagedAsync` (solo estados notificables, filtro `sendStatus`, filtro por `clientIds`, orden por `LastNotificationAt` desc, paginación y total) en `backend/Tests/Infrastructure/MongoInvoiceRepositoryShipmentsTests.cs`
- [x] T009 [P] [US1] Test de render `ShipmentsTable` (columnas y filas) en `frontend/src/features/shipments/components/ShipmentsTable.test.tsx`
- [x] T010 [P] [US1] Test `ShipmentStatusBadge` (color + etiqueta textual por estado) en `frontend/src/features/shipments/components/ShipmentStatusBadge.test.tsx`

### Implementation for User Story 1

- [x] T011 [US1] Añadir firma `GetShipmentsPagedAsync(NotificationOutcome? sendStatus, IReadOnlyCollection<string>? clientIds, int page, int pageSize, CancellationToken)` en `backend/Domain/Repositories/IInvoiceRepository.cs`
- [x] T012 [US1] Implementar `GetShipmentsPagedAsync` + índice compuesto `{Status, LastNotificationOutcome, LastNotificationAt}` en `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs` (depende de T011)
- [x] T013 [P] [US1] `ShipmentsQueryValidator` (sendStatus válido, page≥1, pageSize 1..50, search≤100) en `backend/Application/Validation/ShipmentsQueryValidator.cs`
- [x] T014 [US1] Endpoint `GET /api/invoices/shipments` (búsqueda en dos pasos vía `IClientRepository` por nombre/correo, resolución de email por clientId distinto anti N+1, validación, Serilog) en `backend/Api/Endpoints/Invoices/ListShipments.cs`
- [x] T015 [US1] Registrar `MapListShipments()` en `backend/Api/Program.cs`
- [x] T016 [P] [US1] Cliente API `getShipments` en `frontend/src/features/shipments/api/getShipments.ts`
- [x] T017 [US1] Hook `useShipments` (`useQuery` + `keepPreviousData`, queryKey `['shipments', params]`) en `frontend/src/features/shipments/api/useShipments.ts` (depende de T016)
- [x] T018 [P] [US1] Componente `ShipmentStatusBadge` (insignia de color + etiqueta) en `frontend/src/features/shipments/components/ShipmentStatusBadge.tsx`
- [x] T019 [P] [US1] Componente `ShipmentsTableSkeleton` en `frontend/src/features/shipments/components/ShipmentsTableSkeleton.tsx`
- [x] T020 [P] [US1] Componente `ShipmentsEmptyState` (variante "no hay envíos") en `frontend/src/features/shipments/components/ShipmentsEmptyState.tsx`
- [x] T021 [US1] Componente `ShipmentsTable` (columnas ID/Cliente/Email/Estado/Último intento/Reintentos, usa `ShipmentStatusBadge`) en `frontend/src/features/shipments/components/ShipmentsTable.tsx`
- [x] T022 [US1] Componente `ShipmentsPage` (compone `useShipments` + tabla + skeleton + empty) en `frontend/src/features/shipments/components/ShipmentsPage.tsx`
- [x] T023 [US1] Añadir ruta `/envios` (lazy, fallback `ShipmentsTableSkeleton`) en `frontend/src/App.tsx` y entrada de navegación en `frontend/src/components/layout/navigation.ts`

**Checkpoint**: `/envios` muestra el listado funcional con estados, skeletons y empty state (MVP).

---

## Phase 4: User Story 2 - Reenviar manualmente la notificación de una factura (Priority: P1)

**Goal**: Acción de fila "Reenviar" vía `POST /api/invoices/{id}/resend`, con badge transitorio "reintentando", toast de éxito/error y refresco automático.

**Independent Test**: Sobre una factura fallida, pulsar "Reenviar"; ver badge "reintentando" durante la mutación, toast del resultado y la fila refrescada con nuevo `sendStatus`, `lastAttemptAt` y `retryCount` +1.

### Tests for User Story 2 ⚠️

- [x] T024 [P] [US2] Test de aplicación `InvoiceShipmentService.ResendAsync` (incrementa contador, reusa notifier, outcome sent/failed/skipped) en `backend/Tests/Monolegal.Application.Tests/InvoiceShipmentServiceResendTests.cs`
- [x] T025 [P] [US2] Contract test `POST /api/invoices/{id}/resend` (200 sent, 200 failed fail-soft, skipped sin correo, 404) en `backend/Tests/Infrastructure/ResendInvoiceEndpointTests.cs`
- [x] T026 [P] [US2] Test de mutación de reenvío (badge "reintentando" transitorio + toast + invalidación, MSW) en `frontend/src/features/shipments/components/ShipmentsTable.resend.test.tsx`

### Implementation for User Story 2

- [x] T027 [P] [US2] Abstracción `IInvoiceShipmentService` (ResendAsync) en `backend/Application/Abstractions/IInvoiceShipmentService.cs`
- [x] T028 [US2] `InvoiceShipmentService.ResendAsync` (carga factura, `RecordNotificationRetry()`, `IInvoiceTransitionNotifier.NotifyTransitionAsync(invoice, invoice.Status)`, `UpdateAsync`, Serilog) en `backend/Application/Services/InvoiceShipmentService.cs` (depende de T027)
- [x] T029 [US2] Endpoint `POST /api/invoices/{id}/resend` (404 si no existe, devuelve `ShipmentListItemDto`) en `backend/Api/Endpoints/Invoices/ResendInvoice.cs`
- [x] T030 [US2] Registrar `MapResendInvoice()` y DI de `IInvoiceShipmentService → InvoiceShipmentService` en `backend/Api/Program.cs`
- [x] T031 [US2] Cliente `resendInvoice` en `frontend/src/features/shipments/api/shipmentMutations.ts`
- [x] T032 [US2] Hook `useResendInvoice` (mutación, invalida `['shipments']` / `['invoices']` / `['invoice-stats']`) en `frontend/src/features/shipments/api/useShipmentMutations.ts` (depende de T031)
- [x] T033 [US2] Acción de fila "Reenviar" + badge transitorio "reintentando" (mientras `isPending`) + toast + prevención de doble envío en `frontend/src/features/shipments/components/ShipmentsTable.tsx`

**Checkpoint**: Reenvío por factura funcional con feedback y refresco automático.

---

## Phase 5: User Story 3 - Filtrar por estado de envío y buscar por cliente/correo (Priority: P2)

**Goal**: Controles de filtro por estado y búsqueda por cliente/correo, combinables y limpiables; empty state diferenciado "sin coincidencias".

**Independent Test**: Filtrar "fallido" → solo fallidos; buscar parte de cliente/correo → reduce; combinar → AND; limpiar → listado completo; sin coincidencias → empty state específico.

### Tests for User Story 3 ⚠️

- [x] T034 [P] [US3] Test de filtros (estado, búsqueda, combinado, limpiar, empty "sin coincidencias" vs "no hay envíos") en `frontend/src/features/shipments/components/ShipmentsFilters.test.tsx`

### Implementation for User Story 3

> El backend ya soporta `sendStatus` y `search` (US1, T014). Esta fase es de UI.

- [x] T035 [US3] Hook `useShipmentsViewState` (estado de filtro/búsqueda/página con debounce; reset de página al cambiar filtro) en `frontend/src/features/shipments/hooks/useShipmentsViewState.ts`
- [x] T036 [US3] Componente `ShipmentsFilters` (selector de estado + input de búsqueda + botón limpiar, accesibles) en `frontend/src/features/shipments/components/ShipmentsFilters.tsx`
- [x] T037 [US3] Integrar filtros en `ShipmentsPage` y diferenciar el empty state "sin coincidencias" del de "no hay envíos" en `frontend/src/features/shipments/components/ShipmentsPage.tsx` (y `ShipmentsEmptyState.tsx`)

**Checkpoint**: Filtro y búsqueda funcionales y combinables sobre el listado.

---

## Phase 6: User Story 4 - Acciones por lote: reintentar fallidos y cancelar envío (Priority: P3)

**Goal**: Acción global "Reintentar fallidos" (reutiliza `resend-failed`) y acción de fila "Cancelar envío" (= marcar omitido) con confirmación; toasts y refresco.

**Independent Test**: "Reintentar fallidos" → toast con conteo afectado y refresco; "Cancelar envío" sobre pendiente → confirmación, pasa a "omitido"; sobre no pendiente → deshabilitado/409.

### Tests for User Story 4 ⚠️

- [x] T038 [P] [US4] Test de aplicación `InvoiceShipmentService.CancelAsync` (None→Skipped, conserva registro, rechaza si no pendiente o estado no notificable) en `backend/Tests/Monolegal.Application.Tests/InvoiceShipmentServiceCancelTests.cs`
- [x] T039 [P] [US4] Contract test `POST /api/invoices/{id}/cancel-notification` (200 skipped, 409 no pendiente, 409 estado no notificable, 404) en `backend/Tests/Infrastructure/CancelInvoiceNotificationEndpointTests.cs`
- [x] T040 [P] [US4] Test de que `EmailAdminService.ResendFailedAsync` incrementa `NotificationRetryCount` por factura reintentada en `backend/Tests/Infrastructure/EmailToolsRetryCountTests.cs`
- [x] T041 [P] [US4] Test del diálogo de cancelación + botón global "Reintentar fallidos" (confirmación + toasts, MSW) en `frontend/src/features/shipments/components/CancelNotificationDialog.test.tsx`

### Implementation for User Story 4

- [x] T042 [US4] `InvoiceShipmentService.CancelAsync` (valida pendiente+notificable, `RecordNotificationResult(type, Skipped, now, "cancelado por el administrador")`, `UpdateAsync`) en `backend/Application/Services/InvoiceShipmentService.cs` (y firma en `IInvoiceShipmentService.cs`)
- [x] T043 [US4] Endpoint `POST /api/invoices/{id}/cancel-notification` (200/409/404) en `backend/Api/Endpoints/Invoices/CancelInvoiceNotification.cs`
- [x] T044 [US4] Registrar `MapCancelInvoiceNotification()` en `backend/Api/Program.cs`
- [x] T045 [US4] Hacer que `ResendFailedAsync` llame a `RecordNotificationRetry()` por factura reintentada en `backend/Infrastructure/Email/EmailAdminService.cs`
- [x] T046 [US4] Cliente `cancelNotification` + hook `useCancelNotification` (invalida `['shipments']`) en `frontend/src/features/shipments/api/shipmentMutations.ts` y `frontend/src/features/shipments/api/useShipmentMutations.ts`
- [x] T047 [US4] Componente `CancelNotificationDialog` (confirmación explícita, accesible) en `frontend/src/features/shipments/components/CancelNotificationDialog.tsx`
- [x] T048 [US4] Acción de fila "Cancelar" (deshabilitada si no pendiente) + toast en `frontend/src/features/shipments/components/ShipmentsTable.tsx`
- [x] T049 [US4] Botón global "Reintentar fallidos" reutilizando `useResendFailed` de `frontend/src/features/settings/api/useEmailTools.ts`, con toast de conteo, en `frontend/src/features/shipments/components/ShipmentsPage.tsx`

**Checkpoint**: Acciones de lote (global) y cancelación por factura completas.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Calidad, rendimiento, accesibilidad y validación final.

- [x] T050 [P] Verificar con `explain` que el listado de envíos no produce COLLSCAN bajo carga normal; ajustar índice en `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs`
- [x] T051 [P] Añadir `WithName/WithTags/WithSummary/WithDescription` y `Produces(...)` a los 3 endpoints nuevos (`ListShipments.cs`, `ResendInvoice.cs`, `CancelInvoiceNotification.cs`) para Swagger/OpenAPI
- [x] T052 [P] Paso de accesibilidad (operación por teclado, foco visible, "reducir movimiento" en toasts/skeletons) en todos los componentes de `frontend/src/features/shipments/`
- [x] T053 [P] Ejecutar Biome y React Doctor (objetivo 100/100 honesto) sobre `frontend/src/features/shipments/` y corregir hallazgos
- [x] T054 Ejecutar la validación end-to-end de `specs/019-vista-envios/quickstart.md`
- [x] T055 [P] Actualizar README/diagramas si aplica para incluir la vista `/envios` y los nuevos endpoints

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias.
- **Foundational (Phase 2)**: depende de Setup; **bloquea** todas las historias.
- **US1 (Phase 3)**: depende de Foundational. Es el MVP.
- **US2 (Phase 4)**: depende de Foundational; consume el DTO/endpoint de US1 para refrescar, pero el reenvío es testeable independientemente.
- **US3 (Phase 5)**: depende de Foundational; usa el endpoint de US1 (params ya soportados). UI independiente.
- **US4 (Phase 6)**: depende de Foundational; reutiliza el servicio de US2 (`InvoiceShipmentService`) y `resend-failed` existente.
- **Polish (Phase 7)**: tras las historias deseadas.

### User Story Dependencies

- **US1 (P1)**: tras Foundational. Sin dependencias de otras historias.
- **US2 (P1)**: tras Foundational. Comparte `InvoiceShipmentService` (creado aquí); independientemente testeable.
- **US3 (P2)**: tras Foundational. Solo UI sobre el endpoint de US1.
- **US4 (P3)**: tras Foundational. Extiende `InvoiceShipmentService` (US2) y `EmailAdminService` (existente).

### Within Each User Story

- Tests primero (deben fallar) → dominio/DTO → repositorio → servicio → endpoint → frontend.

### Parallel Opportunities

- T002, T005/T006 (Foundational) en paralelo (archivos distintos); T003 antes de T004.
- US1: T007–T010 (tests) en paralelo; T016, T018, T019, T020 en paralelo; T013 en paralelo con backend.
- US2: T024–T026 en paralelo; T027 antes de T028.
- US4: T038–T041 en paralelo.
- Polish: T050–T053, T055 en paralelo.
- Con equipo: tras Foundational, US1/US2 (backend) y la UI base pueden avanzar en paralelo; US3 depende solo del endpoint de US1.

---

## Parallel Example: User Story 1

```bash
# Tests de US1 en paralelo (deben fallar primero):
Task: "Contract test GET /api/invoices/shipments en backend/Tests/Infrastructure/ListShipmentsEndpointTests.cs"
Task: "Test de repositorio GetShipmentsPagedAsync en backend/Tests/Infrastructure/MongoInvoiceRepositoryShipmentsTests.cs"
Task: "Test ShipmentsTable en frontend/src/features/shipments/components/ShipmentsTable.test.tsx"
Task: "Test ShipmentStatusBadge en frontend/src/features/shipments/components/ShipmentStatusBadge.test.tsx"

# Componentes de presentación de US1 en paralelo:
Task: "ShipmentStatusBadge.tsx"
Task: "ShipmentsTableSkeleton.tsx"
Task: "ShipmentsEmptyState.tsx"
Task: "getShipments.ts"
```

---

## Implementation Strategy

### MVP First (US1)

1. Phase 1 (Setup) → Phase 2 (Foundational) → Phase 3 (US1).
2. **DETENER y VALIDAR**: probar `/envios` de forma independiente (listado, estados, skeleton, empty).
3. Demo/deploy del MVP.

### Incremental Delivery

1. Setup + Foundational → base lista.
2. US1 → listado (MVP) → validar → demo.
3. US2 → reenvío por factura → validar → demo.
4. US3 → filtro/búsqueda → validar → demo.
5. US4 → acciones de lote + cancelar → validar → demo.

---

## Notes

- [P] = archivos distintos, sin dependencias pendientes.
- "reintentando" es UI transitoria (no hay dato de servidor): badge mientras la mutación de reenvío está `isPending`.
- El worker NO reintenta automáticamente; el contador se reinicia al entrar en un nuevo estado notificable.
- "Reintentar fallidos" es global y reutiliza `POST /api/settings/email/tools/resend-failed` (sin endpoint nuevo).
- Verificar que cada test falla antes de implementar; commit por tarea o grupo lógico.
