---
description: "Task list — Panel de Administración (Detalle de Factura en Modal + Dashboard de Estadísticas)"
---

# Tasks: Panel de Administración — Detalle de Factura (Modal) y Dashboard de Estadísticas

**Input**: Documentos de diseño en `specs/015-admin-panel-detail-dashboard/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: INCLUIDOS y OBLIGATORIOS. La Constitución (Principio IV — Test-First, NO NEGOCIABLE) exige escribir las pruebas primero (Red-Green-Refactor) con ≥85% de cobertura. Vitest + Testing Library (frontend) y xUnit + Shouldly (backend).

**Organización**: por historia de usuario (US1–US5) para implementación y prueba independientes.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: puede ejecutarse en paralelo (archivos distintos, sin dependencias incompletas)
- **[Story]**: US1, US2, US3, US4, US5
- Cada tarea incluye ruta de archivo exacta

## Path Conventions

- Frontend: `frontend/src/...` y pruebas co-ubicadas o en `frontend/tests/...`
- Backend: `backend/{Domain,Application,Infrastructure,Api,Tests}/...`

---

## Phase 1: Setup (Infraestructura compartida)

**Purpose**: primitivas shadcn y utilidades transversales del modal y el dashboard. Sin dependencias de runtime nuevas (Radix dialog ya instalado; gráficos in-house con SVG + Motion).

- [X] T001 [P] Añadir primitiva shadcn `dialog` (sobre `@radix-ui/react-dialog` ya instalado) en `frontend/src/components/ui/dialog.tsx`
- [X] T002 [P] Añadir primitiva shadcn `card` (tarjetas del dashboard) en `frontend/src/components/ui/card.tsx`
- [X] T003 [P] Extender variantes/duraciones de animación para modal y gráficos (entrada de overlay/contenido, barras animadas) en `frontend/src/lib/motion.ts`

---

## Phase 2: Foundational (Prerrequisitos bloqueantes)

**Purpose**: extensión del dominio/API (historial + destinos válidos), **eliminación de legacy + migración**, y la capa de datos compartida del detalle que consumen US1, US2 y US3.

**⚠️ CRITICAL**: ninguna historia del modal (US1/US2/US3) puede completarse hasta terminar esta fase.

### Backend — pruebas primero (deben FALLAR)

- [X] T004 [P] Pruebas de dominio: `Invoice.UpdateStatus(newStatus, source)` añade un `StatusChange` (from/to/at/source) y el constructor inicia en `Pending` (ya no `Draft`) en `backend/Tests/Monolegal.Domain.Tests/Entities/InvoiceTests.cs`
- [X] T005 [P] Pruebas de dominio: `InvoiceTransitionService.GetAllowedTransitions(status)` por cada estado (incluye `Pagado` como destino; conjunto vacío en `Pagado`) en `backend/Tests/Monolegal.Domain.Tests/Services/InvoiceTransitionServiceTests.cs`
- [X] T006 [P] Pruebas de API: `GET /api/invoices/{id}` devuelve `statusHistory` y `allowedTransitions` correctos; `POST /api/invoices/transition/{id}` devuelve historial con `source: "manual"` y destinos recalculados en `backend/Tests/Monolegal.Application.Tests/Endpoints/InvoiceDetailTests.cs`
- [X] T007 [P] Prueba de Infraestructura: round-trip Mongo del `StatusHistory` embebido y que `UpdateAsync` conserva el historial acumulado en `backend/Tests/Infrastructure/MongoInvoiceRepositoryStatusHistoryTests.cs`
- [X] T008 [P] Prueba de Infraestructura: migración idempotente (remapeo de estados legacy + backfill de evento de creación; reejecución sin duplicar) en `backend/Tests/Infrastructure/StatusHistoryBackfillMigrationTests.cs`

### Backend — dominio (historial + destinos válidos + inicio en Pending)

- [X] T009 [P] Crear enum `StatusChangeSource` (`Automatic | Manual`, serialización en minúscula) en `backend/Domain/Enums/StatusChangeSource.cs`
- [X] T010 [P] Crear value object inmutable `StatusChange { From, To, At, Source }` en `backend/Domain/Entities/StatusChange.cs`
- [X] T011 Editar `Invoice`: añadir `StatusHistory` (lista embebida), reescribir `UpdateStatus(InvoiceStatus, StatusChangeSource)` para *appendear* el evento, e iniciar el constructor en `Pending` en `backend/Domain/Entities/Invoice.cs`
- [X] T012 Editar `InvoiceTransitionService`: añadir `GetAllowedTransitions(status)` (única fuente de verdad) y propagar el `source` (`Automatic` desde `TryApplyTransition`; `Manual` desde `ApplyManualTransition`/`ApplyPayment`) en `backend/Domain/Services/InvoiceTransitionService.cs`

### Backend — DTOs y endpoints

- [X] T013 Editar `InvoiceDtos`: añadir `StatusChangeDto` y extender `InvoiceDetailDto` con `StatusHistory` + `AllowedTransitions` en `backend/Api/Endpoints/Invoices/InvoiceDtos.cs`
- [X] T014 Editar `GetInvoiceById`: inyectar `InvoiceTransitionService` y mapear `statusHistory` + `allowedTransitions` en `backend/Api/Endpoints/Invoices/GetInvoiceById.cs`
- [X] T015 Verificar/ajustar `TransitionInvoice` para que su respuesta `InvoiceDetailDto` incluya los nuevos campos (la notificación al cliente vía `IInvoiceTransitionNotifier` ya está cableada — habilita FR-017a) en `backend/Api/Endpoints/Invoices/TransitionInvoice.cs`

### Backend — eliminación de legacy (FR-029, FR-031)

- [X] T016 Eliminar `UpdateStatusAsync` de la interfaz en `backend/Domain/Repositories/IInvoiceRepository.cs`
- [X] T017 Eliminar la implementación de `UpdateStatusAsync` en `backend/Infrastructure/Repositories/MongoInvoiceRepository.cs`
- [X] T018 Eliminar `UpdateStatusAsync` de los dobles de prueba: `backend/Tests/Infrastructure/Support/InMemoryInvoiceRepository.cs`, `backend/Tests/Infrastructure/Support/ThrowingInvoiceRepository.cs`, `backend/Tests/Monolegal.Application.Tests/Seeding/FakeInvoiceRepository.cs` y los fakes inline de `backend/Tests/Monolegal.Application.Tests/InvoiceWorkerTests.cs`
- [X] T019 Eliminar las pruebas de `UpdateStatusAsync`: borrar `backend/Tests/Infrastructure/MongoInvoiceRepositoryStatusUpdateTests.cs` y la sección `UpdateStatusAsync` de `backend/Tests/Infrastructure/InvoiceRepositoryContractTests.cs`
- [X] T020 Retirar los valores legacy `Draft/Overdue/Cancelled` de `backend/Domain/Enums/InvoiceStatus.cs` y actualizar el comentario de estados aceptados en `backend/Api/Endpoints/Invoices/InvoiceStatusApi.cs`
- [X] T021 Ajustar a inicio en `Pending` los puntos que asumían `Draft` (seeder de desarrollo y pruebas de specs 007/008): revisar `backend/Infrastructure/Hosting/DevDataSeederHostedService.cs` y las pruebas afectadas en `backend/Tests/...` (p. ej. `InvoiceTests`, seeding, notificaciones)

### Backend — migración (FR-030, FR-031)

- [X] T022 Implementar `StatusHistoryBackfillMigration` idempotente (1: remapeo Borrador→Pending, Vencida→Pending, Cancelada→Desactivado; 2: sembrar evento de creación en facturas sin historial) y registrarla como hosted service de arranque en `backend/Infrastructure/Hosting/StatusHistoryBackfillMigration.cs` (+ DI en `backend/Infrastructure/Configuration/DependencyInjection.cs`)
- [X] T023 [P] Asegurar la serialización del value object `StatusChange` y del enum `StatusChangeSource` (minúsculas, consistente con `InvoiceStatus`) revisando el registro de serializadores/class maps en `backend/Infrastructure/Configuration/DependencyInjection.cs`

### Frontend — capa de datos compartida del detalle

- [X] T024 [P] Añadir tipos `InvoiceDetail` y `StatusChange`; retirar los estados legacy (`draft/overdue/cancelled`) de `KnownInvoiceStatus`, `INVOICE_STATUS_LABELS` y `TERMINAL_STATUSES` en `frontend/src/features/invoices/types.ts`
- [X] T025 [P] Implementar `getInvoiceDetail` (GET `/api/invoices/{id}`, manejo de error/404) en `frontend/src/features/invoices/api/getInvoiceDetail.ts`
- [X] T026 [P] Implementar `useInvoiceDetail` (`useQuery ['invoice', id]`, *enabled* solo con id) en `frontend/src/features/invoices/api/useInvoiceDetail.ts`

**Checkpoint**: backend extendido y sin legacy (verde), migración lista, datos del detalle disponibles. El modal puede construirse.

---

## Phase 3: User Story 1 — Detalle completo de una factura (Priority: P1) 🎯 MVP

**Goal**: al activar una fila se abre un modal con todos los campos de la factura, con skeleton de carga, cierre accesible y manejo de error/404.

**Independent Test**: clic en fila → modal con todos los campos (monto moneda, fechas en español, badge de estado); skeleton durante la carga; cierre por botón/escape/overlay con retorno de foco; error/404 con mensaje.

### Tests (escribir primero, deben FALLAR)

- [X] T027 [P] [US1] Test de `useSelectedInvoice` (abre/cierra vía `?factura=<id>`) en `frontend/tests/features/invoices/useSelectedInvoice.test.tsx`
- [X] T028 [P] [US1] Test de `InvoiceDetailFields` (todos los campos, formato moneda/fecha, badge, estado desconocido neutro) en `frontend/tests/features/invoices/InvoiceDetailFields.test.tsx`
- [X] T029 [P] [US1] Test de `InvoiceDetailModal` (apertura desde fila, skeleton→contenido, cierre por botón/escape/overlay + retorno de foco, error/404) en `frontend/tests/features/invoices/InvoiceDetailModal.test.tsx`

### Implementation

- [X] T030 [P] [US1] Implementar `useSelectedInvoice` (sobre `useSearchParams`) en `frontend/src/features/invoices/hooks/useSelectedInvoice.ts`
- [X] T031 [P] [US1] Implementar `InvoiceDetailSkeleton` (forma del contenido del modal) en `frontend/src/features/invoices/components/InvoiceDetailSkeleton.tsx`
- [X] T032 [P] [US1] Implementar `InvoiceDetailFields` (todos los campos con formato; reutiliza `StatusBadge` y formateadores) en `frontend/src/features/invoices/components/InvoiceDetailFields.tsx` (+ formateadores en `frontend/src/features/invoices/utils.ts`)
- [X] T033 [US1] Implementar `InvoiceDetailModal` (dialog shadcn; orquesta `useInvoiceDetail`; estados loading/success/error/404; foco) en `frontend/src/features/invoices/components/InvoiceDetailModal.tsx`
- [X] T034 [US1] Editar `InvoicesTable`: fila activable por clic y teclado que llama `open(invoice.id)` en `frontend/src/features/invoices/components/InvoicesTable.tsx`
- [X] T035 [US1] Editar `InvoicesPage`: montar `InvoiceDetailModal` con `invoiceId = selectedId` en `frontend/src/features/invoices/components/InvoicesPage.tsx`

**Checkpoint**: el modal muestra el detalle completo de una factura — demostrable como MVP del 4.3.

---

## Phase 4: User Story 2 — Historial de cambios de estado (Priority: P1)

**Goal**: dentro del modal, una línea de tiempo con cada cambio (from→to, fecha/hora, origen), con evento de creación cuando no hay transiciones.

**Independent Test**: abrir una factura con varias transiciones → timeline ordenada con origen automático/manual; factura sin historial → evento de creación derivado de `createdAt`.

### Tests (escribir primero, deben FALLAR)

- [x] T036 [P] [US2] Test de `StatusHistoryTimeline` (orden cronológico claro, `from→to`, etiqueta de origen, fallback de creación cuando vacío) en `frontend/tests/features/invoices/StatusHistoryTimeline.test.tsx`

### Implementation

- [X] T037 [US2] Implementar `StatusHistoryTimeline` (incluye respaldo del evento de creación desde `createdAt`) en `frontend/src/features/invoices/components/StatusHistoryTimeline.tsx`
- [X] T038 [US2] Integrar la línea de tiempo en `InvoiceDetailModal` en `frontend/src/features/invoices/components/InvoiceDetailModal.tsx`

**Checkpoint**: el modal muestra detalle + historial — US1+US2 funcionan independientemente.

---

## Phase 5: User Story 3 — Cambiar el estado desde el modal (Priority: P1)

**Goal**: control que ofrece solo los destinos válidos del backend y ejecuta el cambio (notificando al cliente), con estados de carga/error y sincronización de modal+listado+dashboard.

**Independent Test**: factura con transición válida → ofrece solo destinos permitidos; aplicar cambio → estado e historial actualizados y listado coherente sin recargar; factura terminal → botón oculto/deshabilitado; error 400 → mensaje sin alterar el estado.

### Tests (escribir primero, deben FALLAR)

- [X] T039 [P] [US3] Test de `ChangeStatusControl` (solo destinos permitidos; oculto/deshabilitado en estado terminal; estado ocupado; error 400) en `frontend/tests/features/invoices/ChangeStatusControl.test.tsx`
- [X] T040 [P] [US3] Test de `useTransitionInvoice` (invalida `['invoice',id]`, `['invoices']`, `['invoice-stats']`) en `frontend/tests/features/invoices/useTransitionInvoice.test.tsx`

### Implementation

- [X] T041 [P] [US3] Implementar `transitionInvoice` (POST `/api/invoices/transition/{id}`, manejo de 400/404) en `frontend/src/features/invoices/api/transitionInvoice.ts`
- [X] T042 [US3] Implementar `useTransitionInvoice` (`useMutation` + invalidación dirigida; opcional fijar `InvoiceDetailDto` devuelto en `['invoice',id]`) en `frontend/src/features/invoices/api/useTransitionInvoice.ts`
- [X] T043 [US3] Implementar `ChangeStatusControl` (select de `allowedTransitions` + confirmar; oculto/deshabilitado si vacío; ocupado; error legible) en `frontend/src/features/invoices/components/ChangeStatusControl.tsx`
- [X] T044 [US3] Integrar `ChangeStatusControl` en `InvoiceDetailModal` (refresca estado + historial al éxito) en `frontend/src/features/invoices/components/InvoiceDetailModal.tsx`

**Checkpoint**: el modal está completo (detalle + historial + cambio de estado) — 4.3 funcional.

---

## Phase 6: User Story 4 — Dashboard de estadísticas (Priority: P2)

**Goal**: sección `/dashboard` con tarjetas (total, por estado, por cliente), gráficos animados (SVG+Motion), último refresh con botón manual, y estados skeleton/vacío/error.

**Independent Test**: navegar a `/dashboard` → skeletons; tarjetas + gráficos animados; indicador de último refresh + botón actualizar; sin datos → ceros/estado vacío; error → mensaje + reintento.

### Tests (escribir primero, deben FALLAR)

- [X] T045 [P] [US4] Test de `DashboardPage` (skeleton, tarjetas, último refresh + botón manual, vacío, error) en `frontend/tests/features/dashboard/DashboardPage.test.tsx`
- [X] T046 [P] [US4] Test de los gráficos (`StatusDistributionChart`, `ClientDistributionChart` top-N + "Otros", respeta reduce-motion) en `frontend/tests/features/dashboard/Charts.test.tsx`
- [X] T047 [P] [US4] Test de `useInvoiceStats` y del util `topClients` (top-N + "Otros") en `frontend/tests/features/dashboard/useInvoiceStats.test.tsx`

### Implementation

- [X] T048 [P] [US4] Crear tipo `InvoiceStats` en `frontend/src/features/dashboard/types.ts`
- [X] T049 [P] [US4] Implementar `getInvoiceStats` (GET `/api/invoices/stats`) y `useInvoiceStats` (`useQuery ['invoice-stats']`) en `frontend/src/features/dashboard/api/getInvoiceStats.ts` y `frontend/src/features/dashboard/api/useInvoiceStats.ts`
- [X] T050 [P] [US4] Implementar `StatCard` en `frontend/src/features/dashboard/components/StatCard.tsx`
- [X] T051 [P] [US4] Implementar `StatusDistributionChart` (SVG + Motion, respeta reduce-motion) en `frontend/src/features/dashboard/components/StatusDistributionChart.tsx`
- [X] T052 [P] [US4] Implementar `ClientDistributionChart` (top-N + "Otros") y el util `topClients` en `frontend/src/features/dashboard/components/ClientDistributionChart.tsx`
- [X] T053 [P] [US4] Implementar `LastRefreshIndicator` (`dataUpdatedAt` + botón manual de actualizar; sin polling) en `frontend/src/features/dashboard/components/LastRefreshIndicator.tsx`
- [X] T054 [P] [US4] Implementar `DashboardSkeleton` y `DashboardEmptyState` en `frontend/src/features/dashboard/components/DashboardSkeleton.tsx` y `frontend/src/features/dashboard/components/DashboardEmptyState.tsx`
- [X] T055 [US4] Implementar `DashboardPage` (orquesta tarjetas + gráficos + último refresh; estados loading/success/empty/error) en `frontend/src/features/dashboard/components/DashboardPage.tsx`
- [X] T056 [US4] Añadir la ruta `/dashboard` (lazy + Suspense con `DashboardSkeleton`) en `frontend/src/App.tsx`

**Checkpoint**: el dashboard es navegable por URL y muestra estadísticas reales — 4.4 funcional.

---

## Phase 7: User Story 5 — Acceso a Dashboard desde la navegación (Priority: P3)

**Goal**: habilitar la entrada "Dashboard" de la navegación lateral y verificar el ruteo y el resaltado de sección activa.

**Independent Test**: la entrada "Dashboard" aparece habilitada; al seleccionarla navega a `/dashboard` con la sección resaltada; la navegación Facturas↔Dashboard funciona en escritorio y móvil.

### Tests (escribir primero, deben FALLAR)

- [X] T057 [P] [US5] Test de navegación: "Dashboard" habilitado (sin "próximamente"), navega a `/dashboard` y queda `aria-current` en `frontend/tests/components/layout/Navigation.dashboard.test.tsx`

### Implementation

- [X] T058 [US5] Habilitar la entrada Dashboard (`disabled: false`) en `frontend/src/components/layout/navigation.ts`
- [X] T059 [US5] Verificar/ajustar el resaltado de sección activa para `/dashboard` en `frontend/src/components/layout/Sidebar.tsx`

**Checkpoint**: todas las historias completas e independientemente funcionales.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [X] T060 [P] Pase de accesibilidad WCAG A: *focus trap* y retorno de foco del modal (Radix Dialog), operabilidad por teclado del control de estado y del dashboard, `aria`/labels en gráficos, en `frontend/src/features/invoices/components/` y `frontend/src/features/dashboard/components/`
- [X] T061 [P] Verificar `prefers-reduced-motion` en modal (apertura/cierre) y gráficos (entrada) vía `useReducedMotion` en `frontend/src/lib/motion.ts` y componentes asociados
- [X] T062 React Doctor **100/100 honesto** (sin supresiones nuevas) sobre el modal y el dashboard
- [ ] T063 Ejecutar y verificar la migración: ninguna factura queda sin historial y no existen documentos en estados legacy tras `StatusHistoryBackfillMigration` (validación de datos) — ⚠️ requiere instancia MongoDB (no disponible en este entorno; los tests `StatusHistoryBackfillMigrationTests` están escritos y compilan, pero solo corren contra Mongo)
- [~] T064 Gates de calidad: ✅ `npm run lint` + `npm run build` + `npm run test:run` (96/96) y `dotnet test` Domain.Tests (71/71); ⚠️ los tests de Infra de `Tests.csproj` (29) fallan solo por falta de MongoDB (`localhost:27017` rechaza la conexión), no por la lógica
- [X] T065 [P] Marcar specs 4.3 y 4.4 como implementadas en `roadmap.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sin dependencias — inicia de inmediato.
- **Foundational (Phase 2)**: depende de Setup — BLOQUEA US1/US2/US3 (modal). US4/US5 (dashboard) dependen solo del endpoint de stats ya existente y de Setup, pero comparten el pulido transversal.
- **US1 (Phase 3)**: tras Foundational (datos del detalle + dialog).
- **US2 (Phase 4)**: tras US1 (integra en el modal).
- **US3 (Phase 5)**: tras US1 (integra en el modal) y Foundational (destinos válidos + endpoint de transición).
- **US4 (Phase 6)**: tras Setup (card/motion); independiente del modal.
- **US5 (Phase 7)**: tras US4 (necesita la ruta/sección para navegar).
- **Polish (Phase 8)**: tras las historias deseadas.

### User Story Dependencies

- **US1 (P1)**: requiere la capa de datos del detalle (Phase 2). Independientemente testeable.
- **US2 (P1)**: extiende el modal de US1 (archivo compartido `InvoiceDetailModal.tsx`).
- **US3 (P1)**: extiende el modal de US1; usa destinos válidos (Phase 2). 
- **US4 (P2)**: independiente del modal; usa `GET /api/invoices/stats` existente.
- **US5 (P3)**: depende de que exista la ruta del dashboard (US4).

### Within Each User Story

- Tests primero (deben fallar) → implementación.
- `InvoiceDetailModal.tsx` evoluciona en US1→US2→US3: las tareas que lo tocan son **secuenciales** (no [P]).
- En backend, dominio antes que DTOs/endpoints; la eliminación de legacy y la migración pueden ir en paralelo a los componentes frontend.

### Parallel Opportunities

- Setup: T001–T003 en paralelo.
- Foundational: tests T004–T008 en paralelo; dominio T009–T010 en paralelo; frontend T024–T026 en paralelo con el backend.
- US1: tests T027–T029 en paralelo; impl hoja T030–T032 en paralelo.
- US3: tests T039–T040 en paralelo; `transitionInvoice` (T041) en paralelo.
- US4: tests T045–T047 en paralelo; componentes hoja T048–T054 en paralelo.

---

## Parallel Example: User Story 4 (Dashboard)

```bash
# Tests de US4 en paralelo:
Task: "DashboardPage.test.tsx"
Task: "Charts.test.tsx"
Task: "useInvoiceStats.test.tsx"

# Componentes hoja de US4 en paralelo:
Task: "StatCard.tsx"
Task: "StatusDistributionChart.tsx"
Task: "ClientDistributionChart.tsx"
Task: "LastRefreshIndicator.tsx"
Task: "DashboardSkeleton.tsx + DashboardEmptyState.tsx"
```

---

## Implementation Strategy

### MVP First (US1 + US2 + US3 = modal 4.3 completo)

1. Completar Phase 1 (Setup) y Phase 2 (Foundational, incl. limpieza de legacy + migración).
2. US1 (detalle en modal) → validar.
3. US2 (historial) → validar.
4. US3 (cambio de estado) → validar.
5. **STOP and VALIDATE**: modal de detalle completo y funcional = MVP del 4.3.

### Incremental Delivery

1. Setup + Foundational → base lista (backend sin legacy + datos del detalle).
2. US1 → demo detalle en modal (MVP del 4.3 en construcción).
3. US2 → demo historial.
4. US3 → demo cambio de estado (4.3 completo).
5. US4 → demo dashboard (4.4).
6. US5 → navegación al dashboard habilitada.
7. Polish → a11y, reduce-motion, React Doctor 100/100, migración verificada.

### Parallel Team Strategy

Tras Foundational, un equipo puede dividir: Dev A en el modal (US1→US2→US3, secuenciales por archivo compartido) y Dev B en el dashboard (US4→US5, independiente del modal).

---

## Notes

- [P] = archivos distintos sin dependencias incompletas.
- `InvoiceDetailModal.tsx` evoluciona en US1→US2→US3: no marcar en paralelo esas tareas.
- Verificar que cada prueba falla antes de implementar (Red-Green-Refactor).
- "No dejar nada en legacy": tras la Phase 2 no debe quedar `UpdateStatusAsync` ni valores legacy del enum, y ninguna factura sin historial (T016–T023, T063).
- La notificación al cliente en cambios manuales (FR-017a) reutiliza el `IInvoiceTransitionNotifier` ya existente; no requiere endpoint nuevo.
- React Doctor 100/100 debe ser **honesto** (FR-028/SC-010): sin añadir supresiones.
- Commit por tarea o grupo lógico; detenerse en checkpoints para validar cada historia.
